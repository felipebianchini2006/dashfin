using Finance.Application.Abstractions;
using Finance.Application.Categories.Models;
using Finance.Application.Common;
using Finance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Categories.Create;

internal sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public CreateCategoryCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<CategoryDto>(Error.Unauthorized());

    var name = request.Name.Trim();
    if (name.Length == 0)
      return Result.Fail<CategoryDto>(Error.Validation("Name is required."));

    var nameKey = name.ToUpperInvariant();
    var exists = await _db.Categories
      .AsNoTracking()
      .AnyAsync(c => c.UserId == userId.Value && c.Name.ToUpper() == nameKey, ct);
    if (exists)
      return Result.Fail<CategoryDto>(Error.Conflict("Category name already exists."));

    Guid? parentId = request.ParentId;
    if (parentId == Guid.Empty)
      parentId = null;

    if (parentId is not null)
    {
      var parent = await _db.Categories.SingleOrDefaultAsync(c => c.Id == parentId.Value && c.UserId == userId.Value, ct);
      if (parent is null)
        return Result.Fail<CategoryDto>(Error.Validation("Parent category not found."));

      var visited = new HashSet<Guid>();
      var cursor = parentId;
      while (cursor is not null)
      {
        if (!visited.Add(cursor.Value))
          return Result.Fail<CategoryDto>(Error.Validation("Invalid category hierarchy (cycle detected)."));

        var node = await _db.Categories
          .AsNoTracking()
          .Where(c => c.Id == cursor.Value && c.UserId == userId.Value)
          .Select(c => new { c.Id, c.ParentId })
          .SingleOrDefaultAsync(ct);

        if (node is null)
          return Result.Fail<CategoryDto>(Error.Validation("Invalid category hierarchy."));

        cursor = node.ParentId;
      }
    }

    var category = new Category
    {
      Id = Guid.NewGuid(),
      UserId = userId.Value,
      Name = name,
      ParentId = parentId,
      Color = null
    };

    _db.Categories.Add(category);
    await _db.SaveChangesAsync(ct);

    return Result.Ok(new CategoryDto(category.Id, category.Name, category.ParentId, category.Color));
  }
}
