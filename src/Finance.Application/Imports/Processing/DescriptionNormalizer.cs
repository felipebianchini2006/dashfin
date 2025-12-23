using System.Globalization;
using System.Text;

namespace Finance.Application.Imports.Processing;

public static class DescriptionNormalizer
{
  private static readonly HashSet<string> VariableLabels = new(StringComparer.Ordinal)
  {
    "ID",
    "REF",
    "REFERENCIA",
    "REFERENCE",
    "COD",
    "CODIGO",
    "DOC",
    "CPF",
    "CNPJ",
    "AUTH",
    "AUT",
    "NSU",
    "TXID",
    "TID",
    "E2E",
    "ENDTOEND",
    "ENDTOENDID",
    "PROTOCOLO"
  };

  public static string Normalize(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    var normalized = input.Trim().Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);

    var prevSpace = false;
    foreach (var ch in normalized)
    {
      var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
      if (cat == UnicodeCategory.NonSpacingMark)
        continue;

      var c = char.ToUpperInvariant(ch);
      if (char.IsLetterOrDigit(c))
      {
        prevSpace = false;
        sb.Append(c);
        continue;
      }

      if (char.IsWhiteSpace(c))
      {
        if (!prevSpace)
        {
          sb.Append(' ');
          prevSpace = true;
        }
        continue;
      }

      if (!prevSpace)
      {
        sb.Append(' ');
        prevSpace = true;
      }
    }

    var collapsed = sb.ToString().Trim();
    if (collapsed.Length == 0)
      return string.Empty;

    var tokens = collapsed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length == 0)
      return string.Empty;

    var filtered = new List<string>(tokens.Length);
    for (var i = 0; i < tokens.Length; i++)
    {
      var token = tokens[i];

      if (VariableLabels.Contains(token))
      {
        if (i + 1 < tokens.Length && IsVariableToken(tokens[i + 1]))
          i++;
        continue;
      }

      if (IsVariableToken(token))
        continue;

      filtered.Add(token);
    }

    return string.Join(' ', filtered).Normalize(NormalizationForm.FormC);
  }

  private static bool IsVariableToken(string token)
  {
    if (token.Length == 0)
      return true;

    var hasLetter = false;
    var hasDigit = false;
    var isAllDigits = true;
    var isAllHex = true;
    var hasDash = false;

    foreach (var ch in token)
    {
      if (ch is >= 'A' and <= 'Z')
      {
        hasLetter = true;
        isAllDigits = false;
      }
      else if (ch is >= '0' and <= '9')
      {
        hasDigit = true;
      }
      else if (ch == '-')
      {
        hasDash = true;
        isAllDigits = false;
      }
      else
      {
        // After normalization, tokens should be alnum; keep defensive.
        isAllDigits = false;
        isAllHex = false;
      }

      if (!((ch is >= '0' and <= '9') || (ch is >= 'A' and <= 'F')))
        isAllHex = false;
    }

    if (isAllDigits && token.Length >= 4)
      return true;

    if (hasLetter && hasDigit && token.Length >= 8)
      return true;

    if (isAllHex && token.Length >= 12)
      return true;

    if (hasDash && hasDigit && token.Length >= 16)
      return true;

    return false;
  }
}
