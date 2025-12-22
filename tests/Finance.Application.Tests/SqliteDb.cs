using Finance.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Tests;

internal sealed class SqliteDb : IAsyncDisposable
{
  private readonly SqliteConnection _connection;
  public FinanceDbContext Db { get; }

  public SqliteDb()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseSqlite(_connection)
      .Options;

    Db = new FinanceDbContext(opts);
    Db.Database.EnsureCreated();
  }

  public async ValueTask DisposeAsync()
  {
    await Db.DisposeAsync();
    await _connection.DisposeAsync();
  }
}

