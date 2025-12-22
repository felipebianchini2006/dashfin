using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Finance.Infrastructure.Imports;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
  /// <summary>
  /// Uses UglyToad.PdfPig (text extraction; no OCR) for deterministic page-ordered text.
  /// </summary>
  public Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(ReadOnlyMemory<byte> pdfBytes, CancellationToken ct)
  {
    using var ms = new MemoryStream(pdfBytes.ToArray(), writable: false);
    using var doc = PdfDocument.Open(ms);
    var pages = new List<PdfTextPage>(doc.NumberOfPages);

    for (var i = 1; i <= doc.NumberOfPages; i++)
    {
      ct.ThrowIfCancellationRequested();

      Page page = doc.GetPage(i);
      var rawText = ContentOrderTextExtractor.GetText(page);
      var lines = PdfTextCleanup.ToCleanLines(rawText);
      pages.Add(new PdfTextPage(i, rawText, lines));
    }

    return Task.FromResult<IReadOnlyList<PdfTextPage>>(pages);
  }
}
