using System.Globalization;

namespace Finance.Application.Imports.Processing;

public static class MoneyParserBr
{
  public static bool TryParse(string input, out decimal value)
  {
    value = 0m;
    if (string.IsNullOrWhiteSpace(input))
      return false;

    var trimmed = input.Trim();

    var negative = false;
    if (trimmed.StartsWith('(') && trimmed.EndsWith(')'))
    {
      negative = true;
      trimmed = trimmed[1..^1].Trim();
    }

    if (trimmed.EndsWith('-'))
    {
      negative = true;
      trimmed = trimmed[..^1].Trim();
    }

    var cleaned = trimmed
      .Replace("R$", "", StringComparison.OrdinalIgnoreCase)
      .Replace(".", "", StringComparison.Ordinal)
      .Replace(" ", "", StringComparison.Ordinal);

    // "1.234,56" -> "1234,56"
    if (!decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, new CultureInfo("pt-BR"), out value))
      return false;

    if (negative)
      value = -Math.Abs(value);

    return true;
  }
}
