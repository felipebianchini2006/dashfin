using System.Security.Cryptography;
using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Application.Imports.Upload;

internal sealed class UploadImportCommandHandler : IRequestHandler<UploadImportCommand, Result<Guid>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;
  private readonly IFileStorage _storage;
  private readonly IImportJobQueue _jobs;
  private readonly ImportUploadOptions _options;
  private readonly ILogger<UploadImportCommandHandler> _logger;

  public UploadImportCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    IImportJobQueue jobs,
    IOptions<ImportUploadOptions> options,
    ILogger<UploadImportCommandHandler> logger)
  {
    _db = db;
    _currentUser = currentUser;
    _storage = storage;
    _jobs = jobs;
    _options = options.Value;
    _logger = logger;
  }

  public async Task<Result<Guid>> Handle(UploadImportCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<Guid>(Error.Unauthorized());

    var accountOk = await _db.Accounts.AnyAsync(a => a.Id == request.AccountId && a.UserId == userId.Value, ct);
    if (!accountOk)
      return Result.Fail<Guid>(Error.Forbidden("Account does not belong to user."));

    var importId = Guid.NewGuid();
    var storageKey = $"imports/{userId.Value:D}/{importId:D}.pdf";

    var tmpPath = Path.Combine(Path.GetTempPath(), $"dashfin-import-{importId:D}.pdf");
    try
    {
      string fileSha256;
      long total;

      await using (var tmp = File.Create(tmpPath))
      {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        total = 0;
        var checkedHeader = false;
        var header = new byte[4];
        var headerFilled = 0;

        while (true)
        {
          var read = await request.Content.ReadAsync(buffer, ct);
          if (read <= 0)
            break;

          if (!checkedHeader)
          {
            var take = Math.Min(4 - headerFilled, read);
            Array.Copy(buffer, 0, header, headerFilled, take);
            headerFilled += take;
            if (headerFilled == 4)
            {
              checkedHeader = true;
              if (!(header[0] == (byte)'%' && header[1] == (byte)'P' && header[2] == (byte)'D' && header[3] == (byte)'F'))
                return Result.Fail<Guid>(Error.Validation("Invalid PDF file."));
            }
          }

          total += read;
          if (total > _options.MaxUploadBytes)
            return Result.Fail<Guid>(Error.Validation($"Max upload size is {_options.MaxUploadBytes} bytes."));

          sha.AppendData(buffer.AsSpan(0, read));
          await tmp.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        if (total == 0)
          return Result.Fail<Guid>(Error.Validation("File is required."));

        fileSha256 = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
      }

      await using (var readBack = File.OpenRead(tmpPath))
      {
        await _storage.SaveAsync(storageKey, readBack, "application/pdf", ct);
      }

      var import = new ImportBatch
      {
        Id = importId,
        UserId = userId.Value,
        AccountId = request.AccountId,
        Status = ImportStatus.Uploaded,
        FileName = Path.GetFileName(request.FileName),
        FileSizeBytes = total,
        FileSha256 = fileSha256,
        StorageProvider = _storage.Provider,
        StorageKey = storageKey
      };

      _db.Imports.Add(import);
      await _db.SaveChangesAsync(ct);

      _jobs.EnqueueProcessImport(importId);
      _logger.LogInformation("Import uploaded {ImportId} (user={UserId}, account={AccountId}, provider={Provider})",
        importId, userId.Value, request.AccountId, _storage.Provider);

      return Result.Ok(importId);
    }
    finally
    {
      try { if (File.Exists(tmpPath)) File.Delete(tmpPath); }
      catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp import file {TmpPath}", tmpPath); }
    }
  }
}
