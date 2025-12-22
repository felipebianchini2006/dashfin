using System.Globalization;
using System.Text.RegularExpressions;
using Finance.Application.Abstractions;
using Finance.Domain.Enums;

namespace Finance.Application.Imports.Processing;

/// <summary>
/// Nubank Cartão (Credit Card) PDF parser.
/// Parsing rules (deterministic, line-ordered):
/// - Works over extracted PDF lines (order preserved); no OCR.
/// - A "transaction" is identified by a purchase date + description + BRL amount.
/// - Purchases are negative by default; credits/estornos/ajustes are positive by keyword heuristics.
/// - Installments ("2/10", "Parcela 2/10") are kept as-is in the description (no expansion).
/// - Totals/resumos/limites are skipped (keywords: "resumo", "total", "vencimento", "limite", "pagamento mínimo", etc.).
/// - Supports multi-line entries:
///   - Start: "02 JAN Uber" (date+desc without amount) then next line has amount ("R$ 15,90")
///   - Or "02 JAN" then next line "Uber R$ 15,90"
///   - Or split description across multiple lines before the amount.
/// - Date normalization:
///   - If year missing, infer defaultYear from statement text (first YYYY found in headers or full dates).
///   - If defaultYear cannot be inferred, missing-year dates become ERROR.
/// Examples accepted:
/// - "02 JAN UBER *TRIP R$ 15,90" => amount -15.90
/// - "03 JAN ESTORNO UBER R$ 15,90" => amount +15.90
/// - "04/01/2025 AJUSTE R$ 10,00" => amount +10.00
/// - Broken:
///   - "02 JAN UBER *TRIP"
///   - "R$ 15,90"
/// </summary>
public sealed class NubankCreditCardPdfParser
{
  private static readonly Regex DatePrefixRegex =
    new(@"^(?<date>(\d{1,2}[\/\-]\d{1,2}([\/\-]\d{2,4})?)|(\d{1,2}\s+[A-Z]{3}(\s+\d{2,4})?))(\s+(?<rest>.*))?$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

  private static readonly Regex AmountOnlyRegex =
    new(@"^(?<amount>(-?\s*)?(R\$\s*)?[\d\.\,]+(\-\s*)?)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  private static readonly Regex DateDescAmountRegex =
    new(@"^(?<date>(\d{1,2}[\/\-]\d{1,2}([\/\-]\d{2,4})?)|(\d{1,2}\s+[A-Z]{3}(\s+\d{2,4})?))\s+(?<desc>.+?)\s+(?<amount>(-?\s*)?(R\$\s*)?[\d\.\,]+(\-\s*)?)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  private static readonly Regex YearRegex =
    new(@"\b(20\d{2})\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public ParseResult Parse(IReadOnlyList<PdfTextPage> pages)
  {
    var lines = pages.SelectMany(p => p.Lines).ToList();
    var defaultYear = InferDefaultYear(lines);

    var audits = new List<RowAudit>(lines.Count);
    var parsed = new List<ParsedTransactionItem>();

    Pending? pending = null;
    var globalRow = 0;

    foreach (var page in pages)
    {
      foreach (var line in page.Lines)
      {
        globalRow++;
        var decision = ParseLine(line, page.PageNumber, globalRow, defaultYear, ref pending);
        audits.Add(decision.Audit);
        if (decision.Transaction is not null)
          parsed.Add(decision.Transaction);
      }
    }

    // Dangling pending entry -> mark error for last line.
    if (pending is not null)
    {
      audits.Add(new RowAudit(
        pending.PageNumber,
        pending.RowIndex,
        pending.SourceLines[^1],
        ImportRowStatus.Error,
        "dangling_transaction",
        "Transaction started but no amount was found."));
    }

    return new ParseResult(defaultYear, parsed, audits);
  }

  private static LineDecision ParseLine(string line, int pageNumber, int rowIndex, int? defaultYear, ref Pending? pending)
  {
    var trimmed = (line ?? string.Empty).Trim();
    if (trimmed.Length == 0)
      return Skip(pageNumber, rowIndex, line, "empty");

    var lower = trimmed.ToLowerInvariant();
    if (IsHeaderFooterOrSummary(lower))
      return Skip(pageNumber, rowIndex, line, "header/footer");

    // If we are pending and this line is amount-only -> finalize.
    if (pending is not null)
    {
      var amountOnly = AmountOnlyRegex.Match(trimmed);
      if (amountOnly.Success)
      {
        if (!MoneyParserBr.TryParse(amountOnly.Groups["amount"].Value, out var v))
          return Error(pageNumber, rowIndex, line, "Invalid amount.");

        var desc = pending.Description.Trim();
        var occurredAt = pending.OccurredAt;
        var amount = ApplySignHeuristic(v, amountOnly.Groups["amount"].Value, desc);
        var tx = new ParsedTransactionItem(occurredAt, desc, amount, "BRL", string.Join(" ", pending.SourceLines) + " " + trimmed, pending.PageNumber, pending.RowIndex);
        pending = null;
        return new LineDecision(
          tx,
          new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Parsed, "transaction_amount", null));
      }
    }

    // Single-line transaction (date + desc + amount).
    var mFull = DateDescAmountRegex.Match(trimmed);
    if (mFull.Success)
    {
      var dateRaw = mFull.Groups["date"].Value.Trim();
      if (!TryParseDate(dateRaw, defaultYear, out var occurredAt, out var dateError))
        return Error(pageNumber, rowIndex, line, dateError);

      var desc = mFull.Groups["desc"].Value.Trim();
      var amountRaw = mFull.Groups["amount"].Value.Trim();
      if (!MoneyParserBr.TryParse(amountRaw, out var v))
        return Error(pageNumber, rowIndex, line, "Invalid amount.");

      var amount = ApplySignHeuristic(v, amountRaw, desc);
      var tx = new ParsedTransactionItem(occurredAt, desc, amount, "BRL", trimmed, pageNumber, rowIndex);
      pending = null;
      return new LineDecision(tx, new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Parsed, "transaction", null));
    }

    // Date prefix without amount -> start/continue pending.
    var mDate = DatePrefixRegex.Match(trimmed);
    if (mDate.Success)
    {
      var dateRaw = mDate.Groups["date"].Value.Trim();
      var rest = mDate.Groups["rest"].Success ? mDate.Groups["rest"].Value.Trim() : string.Empty;

      if (!TryParseDate(dateRaw, defaultYear, out var occurredAt, out var dateError))
        return Error(pageNumber, rowIndex, line, dateError);

      if (pending is not null)
      {
        // Previous pending never completed; mark as error and restart.
        var prev = pending;
        pending = null;
        return new LineDecision(
          null,
          new RowAudit(prev.PageNumber, prev.RowIndex, prev.SourceLines[^1], ImportRowStatus.Error, "dangling_transaction", "Transaction started but no amount was found."));
      }

      pending = new Pending(occurredAt, rest, pageNumber, rowIndex, [trimmed]);
      return new LineDecision(null, new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Parsed, "transaction_start", null));
    }

    // If pending exists, treat this as description continuation (unless it looks like a new header).
    if (pending is not null)
    {
      pending.Description = string.IsNullOrWhiteSpace(pending.Description) ? trimmed : $"{pending.Description} {trimmed}";
      pending.SourceLines.Add(trimmed);
      return new LineDecision(null, new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Parsed, "transaction_continuation", null));
    }

    return Skip(pageNumber, rowIndex, line, "unrecognized");
  }

