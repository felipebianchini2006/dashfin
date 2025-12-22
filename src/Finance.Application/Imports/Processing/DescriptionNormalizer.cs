using System.Globalization;
using System.Text;

namespace Finance.Application.Imports.Processing;

public static class DescriptionNormalizer
{
  public static string Normalize(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    var trimmed = input.Trim();
    var normalized = trimmed.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);

    var prevSpace = false;
    foreach (var ch in normalized)
    {
      var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
      if (cat == UnicodeCategory.NonSpacingMark)
        continue;

      var c = char.ToLowerInvariant(ch);
      if (char.IsWhiteSpace(c))
      {
        if (!prevSpace)
        {
          sb.Append(' ');
          prevSpace = true;
        }
        continue;
      }

      prevSpace = false;
      sb.Append(c);
    }

    return sb.ToString().Normalize(NormalizationForm.FormC);
  }
}

