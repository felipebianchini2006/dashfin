using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Dashboard.Balances;

internal sealed class GetDashboardBalancesQueryHandler : IRequestHandler<GetDashboardBalancesQuery, Result<DashboardBalancesDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetDashboardBalancesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<DashboardBalancesDto>> Handle(GetDashboardBalancesQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<DashboardBalancesDto>(Error.Unauthorized());

    var accounts = await _db.Accounts
      .AsNoTracking()
      .Where(a => a.UserId == userId.Value)
      .Select(a => new { a.Id, a.Name, a.Currency, a.Type, a.InitialBalance })
      .ToListAsync(ct);

    var sums = await _db.Transactions
      .AsNoTracking()
      .Where(t => t.UserId == userId.Value && !t.IgnoreInDashboard)
      .GroupBy(t => t.AccountId)
      .Select(g => new { AccountId = g.Key, Sum = g.Sum(x => x.Amount) })
      .ToListAsync(ct);

    var sumByAccount = sums.ToDictionary(x => x.AccountId, x => x.Sum);

    var checking = new List<AccountBalanceDto>();
    decimal checkingTotal = 0m;
    decimal savingsTotal = 0m;
    decimal creditCardOpen = 0m;

    foreach (var a in accounts)
    {
      var txSum = sumByAccount.GetValueOrDefault(a.Id, 0m);
      if (a.Type is AccountType.Checking or AccountType.Savings)
      {
        var balance = a.InitialBalance + txSum;
        if (a.Type == AccountType.Checking)
        {
          checking.Add(new AccountBalanceDto(a.Id, a.Name, a.Currency, balance));
          checkingTotal += balance;
        }
        else
        {
          savingsTotal += balance;
        }
      }
      else if (a.Type == AccountType.CreditCard)
      {
        creditCardOpen += -txSum;
      }
    }

    creditCardOpen = Math.Max(0m, creditCardOpen);
    var netWorth = checkingTotal + savingsTotal - creditCardOpen;

    return Result.Ok(new DashboardBalancesDto(
      checking.OrderBy(x => x.Name).ToList(),
      savingsTotal,
      accounts.Any(a => a.Type == AccountType.CreditCard) ? creditCardOpen : null,
      netWorth));
  }
}

