using System.Net.Http.Json;
using Finance.Application.Imports.Processing;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.IntegrationTests;

public sealed class ImportsTransactionsDashboardTests : IAsyncLifetime
{
  private PostgresTestDatabase _db = null!;
  private DashfinWebApplicationFactory _factory = null!;
  private HttpClient _client = null!;

  public async Task InitializeAsync()
  {
    _db = new PostgresTestDatabase();
    await _db.InitializeAsync();

    var filesRoot = Path.Combine(Path.GetTempPath(), $"dashfin-it-files-{Guid.NewGuid():N}");
    Directory.CreateDirectory(filesRoot);

    _factory = new DashfinWebApplicationFactory(_db.ConnectionString, filesRoot);
    _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("http://localhost")
    });

    var token = await TestAuth.RegisterAndLoginAsync(_client, $"it-{Guid.NewGuid():N}@example.com", "Password1!");
    TestAuth.SetBearer(_client, token);
  }

  public async Task DisposeAsync()
  {
    _client.Dispose();
    await _factory.DisposeAsync();
    await _db.DisposeAsync();
  }

  [Fact]
  public async Task Upload_import_creates_transactions_and_dashboard_updates()
  {
    // Create a CHECKING account via API.
    var accRes = await _client.PostAsJsonAsync("/accounts", new { name = "Banco", type = 1, initialBalance = 0 });
    accRes.EnsureSuccessStatusCode();
    var account = await accRes.Content.ReadFromJsonAsync<AccountDto>();
    Assert.NotNull(account);

    // Upload a "PDF" stub containing already-extracted lines.
    var pdfBytes = FakePdfBytes(
      "NuConta",
      "Extrato Janeiro 2025",
      "05/01 PIX RECEBIDO R$ 100,00",
      "06/01 PIX ENVIADO R$ 10,00",
      "Saldo R$ 0,00");

    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(account!.id), "account_id");
    form.Add(new ByteArrayContent(pdfBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") } }, "pdf", "extrato.pdf");

    var upload = await _client.PostAsync("/imports", form);
    upload.EnsureSuccessStatusCode();
    var uploadJson = await upload.Content.ReadFromJsonAsync<UploadResponse>();
    Assert.NotNull(uploadJson);

    // Inline job queue processes synchronously: import should be DONE and transactions created.
    var import = await _client.GetFromJsonAsync<ImportDto>($"/imports/{uploadJson!.importId}");
    Assert.NotNull(import);
    Assert.Equal(ImportStatus.Done, import!.status);

    var tx = await _client.GetFromJsonAsync<ListTransactionsResponse>("/transactions?from=2025-01-01T00:00:00Z&to=2025-01-31T23:59:59Z&page=1&page_size=50");
    Assert.NotNull(tx);
    Assert.Equal(2, tx!.items.Count);

    var summary = await _client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?month=2025-01-01");
    Assert.NotNull(summary);
    Assert.Equal(100m, summary!.incomeAmount);
    Assert.Equal(10m, summary!.checkingOutAmount);
  }

  [Fact]
  public async Task Transactions_filters_q_and_type()
  {
    // Seed directly in DB to test query behavior with Postgres ILIKE.
    await using var scope = _factory.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<Finance.Infrastructure.Persistence.FinanceDbContext>();

    var userId = await db.Users.Select(u => u.Id).SingleAsync();
    var accountId = Guid.NewGuid();
    db.Accounts.Add(new Account { Id = accountId, UserId = userId, Name = "Banco", Type = AccountType.Checking, Currency = "BRL", InitialBalance = 0m });

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        OccurredAt = new DateTimeOffset(2025, 01, 10, 0, 0, 0, TimeSpan.Zero),
        Description = "UBER TRIP",
        Notes = "corrida",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        OccurredAt = new DateTimeOffset(2025, 01, 10, 0, 0, 0, TimeSpan.Zero),
        Description = "SALARIO",
        Notes = null,
        Amount = 100m,
        Currency = "BRL",
        Fingerprint = "t2"
      });

    await db.SaveChangesAsync();

    var q = await _client.GetFromJsonAsync<ListTransactionsResponse>("/transactions?from=2025-01-01T00:00:00Z&to=2025-01-31T23:59:59Z&type=2&q=uber&page=1&page_size=50");
    Assert.NotNull(q);
    Assert.Single(q!.items);
    Assert.Contains("UBER", q.items[0].description);
  }

  private static byte[] FakePdfBytes(params string[] lines)
  {
    // Must start with %PDF for server-side validation.
    var text = "%PDF\n" + string.Join('\n', lines) + "\n";
    return System.Text.Encoding.UTF8.GetBytes(text);
  }

  private sealed record UploadResponse(Guid importId);

  // Minimal DTOs for integration assertions.
  private sealed record AccountDto(string id, string name, int type, string currency, decimal initialBalance, decimal? balance, decimal? creditCardSpendThisMonth);
  private sealed record ImportDto(Guid id, ImportStatus status, string? summaryJson, string? errorMessage, string createdAt, object? account);
  private sealed record ListTransactionsResponse(List<TxDto> items, int page, int pageSize, int totalCount);
  private sealed record TxDto(Guid id, string occurredAt, string description, string? notes, decimal amount);
  private sealed record DashboardSummaryDto(string month, decimal incomeAmount, decimal checkingOutAmount, decimal creditCardSpendAmount, decimal netCashAmount);
}

