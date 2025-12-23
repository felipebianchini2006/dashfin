using Finance.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Api.IntegrationTests;

internal sealed class DashfinWebApplicationFactory : WebApplicationFactory<Program>
{
  private readonly string _connectionString;
  private readonly string _filesRoot;

  public DashfinWebApplicationFactory(string connectionString, string filesRoot)
  {
    _connectionString = connectionString;
    _filesRoot = filesRoot;
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((_, cfg) =>
    {
      var overrides = new Dictionary<string, string?>
      {
        ["ConnectionStrings:Default"] = _connectionString,
        ["Hangfire:Enabled"] = "false",
        ["Imports:JobQueue"] = "inline",
        ["Imports:PdfTextExtractor"] = "plaintext",
        ["FileStorage:Provider"] = "local",
        ["FileStorage:Local:RootPath"] = _filesRoot,
        ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_CHANGE_ME",
        ["AuthCookies:RefreshTokenSecure"] = "false",
        ["AuthCookies:RefreshTokenSameSite"] = "Lax",
        ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
      };
      cfg.AddInMemoryCollection(overrides);
    });

    builder.ConfigureServices(services =>
    {
      // Ensure a PdfTextExtractor is registered (via config), and disable any accidental background tasks.
      services.AddSingleton<IPostImportTasks, Finance.Infrastructure.Jobs.NoopPostImportTasks>();
    });
  }
}
