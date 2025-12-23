using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finance.Api.IntegrationTests;

internal sealed class PostgresTestDatabase : IAsyncDisposable
{
  public string ConnectionString { get; }
  public string DatabaseName { get; }

  private readonly string _adminConnection;

  public PostgresTestDatabase()
  {
    var baseConn = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
      ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    var builder = new NpgsqlConnectionStringBuilder(baseConn);
    if (string.IsNullOrWhiteSpace(builder.Database))
      builder.Database = "postgres";

    _adminConnection = builder.ToString();

    DatabaseName = $"dashfin_test_{Guid.NewGuid():N}";
    ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = DatabaseName }.ToString();
  }

  public async Task InitializeAsync()
  {
    await using (var admin = new NpgsqlConnection(_adminConnection))
    {
      await admin.OpenAsync();
      await using var cmd = admin.CreateCommand();
      cmd.CommandText = $"CREATE DATABASE \"{DatabaseName}\"";
      await cmd.ExecuteNonQueryAsync();
    }

    // Ensure schema exists using EF model (no migrations required for tests).
    var opts = new DbContextOptionsBuilder<Finance.Infrastructure.Persistence.FinanceDbContext>()
      .UseNpgsql(ConnectionString)
      .Options;

    await using var db = new Finance.Infrastructure.Persistence.FinanceDbContext(opts);
    await db.Database.EnsureCreatedAsync();
  }

  public async ValueTask DisposeAsync()
  {
    // Drop database (terminate connections).
    await using var admin = new NpgsqlConnection(_adminConnection);
    await admin.OpenAsync();

    await using (var term = admin.CreateCommand())
    {
      term.CommandText = @"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = @db AND pid <> pg_backend_pid();";
      term.Parameters.AddWithValue("db", DatabaseName);
      await term.ExecuteNonQueryAsync();
    }

    await using (var drop = admin.CreateCommand())
    {
      drop.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\"";
      await drop.ExecuteNonQueryAsync();
    }
  }
}

