namespace Finance.Application.Abstractions;

public sealed record PdfTextPage(int PageNumber, string RawText, IReadOnlyList<string> Lines);

public interface IPdfTextExtractor
{
  Task<IReadOnlyList<PdfTextPage>> ExtractTextByPageAsync(ReadOnlyMemory<byte> pdfBytes, CancellationToken ct);
}
