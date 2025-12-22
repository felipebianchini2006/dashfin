using System.Text;
using Finance.Infrastructure.Imports;
using Xunit;

namespace Finance.Application.Tests;

public sealed class PdfPigTextExtractorTests
{
  [Fact]
  public async Task ExtractTextByPageAsync_is_deterministic_and_cleans_lines()
  {
    var pdfBytes = MinimalPdf.BuildSinglePage("Hello   R$   -10,00\nData  05/01/2025");
    var extractor = new PdfPigTextExtractor();

    var first = await extractor.ExtractTextByPageAsync(pdfBytes, CancellationToken.None);
    var second = await extractor.ExtractTextByPageAsync(pdfBytes, CancellationToken.None);

    Assert.Single(first);
    Assert.Single(second);
    Assert.Equal(first[0].RawText, second[0].RawText);
    Assert.Equal(first[0].Lines, second[0].Lines);

    Assert.Equal(1, first[0].PageNumber);
    Assert.Contains("Hello", first[0].RawText);
    Assert.Contains(first[0].Lines, l => l == "Hello R$ -10,00");
    Assert.Contains(first[0].Lines, l => l == "Data 05/01/2025");
  }

  private static class MinimalPdf
  {
    public static byte[] BuildSinglePage(string text)
    {
      // Minimal PDF generator for tests (Type1 font, simple BT/Tj text).
      // Deterministic bytes -> deterministic extraction.
      using var ms = new MemoryStream();
      using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

      void W(string s) => writer.Write(s);
      void WL(string s) => writer.Write(s + "\n");

      WL("%PDF-1.4");
      writer.Flush();

      var offsets = new List<long> { 0 };
      void Obj(int n, string body)
      {
        offsets.Add(ms.Position);
        WL($"{n} 0 obj");
        WL(body);
        WL("endobj");
        writer.Flush();
      }

      Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
      Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
      Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> >>");

      var lines = text.Split('\n', StringSplitOptions.None);
      var sb = new StringBuilder();
      sb.Append("BT /F1 12 Tf 14 TL 40 160 Td ");
      for (var i = 0; i < lines.Length; i++)
      {
        var escaped = lines[i]
          .Replace("\\", "\\\\", StringComparison.Ordinal)
          .Replace("(", "\\(", StringComparison.Ordinal)
          .Replace(")", "\\)", StringComparison.Ordinal);
        sb.Append('(').Append(escaped).Append(") Tj ");
        if (i < lines.Length - 1)
          sb.Append("T* ");
      }
      sb.Append("ET\n");
      var streamContent = sb.ToString();
      var streamBytes = Encoding.ASCII.GetBytes(streamContent);

      offsets.Add(ms.Position);
      WL("4 0 obj");
      WL($"<< /Length {streamBytes.Length} >>");
      WL("stream");
      writer.Flush();
      ms.Write(streamBytes, 0, streamBytes.Length);
      WL("\nendstream");
      WL("endobj");
      writer.Flush();

      Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

      var xrefOffset = ms.Position;
      WL("xref");
      WL($"0 {offsets.Count}");
      WL("0000000000 65535 f ");
      for (var i = 1; i < offsets.Count; i++)
        WL($"{offsets[i]:D10} 00000 n ");

      WL("trailer");
      WL($"<< /Size {offsets.Count} /Root 1 0 R >>");
      WL("startxref");
      WL($"{xrefOffset}");
      WL("%%EOF");
      writer.Flush();

      return ms.ToArray();
    }
  }
}
