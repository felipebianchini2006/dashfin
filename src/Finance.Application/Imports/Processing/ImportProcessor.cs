using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Finance.Application.Abstractions;
using Finance.Application.CategoryRules;
using Finance.Application.Common;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Finance.Application.Imports.Processing;

public sealed class ImportProcessor
{
  private readonly IAppDbContext _db;
  private readonly IFileStorage _storage;
  private readonly IPdfTextExtractor _pdf;
  private readonly IClock _clock;
  private readonly ILogger<ImportProcessor> _logger;
  private readonly IPostImportTasks? _post;

  public ImportProcessor(
    IAppDbContext db,
    IFileStorage storage,
    IPdfTextExtractor pdf,
    IClock clock,
    ILogger<ImportProcessor> logger,
    IPostImportTasks? post = null)
  {
    _db = db;
    _storage = storage;
    _pdf = pdf;
    _clock = clock;
    _logger = logger;
    _post = post;
  }

  public async Task<Result> ProcessImportAsync(Guid importId, CancellationToken ct)
  {
    using var scope = _logger.BeginScope(new Dictionary<string, object> { ["importId"] = importId });

    var import = await _db.Imports.SingleOrDefaultAsync(i => i.Id == importId, ct);
    if (import is null)
      return Result.Fail(Error.NotFound("Import not found."));

    if (import.Status == ImportStatus.Done)
    {
      _logger.LogInformation("Import already DONE; skipping");
      return Result.Ok();
    }

    var userId = import.UserId;
    if (import.AccountId is null)
      return Result.Fail(Error.Validation("Missing account_id."));

    var account = await _db.Accounts.SingleOrDefaultAsync(a => a.Id == import.AccountId.Value && a.UserId == userId, ct);
    if (account is null)
      return Result.Fail(Error.Forbidden("Account does not belong to user."));

    _logger.LogInformation("Starting import processing (provider={Provider})", import.StorageProvider);

    import.Status = ImportStatus.Processing;
    import.ErrorMessage = null;
    import.ProcessedAt = null;
    import.SummaryJson = null;
    await _db.SaveChangesAsync(ct);

    try
    {
      await using var pdfStream = await _storage.OpenReadAsync(import.StorageKey, ct);
      byte[] pdfBytes;
      await using (var ms = new MemoryStream())
      {
        await pdfStream.CopyToAsync(ms, ct);
        pdfBytes = ms.ToArray();
      }

      var pages = await _pdf.ExtractTextByPageAsync(pdfBytes, ct);
      var allLines = pages.SelectMany(p => p.Lines).ToList();

      var layout = ImportLayoutDetector.Detect(allLines);

      var now = _clock.UtcNow;
      var fingerprintBuilder = new TransactionFingerprintBuilder();
      var autoCategorizer = new CategoryAutoCategorizer(_db);
      var parsed = new List<ParsedTransaction>();
      var rows = new List<ImportRow>();
      var rowIndex = 0;
      var errorCount = 0;

      if (layout == ImportLayout.NubankConta)
      {
        var parser = new NubankCheckingPdfParser();
        var result = parser.Parse(pages);

        foreach (var audit in result.RowAudits)
        {
          rowIndex = Math.Max(rowIndex, audit.RowIndex);
          if (audit.Status == ImportRowStatus.Error)
            errorCount++;

          rows.Add(new ImportRow
          {
            ImportId = importId,
            UserId = userId,
            RowIndex = audit.RowIndex,
            PageNumber = audit.PageNumber,
            RowSha256 = HashLine(audit.Line),
            Status = audit.Status,
            RawText = audit.Line,
            ErrorCode = audit.Reason,
            ErrorMessage = audit.ErrorMessage
          });
        }

        foreach (var t in result.ParsedTransactions)
        {
          var built = fingerprintBuilder.BuildWithLegacy(
            userId,
            account.Id,
            t.OccurredAt,
            t.Amount,
            t.Description,
            t.Currency,
            t.SourceLine);

          parsed.Add(new ParsedTransaction(
            t.OccurredAt,
            t.Description,
            t.Amount,
            t.Currency,
            built.DescriptionNormalized,
            built.Hash,
            t.SourceLine,
            built.LegacyHash));
        }
      }
      else if (layout == ImportLayout.NubankCartao)
      {
        var parser = new NubankCreditCardPdfParser();
        var result = parser.Parse(pages);

        foreach (var audit in result.RowAudits)
        {
          rowIndex = Math.Max(rowIndex, audit.RowIndex);
          if (audit.Status == ImportRowStatus.Error)
            errorCount++;

          rows.Add(new ImportRow
          {
            ImportId = importId,
            UserId = userId,
            RowIndex = audit.RowIndex,
            PageNumber = audit.PageNumber,
            RowSha256 = HashLine(audit.Line),
            Status = audit.Status,
            RawText = audit.Line,
            ErrorCode = audit.Reason,
            ErrorMessage = audit.ErrorMessage
          });
        }

        foreach (var t in result.ParsedTransactions)
        {
          var built = fingerprintBuilder.BuildWithLegacy(
            userId,
            account.Id,
            t.OccurredAt,
            t.Amount,
            t.Description,
            t.Currency,
            t.SourceLine);

          parsed.Add(new ParsedTransaction(
            t.OccurredAt,
            t.Description,
            t.Amount,
            t.Currency,
            built.DescriptionNormalized,
            built.Hash,
            t.SourceLine,
            built.LegacyHash));
        }
      }
      else
      {
        return await FailAsync(import, $"Unknown PDF layout for import {importId:D}.", ct);
      }

      var parsedUnique = parsed
        .GroupBy(t => t.Fingerprint, StringComparer.Ordinal)
        .Select(g => g.First())
        .ToList();

      var fingerprints = parsedUnique
        .SelectMany(t => t.LegacyFingerprint is null ? [t.Fingerprint] : [t.Fingerprint, t.LegacyFingerprint])
        .Distinct(StringComparer.Ordinal)
        .ToList();
      var existingBefore = await _db.Transactions
        .AsNoTracking()
        .Where(t => t.UserId == userId && fingerprints.Contains(t.Fingerprint))
        .Select(t => t.Fingerprint)
        .ToListAsync(ct);

      var existingSet = existingBefore.ToHashSet(StringComparer.Ordinal);
      var toInsert = parsedUnique
        .Where(t => !existingSet.Contains(t.Fingerprint) &&
                    (t.LegacyFingerprint is null || !existingSet.Contains(t.LegacyFingerprint)))
        .ToList();

      var compiledRules = await autoCategorizer.LoadAsync(userId, ct);

      // Audit rows are idempotent per run: recreate for this importId
      await _db.ImportRows.Where(r => r.ImportId == importId && r.UserId == userId).ExecuteDeleteAsync(ct);
      _db.ImportRows.AddRange(rows);
      await _db.SaveChangesAsync(ct);

      foreach (var t in toInsert)
      {
        var categoryId = compiledRules.MatchCategoryId(account.Id, t.DescriptionNormalized, t.Amount);
        _db.Transactions.Add(new Transaction
        {
          Id = Guid.NewGuid(),
          UserId = userId,
          AccountId = account.Id,
          CategoryId = categoryId,
          OccurredAt = t.OccurredAt,
          Description = t.Description,
          Amount = t.Amount,
          Currency = t.Currency,
          Fingerprint = t.Fingerprint,
          MetadataJson = JsonSerializer.Serialize(new { importId })
        });
      }

      try
      {
        await _db.SaveChangesAsync(ct);
      }
      catch (DbUpdateException ex)
      {
        _logger.LogWarning(ex, "Transaction insert had conflicts; retrying with duplicates removed");
        var existingNow = await _db.Transactions
          .AsNoTracking()
          .Where(t => t.UserId == userId && fingerprints.Contains(t.Fingerprint))
          .Select(t => t.Fingerprint)
          .ToListAsync(ct);

        var existingNowSet = existingNow.ToHashSet(StringComparer.Ordinal);
        foreach (var entry in _db.ChangeTracker.Entries<Transaction>().Where(e => e.State == EntityState.Added).ToList())
        {
          if (existingNowSet.Contains(entry.Entity.Fingerprint))
            entry.State = EntityState.Detached;
        }

        await _db.SaveChangesAsync(ct);
      }

      var existingAfter = await _db.Transactions
        .AsNoTracking()
        .Where(t => t.UserId == userId && fingerprints.Contains(t.Fingerprint))
        .Select(t => t.Fingerprint)
        .ToListAsync(ct);

      var insertedCount = Math.Max(0, existingAfter.Count - existingBefore.Count);
      var dedupedCount = parsedUnique.Count - insertedCount;

      var periodStart = parsedUnique.Count == 0 ? (DateTimeOffset?)null : parsedUnique.Min(t => t.OccurredAt);
      var periodEnd = parsedUnique.Count == 0 ? (DateTimeOffset?)null : parsedUnique.Max(t => t.OccurredAt);

      var totalIn = parsedUnique.Where(t => t.Amount > 0).Sum(t => t.Amount);
      var totalOut = parsedUnique.Where(t => t.Amount < 0).Sum(t => t.Amount);
      var totalCard = layout == ImportLayout.NubankCartao ? -parsedUnique.Where(t => t.Amount < 0).Sum(t => t.Amount) : 0m;

      var summary = new
      {
        layout = layout.ToString(),
        period = new { start = periodStart, end = periodEnd },
        counts = new
        {
          parsed = parsedUnique.Count,
          inserted = insertedCount,
          deduped = dedupedCount,
          errors = errorCount
        },
        totals = new
        {
          inAmount = totalIn,
          outAmount = totalOut,
          cardSpend = totalCard
        }
      };

      import.SummaryJson = JsonSerializer.Serialize(summary);
      import.Status = ImportStatus.Done;
      import.ProcessedAt = _clock.UtcNow;
      await _db.SaveChangesAsync(ct);

      _logger.LogInformation("Import DONE (parsed={Parsed}, inserted={Inserted}, deduped={Deduped}, errors={Errors})",
        parsedUnique.Count, insertedCount, dedupedCount, errorCount);

      if (periodStart is not null)
      {
        _post?.EnqueueGenerateAlerts(userId, periodStart.Value.Year, periodStart.Value.Month);
        _post?.EnqueueComputeForecast(userId, periodStart.Value.Year, periodStart.Value.Month);
      }

      return Result.Ok();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Import FAILED");
      return await FailAsync(import, ex.Message, ct);
    }
  }

  private async Task<Result> FailAsync(ImportBatch import, string error, CancellationToken ct)
  {
    import.Status = ImportStatus.Failed;
    import.ErrorMessage = error;
    import.ProcessedAt = _clock.UtcNow;
    await _db.SaveChangesAsync(ct);
    return Result.Fail(Error.Unexpected(error));
  }

  private static string HashLine(string line)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(line ?? string.Empty));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}