  private static bool IsHeaderFooterOrSummary(string lower)
  {
    return lower.Contains("fatura", StringComparison.Ordinal)
      || lower.Contains("resumo", StringComparison.Ordinal)
      || lower.Contains("total", StringComparison.Ordinal)
      || lower.Contains("vencimento", StringComparison.Ordinal)
      || lower.Contains("pagamento mínimo", StringComparison.Ordinal)
      || lower.Contains("pagamento minimo", StringComparison.Ordinal)
      || lower.Contains("limite", StringComparison.Ordinal)
      || lower.Contains("disponível", StringComparison.Ordinal)
      || lower.Contains("disponivel", StringComparison.Ordinal)
      || lower.Contains("página", StringComparison.Ordinal)
      || lower.Contains("pagina", StringComparison.Ordinal);
  }

  private static decimal ApplySignHeuristic(decimal parsedValue, string rawAmount, string description)
  {
    // Explicit sign always wins.
    if (rawAmount.Contains('-', StringComparison.Ordinal))
      return parsedValue;

    var d = description.ToLowerInvariant();
    if (d.Contains("estorno", StringComparison.Ordinal)
        || d.Contains("reembolso", StringComparison.Ordinal)
        || d.Contains("refund", StringComparison.Ordinal)
        || d.Contains("crédito", StringComparison.Ordinal)
        || d.Contains("credito", StringComparison.Ordinal)
        || d.Contains("ajuste", StringComparison.Ordinal)
        || d.Contains("cancel", StringComparison.Ordinal)
        || d.Contains("devolu", StringComparison.Ordinal))
      return Math.Abs(parsedValue);

    return -Math.Abs(parsedValue);
  }

