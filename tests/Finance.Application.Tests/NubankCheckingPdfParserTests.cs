using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using Finance.Domain.Enums;
using System.Linq;
using Xunit;

namespace Finance.Application.Tests;

public sealed class NubankCheckingPdfParserTests
{
  [Fact]
  public void Parses_transactions_and_assigns_signs()
  {
    var pages = new[]
    {
      Page(1,
        "NuConta",
        "Extrato Janeiro 2025",
        "05/01 PIX RECEBIDO R$ 100,00",
        "06/01 PIX ENVIADO R$ 10,00",
        "07/01 Supermercado R$ 1.234,56",
        "Saldo R$ 0,00")
    };

    var parser = new NubankCheckingPdfParser();
    var result = parser.Parse(pages);

    Assert.Equal(2025, result.DefaultYear);
    Assert.Equal(3, result.ParsedTransactions.Count);

    var t1 = result.ParsedTransactions[0];
    Assert.Equal(new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero), t1.OccurredAt);
    Assert.Equal("PIX RECEBIDO", t1.Description);
    Assert.Equal(100m, t1.Amount);
    Assert.Equal("BRL", t1.Currency);

    var t2 = result.ParsedTransactions[1];
    Assert.Equal(new DateTimeOffset(2025, 01, 06, 0, 0, 0, TimeSpan.Zero), t2.OccurredAt);
    Assert.Equal("PIX ENVIADO", t2.Description);
    Assert.Equal(-10m, t2.Amount);

    var t3 = result.ParsedTransactions[2];
    Assert.Equal(new DateTimeOffset(2025, 01, 07, 0, 0, 0, TimeSpan.Zero), t3.OccurredAt);
    Assert.Equal("Supermercado", t3.Description);
    Assert.Equal(-1234.56m, t3.Amount);

    Assert.Contains(result.RowAudits, a => a.Status == ImportRowStatus.Skipped && a.Reason == "header/footer");
    Assert.Contains(result.RowAudits, a => a.Status == ImportRowStatus.Parsed);
  }

  [Fact]
  public void Parses_trailing_minus_amounts()
  {
    var pages = new[]
    {
      Page(1,
        "Extrato 2025",
        "07 JAN 2025 Transferência R$ 10,00-")
    };

    var parser = new NubankCheckingPdfParser();
    var result = parser.Parse(pages);

    Assert.Single(result.ParsedTransactions);
    Assert.Equal(-10m, result.ParsedTransactions[0].Amount);
  }

  [Fact]
  public void Missing_year_without_statement_year_marks_error()
  {
    var pages = new[]
    {
      Page(1,
        "NuConta",
        "05/01 PIX RECEBIDO R$ 10,00")
    };

    var parser = new NubankCheckingPdfParser();
    var result = parser.Parse(pages);

    Assert.Null(result.DefaultYear);
    Assert.Empty(result.ParsedTransactions);
    Assert.Contains(result.RowAudits, a => a.Status == ImportRowStatus.Error);
  }

  [Fact]
  public void Invalid_date_marks_error_audit()
  {
    var pages = new[]
    {
      Page(1,
        "Extrato 2025",
        "99/99 PIX RECEBIDO R$ 10,00")
    };

    var parser = new NubankCheckingPdfParser();
    var result = parser.Parse(pages);

    Assert.Empty(result.ParsedTransactions);
    var error = Assert.Single(result.RowAudits.Where(a => a.Status == ImportRowStatus.Error));
    Assert.Equal("parse_error", error.Reason);
  }

  private static PdfTextPage Page(int pageNumber, params string[] lines)
  {
    var raw = string.Join('\n', lines) + "\n";
    return new PdfTextPage(pageNumber, raw, lines);
  }
}
