using System.Globalization;
using System.Text.RegularExpressions;
using Finance.Application.Abstractions;
using Finance.Domain.Enums;

namespace Finance.Application.Imports.Processing;

/// <summary>
/// Nubank Conta (Checking) PDF parser.
/// Parsing rules (line-based, deterministic):
/// - Operates on already-extracted PDF lines (order preserved).
/// - A "transaction line" must match: DATE + DESCRIPTION + AMOUNT (BRL) on the same line.
/// - Headers/footers are skipped by keyword heuristics (e.g., "NuConta", "Extrato", "Saldo", "Página").
/// - Amount normalization uses pt-BR formatting (1.234,56 => 1234.56) and supports trailing '-' for negatives.
/// - Sign heuristic:
///   - If amount has explicit negative sign/trailing '-', it is negative.
///   - Else, if description indicates income (e.g., "receb", "entrada", "salário", "cashback", "estorno"), it is positive.
///   - Otherwise, default to negative (expense/outflow).
/// - Date normalization:
///   - If year is missing, infer defaultYear from the statement (first YYYY in full dates or "Mês YYYY" headers).
///   - If defaultYear cannot be inferred, missing-year lines become ERROR.
/// Examples accepted:
/// - "05/01/2025 PIX RECEBIDO R$ 100,00"
/// - "06/01 PIX ENVIADO R$ 10,00"
/// - "07 JAN 2025 Transferência R$ 10,00-"
/// </summary>
public sealed class NubankCheckingPdfParser
{
  private static readonly Regex TransactionRegex =
    new(@"^(?<date>(\d{1,2}[\/\-]\d{1,2}([\/\-]\d{2,4})?)|(\d{1,2}\s+[A-Z]{3}(\s+\d{2,4})?))\s+(?<desc>.+?)\s+(?<amount>(-?\s*)?(R\$\s*)?[\d\.\,]+(\-\s*)?)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

  private static readonly Regex YearRegex = new(@"\b(20\d{2})\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public ParseResult Parse(IReadOnlyList<PdfTextPage> pages)
  {
    var lines = pages.SelectMany(p => p.Lines).ToList();
    var defaultYear = InferDefaultYear(lines);

    var audits = new List<RowAudit>(lines.Count);
    var parsed = new List<ParsedTransactionItem>();

    var globalRow = 0;
    foreach (var page in pages)
    {
      foreach (var line in page.Lines)
      {
        globalRow++;
        var decision = ParseLine(line, page.PageNumber, globalRow, defaultYear);
        audits.Add(decision.Audit);
        if (decision.Transaction is not null)
          parsed.Add(decision.Transaction);
      }
    }

    return new ParseResult(defaultYear, parsed, audits);
  }

  private static LineDecision ParseLine(string line, int pageNumber, int rowIndex, int? defaultYear)
  {
    var trimmed = (line ?? string.Empty).Trim();
    if (trimmed.Length == 0)
      return Skip(pageNumber, rowIndex, line, "empty");

    var lower = trimmed.ToLowerInvariant();
    if (IsHeaderFooter(lower))
      return Skip(pageNumber, rowIndex, line, "header/footer");

    var match = TransactionRegex.Match(trimmed);
    if (!match.Success)
      return Skip(pageNumber, rowIndex, line, "unrecognized");

    var dateRaw = match.Groups["date"].Value.Trim();
    if (!TryParseDate(dateRaw, defaultYear, out var occurredAt, out var dateError))
      return Error(pageNumber, rowIndex, line, dateError);

    var desc = match.Groups["desc"].Value.Trim();
    var amountRaw = match.Groups["amount"].Value.Trim();
    if (!MoneyParserBr.TryParse(amountRaw, out var value))
      return Error(pageNumber, rowIndex, line, "Invalid amount.");

    var amount = ApplySignHeuristic(value, amountRaw, desc);
    var tx = new ParsedTransactionItem(occurredAt, desc, amount, "BRL", trimmed, pageNumber, rowIndex);
    return new LineDecision(
      tx,
      new RowAudit(pageNumber, rowIndex, line, ImportRowStatus.Parsed, "transaction", null));
  }

  private static bool IsHeaderFooter(string lower)
  {
    return lower.Contains("nuconta", StringComparison.Ordinal)
      || lower.Contains("conta do nubank", StringComparison.Ordinal)
      || lower.Contains("extrato", StringComparison.Ordinal)
      || lower.Contains("saldo", StringComparison.Ordinal)
      || lower.Contains("página", StringComparison.Ordinal)
      || lower.Contains("pagina", StringComparison.Ordinal)
      || lower.Contains("www", StringComparison.Ordinal)
      || lower.Contains("nubank", StringComparison.Ordinal) && lower.Contains("cnpj", StringComparison.Ordinal);
  }

  private static decimal ApplySignHeuristic(decimal parsedValue, string rawAmount, string description)
  {
    // Explicit sign always wins.
    if (rawAmount.Contains('-', StringComparison.Ordinal))
      return parsedValue;

    var d = description.ToLowerInvariant();
    if (d.Contains("receb", StringComparison.Ordinal)
        || d.Contains("entrada", StringComparison.Ordinal)
        || d.Contains("salário", StringComparison.Ordinal)
        || d.Contains("salario", StringComparison.Ordinal)
        || d.Contains("cashback", StringComparison.Ordinal)
        || d.Contains("estorno", StringComparison.Ordinal)
        || d.Contains("reembolso", StringComparison.Ordinal)
        || d.Contains("devolu", StringComparison.Ordinal))
      return Math.Abs(parsedValue);

    return -Math.Abs(parsedValue);
  }

  private static bool TryParseDate(string raw, int? defaultYear, out DateTimeOffset occurredAt, out string error)
  {
    occurredAt = default;
    error = "Invalid date.";

    raw = raw.Trim();

    // dd/MM/yyyy
    if (DateTime.TryParseExact(raw, ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out var full))
    {
      occurredAt = new DateTimeOffset(full.Date, TimeSpan.Zero);
      return true;
    }

    // dd/MM (missing year)
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

    // dd MMM [yyyy]
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
