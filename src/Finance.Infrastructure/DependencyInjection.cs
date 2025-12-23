using Finance.Application.Abstractions;
using Finance.Application.Abstractions.Persistence;
using Finance.Application.Alerts.Generate;
using Finance.Application.Forecasting;
using Finance.Infrastructure.Auth;
using Finance.Infrastructure.Common;
using Finance.Infrastructure.Files;
using Finance.Infrastructure.Imports;
using Finance.Infrastructure.Jobs;
using Finance.Infrastructure.Persistence;
using Finance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
  {
    services.AddDbContext<FinanceDbContext>(opt =>
    {
      opt.UseNpgsql(config.GetConnectionString("Default"));
    });

    services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<FinanceDbContext>());
    services.AddScoped<ITransactionRepository, TransactionRepository>();
    services.AddSingleton<IClock, SystemClock>();

    services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
    services.AddSingleton<ITokenService, JwtTokenService>();
    services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

    services.Configure<LocalFileStorageOptions>(config.GetSection(LocalFileStorageOptions.SectionName));
    services.Configure<S3FileStorageOptions>(config.GetSection(S3FileStorageOptions.SectionName));
    var provider = (config["FileStorage:Provider"] ?? "local").ToLowerInvariant();
    if (provider == "s3")
      services.AddSingleton<IFileStorage, S3FileStorage>();
    else
      services.AddSingleton<IFileStorage, LocalFileStorage>();

    services.AddScoped<GenerateAlertsService>();
    services.AddScoped<ComputeForecastService>();

    var hangfireEnabled = config.GetValue("Hangfire:Enabled", true);
    if (hangfireEnabled)
    {
      services.AddHangfirePostgres(config);
      services.AddScoped<ImportJobs>();
      services.AddScoped<PostImportJobs>();
      services.AddScoped<IImportJobQueue, HangfireImportJobQueue>();
      services.AddScoped<IPostImportTasks, HangfirePostImportTasks>();
    }
    else
    {
      var importQueue = (config["Imports:JobQueue"] ?? "inline").ToLowerInvariant();
      if (importQueue == "noop")
        services.AddSingleton<IImportJobQueue, NoopImportJobQueue>();
      else
        services.AddScoped<IImportJobQueue, InlineImportJobQueue>();

      services.AddSingleton<IPostImportTasks, NoopPostImportTasks>();
    }

    var pdfExtractor = (config["Imports:PdfTextExtractor"] ?? "pdfpig").ToLowerInvariant();
    if (pdfExtractor == "plaintext")
      services.AddSingleton<IPdfTextExtractor, PlainTextPdfTextExtractor>();
    else
      services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();

    services.AddScoped<Finance.Application.Imports.Processing.ImportProcessor>();
    return services;
  }
}
