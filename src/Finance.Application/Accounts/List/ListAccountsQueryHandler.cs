using Finance.Application.Abstractions;
using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Accounts.List;

internal sealed class ListAccountsQueryHandler : IRequestHandler<ListAccountsQuery, Result<IReadOnlyList<AccountDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;
  private readonly IClock _clock;

  public ListAccountsQueryHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
  {
    _db = db;
    _currentUser = currentUser;
    _clock = clock;
  }

  public async Task<Result<IReadOnlyList<AccountDto>>> Handle(ListAccountsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<AccountDto>>(Error.Unauthorized());

    var now = _clock.UtcNow;
    var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var accounts = await _db.Accounts
      .AsNoTracking()
      .Where(a => a.UserId == userId.Value)
      .OrderBy(a => a.CreatedAt)
      .Select(a => new
      {
        Account = a,
        TransactionsSum = _db.Transactions
          .Where(t => t.UserId == userId.Value && t.AccountId == a.Id)
          .Select(t => (decimal?)t.Amount)
          .Sum(),
        CreditCardMonthSpend = a.Type == AccountType.CreditCard
          ? _db.Transactions
            .Where(t =>
              t.UserId == userId.Value &&
              t.AccountId == a.Id &&
              t.OccurredAt >= monthStart &&
              t.OccurredAt < monthEnd &&
              t.Amount < 0)
            .Select(t => (decimal?)t.Amount)
            .Sum()
          : null
      })
      .ToListAsync(ct);

    var dtos = accounts
      .Select(x =>
      {
        var txSum = x.TransactionsSum ?? 0m;
        var balance = x.Account.Type is AccountType.Checking or AccountType.Savings
          ? x.Account.InitialBalance + txSum
          : (decimal?)null;

        var spend = x.CreditCardMonthSpend ?? 0m;
        var spendPositive = spend == 0m ? (decimal?)null : -spend;

        return new AccountDto(
          x.Account.Id,
          x.Account.Name,
          x.Account.Type,
          x.Account.Currency,
          x.Account.InitialBalance,
          balance,
          x.Account.Type == AccountType.CreditCard ? spendPositive : null);
      })
      .ToList();

    return Result.Ok<IReadOnlyList<AccountDto>>(dtos);
  }
}

