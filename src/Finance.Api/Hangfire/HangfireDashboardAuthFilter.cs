using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace Finance.Api.Hangfire;

public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
  private readonly IConfiguration _config;

  public HangfireDashboardAuthFilter(IConfiguration config)
  {
    _config = config;
  }

  public bool Authorize(DashboardContext context)
  {
    var http = context.GetHttpContext();

    // Allow JWT-authenticated callers (e.g. programmatic access).
    if (http.User?.Identity?.IsAuthenticated == true)
      return true;

    // Dev-friendly Basic auth for browser access.
    var username = _config["Hangfire:Dashboard:Username"];
    var password = _config["Hangfire:Dashboard:Password"];
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
      return false;

    var header = http.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
      return false;

    var encoded = header["Basic ".Length..].Trim();
    if (encoded.Length == 0)
      return false;

    string decoded;
    try
    {
      decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    }
    catch
    {
      return false;
    }

    var parts = decoded.Split(':', 2);
    if (parts.Length != 2)
      return false;

    return FixedEquals(parts[0], username) && FixedEquals(parts[1], password);
  }

  private static bool FixedEquals(string a, string b)
  {
    var aBytes = Encoding.UTF8.GetBytes(a);
    var bBytes = Encoding.UTF8.GetBytes(b);
    if (aBytes.Length != bBytes.Length)
      return false;
    return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
  }
}

