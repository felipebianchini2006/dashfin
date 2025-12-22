using System.Globalization;
using System.Text.RegularExpressions;

namespace Finance.Application.Imports.Processing;

public interface IImportStatementParser
{
  bool TryParseLine(string line, int pageNumber, int rowIndex, DateTimeOffset now, out ParseLineResult result);
}

public sealed record ParseLineResult(
  bool IsSkippable,
  string? SkipReason,
  ParsedTransaction? Transaction,
  string? ErrorMessage);

internal sealed class NubankContaParser : IImportStatementParser
{
  // Examples seen in practice vary; keep it permissive:
  // "05/01/2025 PIX RECEBIDO R$ 100,00"
  // "05 JAN 2025 Transferência R$ -10,00"
  private static readonly Regex DateAmount =
    new(@"^(?<date>\d{1,2}[\/\-]\d{1,2}([\/\-]\d{2,4})?|\d{1,2}\s+[A-Z]{3}\s+\d{2,4})\s+(?<desc>.+?)\s+(?<amount>-?\s*R\$\s*[\d\.\,]+)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  public bool TryParseLine(string line, int pageNumber, int rowIndex, DateTimeOffset now, out ParseLineResult result)
  {
    var trimmed = (line ?? string.Empty).Trim();
    if (trimmed.Length == 0)
    {
      result = new ParseLineResult(true, "empty", null, null);
      return true;
    }

    var lower = trimmed.ToLowerInvariant();
    if (lower.Contains("nuconta") || lower.Contains("conta do nubank") || lower.Contains("extrato") ||
        lower.Contains("saldo") || lower.Contains("página") || lower.Contains("pagina"))
    {
      result = new ParseLineResult(true, "header/footer", null, null);
      return true;
    }

    var match = DateAmount.Match(trimmed);
    if (!match.Success)
    {
      result = new ParseLineResult(true, "unrecognized", null, null);
      return true;
    }

    if (!TryParseDate(match.Groups["date"].Value, now, out var date))
    {
      result = new ParseLineResult(false, null, null, "Invalid date.");
      return true;
    }

    var desc = match.Groups["desc"].Value.Trim();
    if (!MoneyParserBr.TryParse(match.Groups["amount"].Value, out var amount))
    {
      result = new ParseLineResult(false, null, null, "Invalid amount.");
      return true;
    }

    result = new ParseLineResult(false, null,
      new ParsedTransaction(date, desc, amount, "BRL", DescriptionNormalizer.Normalize(desc), "", trimmed),
      null);
    return true;
  }

  private static bool TryParseDate(string input, DateTimeOffset now, out DateTimeOffset date)
  {
    date = default;
    input = input.Trim();

    if (DateTime.TryParseExact(input, ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var full))
    {
      date = new DateTimeOffset(full.Date, TimeSpan.Zero);
      return true;
    }

    // "05 JAN 2025" (pt-BR month abbreviations sometimes in PT; also EN)
    if (DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM yyyy", "d MMM yyyy"], new CultureInfo("pt-BR"),
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var m1)
        || DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM yyyy", "d MMM yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out m1))
    {
      date = new DateTimeOffset(m1.Date, TimeSpan.Zero);
      return true;
    }

    // Missing year -> assume current year
    if (DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM", "d MMM"], new CultureInfo("pt-BR"),
          DateTimeStyles.None, out var noYear)
        || DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM", "d MMM"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out noYear))
    {
      var assumed = new DateTime(now.Year, noYear.Month, noYear.Day);
      date = new DateTimeOffset(assumed, TimeSpan.Zero);
      return true;
    }

    return false;
  }
}

internal sealed class NubankCartaoParser : IImportStatementParser
{
  // Typical: "02 JAN UBER *TRIP R$ 15,90"
  private static readonly Regex DateAmount =
    new(@"^(?<date>\d{1,2}\s+[A-Z]{3}|\d{1,2}[\/\-]\d{1,2}([\/\-]\d{2,4})?)\s+(?<desc>.+?)\s+(?<amount>R\$\s*[\d\.\,]+)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  public bool TryParseLine(string line, int pageNumber, int rowIndex, DateTimeOffset now, out ParseLineResult result)
  {
    var trimmed = (line ?? string.Empty).Trim();
    if (trimmed.Length == 0)
    {
      result = new ParseLineResult(true, "empty", null, null);
      return true;
    }

    var lower = trimmed.ToLowerInvariant();
    if (lower.Contains("fatura") || lower.Contains("vencimento") || lower.Contains("pagamento mínimo") ||
        lower.Contains("resumo") || lower.Contains("página") || lower.Contains("pagina"))
    {
      result = new ParseLineResult(true, "header/footer", null, null);
      return true;
    }

    var match = DateAmount.Match(trimmed);
    if (!match.Success)
    {
      result = new ParseLineResult(true, "unrecognized", null, null);
      return true;
    }

    if (!TryParseDate(match.Groups["date"].Value, now, out var date))
    {
      result = new ParseLineResult(false, null, null, "Invalid date.");
      return true;
    }

    var desc = match.Groups["desc"].Value.Trim();
    if (!MoneyParserBr.TryParse(match.Groups["amount"].Value, out var value))
    {
      result = new ParseLineResult(false, null, null, "Invalid amount.");
      return true;
    }

    var amount = -Math.Abs(value);
    if (lower.Contains("estorno") || lower.Contains("reembolso") || lower.Contains("refund"))
      amount = Math.Abs(value);

    result = new ParseLineResult(false, null,
      new ParsedTransaction(date, desc, amount, "BRL", DescriptionNormalizer.Normalize(desc), "", trimmed),
      null);
    return true;
  }

  private static bool TryParseDate(string input, DateTimeOffset now, out DateTimeOffset date)
  {
    date = default;
    input = input.Trim();

    if (DateTime.TryParseExact(input, ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy"], CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var full))
    {
      date = new DateTimeOffset(full.Date, TimeSpan.Zero);
      return true;
    }

    if (DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM", "d MMM"], new CultureInfo("pt-BR"),
          DateTimeStyles.None, out var noYear)
        || DateTime.TryParseExact(input.ToUpperInvariant(), ["dd MMM", "d MMM"], CultureInfo.InvariantCulture,
          DateTimeStyles.None, out noYear))
    {
      var assumed = new DateTime(now.Year, noYear.Month, noYear.Day);
      date = new DateTimeOffset(assumed, TimeSpan.Zero);
      return true;
    }

    return false;
  }
}

