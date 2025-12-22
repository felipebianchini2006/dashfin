using Finance.Application.Abstractions;
using Finance.Application.Imports.Get;
using Finance.Application.Imports.Rows;
using Finance.Application.Imports.Upload;
using Finance.Application.Tests.Fakes;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Finance.Application.Tests;

public sealed class ImportsHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task UploadImport_happy_path_creates_import_and_enqueues_job()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };

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
    await db.SaveChangesAsync(CancellationToken.None);

    var storage = new FakeFileStorage { Provider = "local" };
    var jobs = new FakeImportJobQueue();
    var opts = Options.Create(new ImportUploadOptions { MaxUploadBytes = 1024 * 1024 });

    var pdfBytes = "%PDF-1.4\nhello"u8.ToArray();
    await using var content = new MemoryStream(pdfBytes);

    var handler = new UploadImportCommandHandler(
      db,
      currentUser,
      storage,
      jobs,
      opts,
      NullLogger<UploadImportCommandHandler>.Instance);

    var result = await handler.Handle(new UploadImportCommand(account.Id, "file.pdf", "application/pdf", pdfBytes.Length, content), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Single(jobs.Enqueued);
    Assert.Equal(result.Value, jobs.Enqueued[0]);

    var import = await db.Imports.SingleAsync(i => i.Id == result.Value);
    Assert.Equal(ImportStatus.Uploaded, import.Status);
    Assert.Equal(account.Id, import.AccountId);
    Assert.Equal("local", import.StorageProvider);
    Assert.True(storage.Contains(import.StorageKey));
  }

  [Fact]
  public async Task UploadImport_invalid_pdf_header_returns_validation_error()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };

    var account = new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Checking, Name = "Banco", Currency = "BRL" };
    db.Accounts.Add(account);
    await db.SaveChangesAsync(CancellationToken.None);

    var storage = new FakeFileStorage();
    var jobs = new FakeImportJobQueue();
    var opts = Options.Create(new ImportUploadOptions { MaxUploadBytes = 1024 * 1024 });

    var bytes = "NOTPDF"u8.ToArray();
    await using var content = new MemoryStream(bytes);

    var handler = new UploadImportCommandHandler(
      db,
      currentUser,
      storage,
      jobs,
      opts,
      NullLogger<UploadImportCommandHandler>.Instance);

    var result = await handler.Handle(new UploadImportCommand(account.Id, "file.pdf", "application/pdf", bytes.Length, content), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("validation_error", result.Error!.Code);
    Assert.Empty(jobs.Enqueued);
  }

  [Fact]
  public async Task UploadImport_account_not_owned_returns_forbidden()
  {
    await using var db = CreateDb();
    var currentUser = new TestCurrentUser { UserId = Guid.NewGuid() };
    var otherUserId = Guid.NewGuid();

    var account = new Account { Id = Guid.NewGuid(), UserId = otherUserId, Type = AccountType.Checking, Name = "Banco", Currency = "BRL" };
    db.Accounts.Add(account);
    await db.SaveChangesAsync(CancellationToken.None);

    var storage = new FakeFileStorage();
    var jobs = new FakeImportJobQueue();
    var opts = Options.Create(new ImportUploadOptions());

    var bytes = "%PDF-1.4"u8.ToArray();
    await using var content = new MemoryStream(bytes);

    var handler = new UploadImportCommandHandler(
      db,
      currentUser,
      storage,
      jobs,
      opts,
      NullLogger<UploadImportCommandHandler>.Instance);

    var result = await handler.Handle(new UploadImportCommand(account.Id, "file.pdf", "application/pdf", bytes.Length, content), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("forbidden", result.Error!.Code);
  }

  [Fact]
  public async Task GetImport_returns_account_info()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };

    var account = new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Checking, Name = "Banco", Currency = "BRL" };
    var import = new ImportBatch
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AccountId = account.Id,
      Status = ImportStatus.Uploaded,
      FileName = "file.pdf",
      FileSha256 = new string('a', 64),
      StorageProvider = "local",
      StorageKey = "k"
    };
    db.Accounts.Add(account);
    db.Imports.Add(import);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetImportQueryHandler(db, currentUser);
    var result = await handler.Handle(new GetImportQuery(import.Id), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value!.Account);
    Assert.Equal(account.Id, result.Value.Account!.Id);
  }

  [Fact]
  public async Task ListImportRows_filters_error_only()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };
    var importId = Guid.NewGuid();

    db.Imports.Add(new ImportBatch
    {
      Id = importId,
      UserId = userId,
      Status = ImportStatus.Processing,
      FileName = "f.pdf",
      FileSha256 = new string('a', 64),
      StorageProvider = "local",
      StorageKey = "k"
    });

    db.ImportRows.AddRange(
      new ImportRow { ImportId = importId, UserId = userId, RowIndex = 1, Status = ImportRowStatus.Error, ErrorMessage = "bad" },
      new ImportRow { ImportId = importId, UserId = userId, RowIndex = 2, Status = ImportRowStatus.Parsed },
      new ImportRow { ImportId = importId, UserId = userId, RowIndex = 3, Status = ImportRowStatus.Error, ErrorMessage = "bad2" }
    );

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new ListImportRowsQueryHandler(db, currentUser);
    var result = await handler.Handle(new ListImportRowsQuery(importId, ImportRowStatus.Error), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.Value!.Count);
    Assert.All(result.Value, r => Assert.Equal(ImportRowStatus.Error, r.Status));
  }
}

