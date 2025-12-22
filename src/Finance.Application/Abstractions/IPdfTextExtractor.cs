namespace Finance.Application.Abstractions;

public sealed record PdfTextPage(int PageNumber, IReadOnlyList<string> Lines);

public interface IPdfTextExtractor
{
  Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(Stream pdf, CancellationToken ct);
}

