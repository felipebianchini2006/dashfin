using System.Text;
using Finance.Application.Abstractions;

namespace Finance.Infrastructure.Imports;

/// <summary>
/// Test-friendly extractor that treats PDF bytes as plain UTF-8 text and splits by newline.
/// Useful for integration/E2E tests that upload a "%PDF" stub containing already-extracted lines.
/// </summary>
public sealed class PlainTextPdfTextExtractor : IPdfTextExtractor
{
  public Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(ReadOnlyMemory<byte> pdfBytes, CancellationToken ct)
  {
    var text = Encoding.UTF8.GetString(pdfBytes.Span);
    var lines = text
      .Replace("\r\n", "\n")
      .Replace('\r', '\n')
      .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
      .ToList();

    IReadOnlyList<PdfTextPage> pages = [new PdfTextPage(1, text, lines)];
    return Task.FromResult(pages);
  }
}

