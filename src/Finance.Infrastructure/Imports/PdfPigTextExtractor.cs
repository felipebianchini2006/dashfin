using Finance.Application.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Finance.Infrastructure.Imports;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
  public Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(Stream pdf, CancellationToken ct)
  {
    using var doc = PdfDocument.Open(pdf);
    var pages = new List<PdfTextPage>(doc.NumberOfPages);

    for (var i = 1; i <= doc.NumberOfPages; i++)
    {
      ct.ThrowIfCancellationRequested();

      Page page = doc.GetPage(i);
      var text = ContentOrderTextExtractor.GetText(page);
      var lines = text
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
      pages.Add(new PdfTextPage(i, lines));
    }

    return Task.FromResult<IReadOnlyList<PdfTextPage>>(pages);
  }
}

