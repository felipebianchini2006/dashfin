using System.Globalization;

namespace Finance.Application.Imports.Processing;

public static class MoneyParserBr
{
  public static bool TryParse(string input, out decimal value)
  {
    value = 0m;
    if (string.IsNullOrWhiteSpace(input))
      return false;

    var cleaned = input
      .Trim()
      .Replace("R$", "", StringComparison.OrdinalIgnoreCase)
      .Replace(".", "", StringComparison.Ordinal)
      .Replace(" ", "", StringComparison.Ordinal);

    // "1.234,56" -> "1234,56"
    return decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, new CultureInfo("pt-BR"), out value);
  }
}

