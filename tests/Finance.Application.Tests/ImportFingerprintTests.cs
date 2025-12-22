using Finance.Application.Imports.Processing;
using Xunit;

namespace Finance.Application.Tests;

public sealed class ImportFingerprintTests
{
  [Fact]
  public void Fingerprint_is_stable_with_description_normalization()
  {
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero);
    var amount = 10.50m;

    var d1 = DescriptionNormalizer.Normalize("Café  Uber");
    var d2 = DescriptionNormalizer.Normalize("Cafe uber ");

    var f1 = TransactionFingerprint.Create(userId, accountId, occurredAt, amount, "BRL", d1);
    var f2 = TransactionFingerprint.Create(userId, accountId, occurredAt, amount, "BRL", d2);

    Assert.Equal(f1, f2);
  }
}

