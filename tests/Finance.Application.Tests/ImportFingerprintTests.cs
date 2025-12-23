using Finance.Application.Imports.Processing;
using Xunit;

namespace Finance.Application.Tests;

public sealed class ImportFingerprintTests
{
  [Fact]
  public void Mesmo_lancamento_com_descricoes_ligeiramente_diferentes_tem_mesmo_fingerprint()
  {
    var builder = new TransactionFingerprintBuilder();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero);
    var amount = 10.50m;

    var t1 = builder.Build(userId, accountId, occurredAt, amount, "Café  Uber *TRIP 123456");
    var t2 = builder.Build(userId, accountId, occurredAt, amount, "Cafe uber trip 987654 ");

    Assert.Equal("CAFE UBER TRIP", t1.DescriptionNormalized);
    Assert.Equal(t1.Hash, t2.Hash);
  }

  [Fact]
  public void Mesma_data_e_valor_mas_descricoes_diferentes_nao_deduplica()
  {
    var builder = new TransactionFingerprintBuilder();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero);
    var amount = -42.10m;

    var a = builder.Build(userId, accountId, occurredAt, amount, "UBER TRIP");
    var b = builder.Build(userId, accountId, occurredAt, amount, "IFOOD");

    Assert.NotEqual(a.Hash, b.Hash);
  }

  [Fact]
  public void Lancamentos_repetidos_reais_tem_o_mesmo_fingerprint()
  {
    var builder = new TransactionFingerprintBuilder();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 02, 10, 0, 0, 0, TimeSpan.Zero);
    var amount = 100m;

    var a = builder.Build(userId, accountId, occurredAt, amount, "PIX RECEBIDO JOAO SILVA");
    var b = builder.Build(userId, accountId, occurredAt, amount, "PIX  recebido  João  Silva ");

    Assert.Equal(a.Hash, b.Hash);
  }

  [Fact]
  public void Hash_do_source_line_e_opcional_e_resiliente_a_espacos()
  {
    var builder = new TransactionFingerprintBuilder();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 03, 01, 0, 0, 0, TimeSpan.Zero);
    var amount = -9.99m;

    var opts = new TransactionFingerprintOptions(IncludeSourceLineHash: true);
    var a = builder.Build(userId, accountId, occurredAt, amount, "UBER TRIP", sourceLine: "01/03 UBER   TRIP R$ 9,99", options: opts);
    var b = builder.Build(userId, accountId, occurredAt, amount, "UBER TRIP", sourceLine: "01/03 UBER TRIP R$ 9,99", options: opts);
    var c = builder.Build(userId, accountId, occurredAt, amount, "UBER TRIP", sourceLine: "01/03 UBER TRIP R$ 10,99", options: opts);

    Assert.Equal(a.Hash, b.Hash);
    Assert.NotEqual(a.Hash, c.Hash);
  }

  [Fact]
  public void Ano_com_quatro_digitos_e_preservado_para_evitar_colisoes_entre_descricoes_semelhantes()
  {
    var builder = new TransactionFingerprintBuilder();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 03, 02, 0, 0, 0, TimeSpan.Zero);
    var amount = -100m;

    var a = builder.Build(userId, accountId, occurredAt, amount, "ANUIDADE 2024 CARTAO");
    var b = builder.Build(userId, accountId, occurredAt, amount, "ANUIDADE 2025 CARTAO");

    Assert.NotEqual(a.DescriptionNormalized, b.DescriptionNormalized);
    Assert.NotEqual(a.Hash, b.Hash);
  }
}
