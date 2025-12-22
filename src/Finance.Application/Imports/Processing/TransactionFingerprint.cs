using System.Security.Cryptography;
using System.Text;

namespace Finance.Application.Imports.Processing;

public static class TransactionFingerprint
{
  public static string Create(
    Guid userId,
    Guid accountId,
    DateTimeOffset occurredAt,
    decimal amount,
    string currency,
    string descriptionNormalized)
  {
    var cents = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    var payload = $"{userId:D}|{accountId:D}|{occurredAt:yyyy-MM-dd}|{cents:0}|{currency}|{descriptionNormalized}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}

