using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Finance.Api.IntegrationTests;

internal static class TestAuth
{
  public static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string password)
  {
    var register = await client.PostAsJsonAsync("/auth/register", new { email, password });
    register.EnsureSuccessStatusCode();

    var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
    login.EnsureSuccessStatusCode();
    var json = await login.Content.ReadFromJsonAsync<LoginResponse>();
    if (json is null || string.IsNullOrWhiteSpace(json.accessToken))
      throw new InvalidOperationException("Missing accessToken in login response.");

    return json.accessToken;
  }

  public static void SetBearer(HttpClient client, string accessToken)
  {
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
  }

  private sealed record LoginResponse(string accessToken);
}

