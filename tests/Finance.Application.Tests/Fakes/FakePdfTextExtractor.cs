using Finance.Application.Abstractions;

namespace Finance.Application.Tests.Fakes;

internal sealed class FakePdfTextExtractor : IPdfTextExtractor
{
  public IReadOnlyList<PdfTextPage> Pages { get; init; } = Array.Empty<PdfTextPage>();

  public Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(Stream pdf, CancellationToken ct)
    => Task.FromResult(Pages);
}

