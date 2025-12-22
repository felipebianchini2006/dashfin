using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
  public FinanceDbContext CreateDbContext(string[] args)
  {
    var connectionString =
      Environment.GetEnvironmentVariable("ConnectionStrings__Default")
      ?? "Host=localhost;Port=5432;Database=dashfin;Username=postgres;Password=postgres";

    var options = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseNpgsql(connectionString)
      .Options;

    return new FinanceDbContext(options);
  }
}

