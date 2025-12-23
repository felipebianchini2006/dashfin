using Finance.Application.Categories.Create;
using Finance.Application.Categories.List;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class CategoriesHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task ListCategories_returns_only_current_user_categories()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    db.Categories.AddRange(
      new Category { Id = Guid.NewGuid(), UserId = userId, Name = "A" },
      new Category { Id = Guid.NewGuid(), UserId = userId, Name = "B" },
      new Category { Id = Guid.NewGuid(), UserId = otherUserId, Name = "X" });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new ListCategoriesQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.Value!.Count);
    Assert.DoesNotContain(result.Value!, c => c.Name == "X");
  }

  [Fact]
  public async Task CreateCategory_prevents_case_insensitive_duplicates()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();

    db.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" });
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new CreateCategoryCommandHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new CreateCategoryCommand("alimentação", null), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("conflict", result.Error!.Code);
  }

  [Fact]
  public async Task CreateCategory_validates_parent_is_owned_and_prevents_cycles()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    var parentOther = new Category { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Other" };
    db.Categories.Add(parentOther);

    var a = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "A" };
    var b = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "B", ParentId = a.Id };
    a.ParentId = b.Id; // intentionally create a cycle A -> B -> A
    db.Categories.AddRange(a, b);

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new CreateCategoryCommandHandler(db, new TestCurrentUser { UserId = userId });

    var crossUserParent = await handler.Handle(new CreateCategoryCommand("C", parentOther.Id), CancellationToken.None);
    Assert.True(crossUserParent.IsFailure);
    Assert.Equal("validation_error", crossUserParent.Error!.Code);

    var cycle = await handler.Handle(new CreateCategoryCommand("D", a.Id), CancellationToken.None);
    Assert.True(cycle.IsFailure);
    Assert.Equal("validation_error", cycle.Error!.Code);
  }
}

