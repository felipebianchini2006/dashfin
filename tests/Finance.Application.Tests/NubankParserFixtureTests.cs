using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using Finance.Domain.Enums;
using Xunit;

namespace Finance.Application.Tests;

public sealed class NubankParserFixtureTests
{
  [Fact]
  public void NubankConta_fixture_parses_basic_rows()
  {
    var lines = LoadLines("conta_basic.txt");
    var page = PageFromLines(lines);
    var parser = new NubankCheckingPdfParser();

    var result = parser.Parse([page]);

    Assert.Equal(2025, result.DefaultYear);
    Assert.Equal(2, result.ParsedTransactions.Count);
    Assert.Equal(ImportRowStatus.Parsed, result.RowAudits.Single(a => a.Line.Contains("PIX RECEBIDO")).Status);
  }

  [Fact]
  public void NubankCartao_fixture_parses_multiline_and_estorno()
  {
    var lines = LoadLines("cartao_multiline_estorno.txt");
    var page = PageFromLines(lines);
    var parser = new NubankCreditCardPdfParser();

    var result = parser.Parse([page]);

    Assert.Equal(2025, result.DefaultYear);
    Assert.Equal(2, result.ParsedTransactions.Count);
    Assert.Equal(-15.90m, result.ParsedTransactions[0].Amount);
    Assert.Equal(15.90m, result.ParsedTransactions[1].Amount);
  }

  private static string[] LoadLines(string file)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nubank", file);
    return File.ReadAllLines(path)
      .Select(l => l.TrimEnd('\r'))
      .Where(l => l.Length != 0)
      .ToArray();
  }

  private static PdfTextPage PageFromLines(string[] lines)
  {
    var raw = string.Join('\n', lines) + "\n";
    return new PdfTextPage(1, raw, lines);
  }
}

