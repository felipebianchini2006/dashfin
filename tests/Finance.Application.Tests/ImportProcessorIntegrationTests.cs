using Finance.Application.Imports.Processing;
using Finance.Application.Tests.Fakes;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Finance.Application.Tests;

public sealed class ImportProcessorIntegrationTests
{
  [Fact]
  public async Task ProcessImport_is_idempotent_across_imports_by_fingerprint()
  {
    await using var sqlite = new SqliteDb();
    var db = sqlite.Db;
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 01, 10, 0, 0, 0, TimeSpan.Zero) };

    var userId = Guid.NewGuid();
    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Transferências/Interno" };
    db.Categories.Add(category);
    db.CategoryRules.Add(new CategoryRule
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = category.Id,
      MatchType = CategoryRuleMatchType.Contains,
      Pattern = "PIX RECEBIDO",
      Priority = 1,
      IsActive = true
    });

    var account = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.Checking,
      Name = "Banco",
      Currency = "BRL",
      InitialBalance = 0m
    };
    db.Accounts.Add(account);

    var storage = new FakeFileStorage { Provider = "local" };
    var pdf = new FakePdfTextExtractor
    {
      Pages =
      [
        new PdfTextPage(1, "NuConta\n05/01/2025 PIX RECEBIDO R$ 100,00\n", new[] { "NuConta", "05/01/2025 PIX RECEBIDO R$ 100,00" })
      ]
    };

    var processor = new ImportProcessor(
      db,
      storage,
      pdf,
      clock,
      NullLogger<ImportProcessor>.Instance);

    async Task<Guid> AddImportAsync(string key)
    {
      var id = Guid.NewGuid();
      db.Imports.Add(new ImportBatch
      {
        Id = id,
        UserId = userId,
        AccountId = account.Id,
        Status = ImportStatus.Uploaded,
        FileName = "f.pdf",
        FileSha256 = new string('a', 64),
        StorageProvider = "local",
        StorageKey = key
      });

      await db.SaveChangesAsync(CancellationToken.None);
      await storage.SaveAsync(key, new MemoryStream("%PDF-1.4"u8.ToArray()), "application/pdf", CancellationToken.None);
      return id;
    }

    var import1 = await AddImportAsync("k1");
    var r1 = await processor.ProcessImportAsync(import1, CancellationToken.None);
    Assert.True(r1.IsSuccess);
    Assert.Equal(1, await db.Transactions.CountAsync());
    var tx = await db.Transactions.SingleAsync();
    Assert.Equal(category.Id, tx.CategoryId);

    var import2 = await AddImportAsync("k2");
    var r2 = await processor.ProcessImportAsync(import2, CancellationToken.None);
    Assert.True(r2.IsSuccess);
    Assert.Equal(1, await db.Transactions.CountAsync());

    var i2 = await db.Imports.SingleAsync(i => i.Id == import2);
    Assert.Equal(ImportStatus.Done, i2.Status);
  }
}
