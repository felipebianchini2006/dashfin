using Finance.Application.Abstractions;
using Finance.Application.Budgets.Models;
using Finance.Application.Common;
using Finance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Budgets.Upsert;

internal sealed class UpsertBudgetCommandHandler : IRequestHandler<UpsertBudgetCommand, Result<BudgetDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpsertBudgetCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<BudgetDto>> Handle(UpsertBudgetCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<BudgetDto>(Error.Unauthorized());

    var month = NormalizeMonth(request.Month);

    var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId.Value, ct);
    if (!categoryExists)
      return Result.Fail<BudgetDto>(Error.Validation("Category not found."));

    var budget = await _db.Budgets.SingleOrDefaultAsync(
      b => b.UserId == userId.Value && b.CategoryId == request.CategoryId && b.Month == month, ct);

    if (budget is null)
    {
      budget = new Budget
      {
        Id = Guid.NewGuid(),
        UserId = userId.Value,
        CategoryId = request.CategoryId,
        Month = month,
        LimitAmount = request.Amount
      };
      _db.Budgets.Add(budget);
    }
    else
    {
      budget.LimitAmount = request.Amount;
    }

    await _db.SaveChangesAsync(ct);

    var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var spent = await _db.Transactions
      .AsNoTracking()
      .Where(t =>
        t.UserId == userId.Value &&
        t.CategoryId == request.CategoryId &&
        t.OccurredAt >= monthStart &&
        t.OccurredAt < monthEnd &&
        t.Amount < 0m &&
        !t.IgnoreInDashboard)
      .Select(t => (decimal?)t.Amount)
      .SumAsync(ct) ?? 0m;

    var spentAmount = -spent;

    return Result.Ok(new BudgetDto(budget.Id, budget.CategoryId, budget.Month, budget.LimitAmount, spentAmount));
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

