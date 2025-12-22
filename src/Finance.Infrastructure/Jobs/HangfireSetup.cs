using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Infrastructure.Jobs;

public static class HangfireSetup
{
  public static IServiceCollection AddHangfirePostgres(
    this IServiceCollection services,
    IConfiguration config)
  {
    var connectionString = config.GetConnectionString("Default");
    services.AddHangfire(cfg =>
    {
      cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
      cfg.UseSimpleAssemblyNameTypeSerializer();
      cfg.UseRecommendedSerializerSettings();
      cfg.UsePostgreSqlStorage(connectionString);
    });

    services.AddHangfireServer();
    return services;
  }
}