  private static bool TryParseDate(string raw, int? defaultYear, out DateTimeOffset occurredAt, out string error)
  {
    occurredAt = default;
    error = "Invalid date.";
    raw = raw.Trim();

    if (DateTime.TryParseExact(raw, ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out var full))
    {
      occurredAt = new DateTimeOffset(full.Date, TimeSpan.Zero);
      return true;
    }

    if (Regex.IsMatch(raw, @"^\d{1,2}[\/\-]\d{1,2}$", RegexOptions.CultureInvariant))
    {
      if (defaultYear is null)
      {
        error = "Missing year and statement year could not be inferred.";
        return false;
      }

      var parts = raw.Replace('-', '/').Split('/');
      var day = int.Parse(parts[0], CultureInfo.InvariantCulture);
      var month = int.Parse(parts[1], CultureInfo.InvariantCulture);
      try
      {
        occurredAt = new DateTimeOffset(new DateTime(defaultYear.Value, month, day), TimeSpan.Zero);
        return true;
      }
      catch (ArgumentOutOfRangeException)
      {
        error = "Invalid date.";
        return false;
      }
    }

    var upper = raw.ToUpperInvariant();
    if (DateTime.TryParseExact(upper, ["dd MMM yyyy", "d MMM yyyy"], new CultureInfo("pt-BR"),
          DateTimeStyles.None, out var m1)
        || DateTime.TryParseExact(upper, ["dd MMM yyyy", "d MMM yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out m1))
    {
      occurredAt = new DateTimeOffset(m1.Date, TimeSpan.Zero);
      return true;
    }

    if (DateTime.TryParseExact(upper, ["dd MMM", "d MMM"], new CultureInfo("pt-BR"),
          DateTimeStyles.None, out var noYear)
        || DateTime.TryParseExact(upper, ["dd MMM", "d MMM"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out noYear))
    {
      if (defaultYear is null)
      {
        error = "Missing year and statement year could not be inferred.";
        return false;
      }

      occurredAt = new DateTimeOffset(new DateTime(defaultYear.Value, noYear.Month, noYear.Day), TimeSpan.Zero);
      return true;
    }

    return false;
  }

  private static int? InferDefaultYear(IEnumerable<string> allLines)
  {
    foreach (var line in allLines)
    {
      var m = Regex.Match(line, @"\b\d{1,2}[\/\-]\d{1,2}[\/\-](?<y>20\d{2})\b", RegexOptions.CultureInvariant);
      if (m.Success && int.TryParse(m.Groups["y"].Value, out var y1))
        return y1;
    }

    foreach (var line in allLines)
    {
      var m = YearRegex.Match(line);
      if (m.Success && int.TryParse(m.Groups[1].Value, out var y2))
        return y2;
    }

    return null;
  }

  private static LineDecision Skip(int pageNumber, int rowIndex, string line, string reason)
    => new(null, new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Skipped, reason, null));

  private static LineDecision Error(int pageNumber, int rowIndex, string line, string error)
    => new(null, new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Error, "parse_error", error));

  private sealed record LineDecision(ParsedTransactionItem? Transaction, RowAudit Audit);

  private sealed class Pending
  {
    public Pending(DateTimeOffset occurredAt, string description, int pageNumber, int rowIndex, List<string> sourceLines)
    {
      OccurredAt = occurredAt;
      Description = description;
      PageNumber = pageNumber;
      RowIndex = rowIndex;
      SourceLines = sourceLines;
    }

    public DateTimeOffset OccurredAt { get; }
    public string Description { get; set; }
    public int PageNumber { get; }
    public int RowIndex { get; }
    public List<string> SourceLines { get; }
  }

  public sealed record ParsedTransactionItem(
    DateTimeOffset OccurredAt,
    string Description,
    decimal Amount,
    string Currency,
    string SourceLine,
    int PageNumber,
    int RowIndex);

  public sealed record RowAudit(
    int PageNumber,
    int RowIndex,
    string Line,
    ImportRowStatus Status,
    string Reason,
    string? ErrorMessage);

  public sealed record ParseResult(
    int? DefaultYear,
    IReadOnlyList<ParsedTransactionItem> ParsedTransactions,
    IReadOnlyList<RowAudit> RowAudits);
}

