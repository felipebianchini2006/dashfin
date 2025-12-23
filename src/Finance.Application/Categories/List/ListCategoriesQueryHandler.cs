using Finance.Application.Abstractions;
using Finance.Application.Categories.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Categories.List;

internal sealed class ListCategoriesQueryHandler : IRequestHandler<ListCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListCategoriesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(ListCategoriesQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<CategoryDto>>(Error.Unauthorized());

    var categories = await _db.Categories
      .AsNoTracking()
      .Where(c => c.UserId == userId.Value)
      .OrderBy(c => c.Name)
      .Select(c => new CategoryDto(c.Id, c.Name, c.ParentId, c.Color))
      .ToListAsync(ct);

    return Result.Ok<IReadOnlyList<CategoryDto>>(categories);
  }
}

