using System.Text;

namespace Finance.Application.Imports.Processing;

public static class PdfTextCleanup
{
  public static IReadOnlyList<string> ToCleanLines(string rawText)
  {
    if (string.IsNullOrEmpty(rawText))
      return Array.Empty<string>();

    var split = rawText.Split(['\r', '\n'], StringSplitOptions.None);
    var lines = new List<string>(split.Length);

    foreach (var s in split)
    {
      var cleaned = CleanLine(s);
      if (cleaned.Length == 0)
        continue;
      lines.Add(cleaned);
    }

    return lines;
  }

  public static string CleanLine(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    // Normalize Unicode but preserve symbols ("R$", minus signs, etc).
    var normalized = input.Normalize(NormalizationForm.FormC).Trim();

    var sb = new StringBuilder(normalized.Length);
    var prevSpace = false;

    foreach (var ch in normalized)
    {
      // Treat any whitespace as a single ASCII space (deterministic across PDFs/platforms).
      if (char.IsWhiteSpace(ch))
      {
        if (!prevSpace)
        {
          sb.Append(' ');
          prevSpace = true;
        }
        continue;
      }

      prevSpace = false;

      // Keep characters as-is (including "R$", dates, punctuation). Lowercasing is NOT applied here.
      sb.Append(ch);
    }

    // Some PDFs emit NBSP; the loop already normalizes to ' '.
    return sb.ToString().Trim();
  }
}
