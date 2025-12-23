using System.Security.Cryptography;
using System.Text;

namespace Finance.Application.Alerts.Generate;

internal static class AlertFingerprint
{
  public static string Create(Guid userId, Guid ruleId, DateOnly month, string kind)
  {
    var payload = $"{userId:D}|{ruleId:D}|{month:yyyy-MM}|{kind}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}

