using System.Linq;
using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using Finance.Domain.Enums;
using Xunit;

namespace Finance.Application.Tests;

public sealed class NubankCreditCardPdfParserTests
{
  [Fact]
  public void Parses_normal_purchase_as_negative()
  {
    var pages = new[]
    {
      Page(1,
        "Fatura Nubank",
        "Janeiro 2025",
        "02 JAN UBER *TRIP R$ 15,90",
        "Total da fatura R$ 999,99")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Equal(2025, result.DefaultYear);
    Assert.Single(result.ParsedTransactions);
    var tx = result.ParsedTransactions[0];
    Assert.Equal(new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero), tx.OccurredAt);
    Assert.Equal("UBER *TRIP", tx.Description);
    Assert.Equal(-15.90m, tx.Amount);
    Assert.Equal("BRL", tx.Currency);
  }

  [Fact]
  public void Parses_estorno_as_positive()
  {
    var pages = new[]
    {
      Page(1,
        "Fatura 2025",
        "03 JAN ESTORNO UBER R$ 15,90")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Single(result.ParsedTransactions);
    Assert.Equal(15.90m, result.ParsedTransactions[0].Amount);
  }

  [Fact]
  public void Parses_ajuste_as_positive()
  {
    var pages = new[]
    {
      Page(1,
        "Fatura 2025",
        "04/01/2025 AJUSTE R$ 10,00")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Single(result.ParsedTransactions);
    Assert.Equal(10m, result.ParsedTransactions[0].Amount);
  }

  [Fact]
  public void Handles_broken_lines_date_desc_then_amount()
  {
    var pages = new[]
    {
      Page(1,
        "Janeiro 2025",
        "02 JAN UBER *TRIP",
        "R$ 15,90",
        "Pagamento mínimo R$ 1,00")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Single(result.ParsedTransactions);
    Assert.Equal("UBER *TRIP", result.ParsedTransactions[0].Description);
    Assert.Equal(-15.90m, result.ParsedTransactions[0].Amount);

    Assert.Contains(result.RowAudits, a => a.Reason == "transaction_start");
    Assert.Contains(result.RowAudits, a => a.Reason == "transaction_amount");
  }

  [Fact]
  public void Handles_broken_lines_date_only_then_desc_then_amount()
  {
    var pages = new[]
    {
      Page(1,
        "Janeiro 2025",
        "02 JAN",
        "IFood 2/10",
        "R$ 30,00")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Single(result.ParsedTransactions);
    Assert.Equal("IFood 2/10", result.ParsedTransactions[0].Description);
    Assert.Equal(-30m, result.ParsedTransactions[0].Amount);
  }

  [Fact]
  public void Ignores_summary_totals_and_limits()
  {
    var pages = new[]
    {
      Page(1,
        "Fatura 2025",
        "Limite disponível R$ 1.000,00",
        "Total da fatura R$ 100,00",
        "Vencimento 10 JAN")
    };

    var parser = new NubankCreditCardPdfParser();
    var result = parser.Parse(pages);

    Assert.Empty(result.ParsedTransactions);
    Assert.All(result.RowAudits.Where(a => a.Line.Contains("R$", StringComparison.OrdinalIgnoreCase)), a => Assert.Equal(ImportRowStatus.Skipped, a.Status));
  }

  private static PdfTextPage Page(int pageNumber, params string[] lines)
  {
    var raw = string.Join('\n', lines) + "\n";
    return new PdfTextPage(pageNumber, raw, lines);
  }
}

