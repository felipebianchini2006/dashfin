using Finance.Application.Abstractions;
using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Accounts.Update;

internal sealed class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Result<AccountDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpdateAccountCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<AccountDto>> Handle(UpdateAccountCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<AccountDto>(Error.Unauthorized());

    var account = await _db.Accounts.SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId.Value, ct);
    if (account is null)
      return Result.Fail<AccountDto>(Error.NotFound("Account not found."));

    if (request.Type is not null && request.Type.Value != account.Type)
    {
      var hasTx = await _db.Transactions.AnyAsync(t => t.UserId == userId.Value && t.AccountId == account.Id, ct);
      if (hasTx)
        return Result.Fail<AccountDto>(Error.Validation("Account type can only be changed when there are no transactions."));

      account.Type = request.Type.Value;
      if (account.Type == AccountType.CreditCard)
        account.InitialBalance = 0m;
    }

    if (request.Name is not null)
    {
      if (string.IsNullOrWhiteSpace(request.Name))
        return Result.Fail<AccountDto>(Error.Validation("Name is required."));
      account.Name = request.Name;
    }

    if (request.InitialBalance is not null)
    {
      if (account.Type == AccountType.CreditCard)
        return Result.Fail<AccountDto>(Error.Validation("InitialBalance can only be updated for CHECKING/SAVINGS."));
      account.InitialBalance = request.InitialBalance.Value;
    }

    await _db.SaveChangesAsync(ct);

    var txSum = await _db.Transactions
      .Where(t => t.UserId == userId.Value && t.AccountId == account.Id)
      .Select(t => (decimal?)t.Amount)
      .SumAsync(ct) ?? 0m;

    var balance = account.Type is AccountType.Checking or AccountType.Savings
      ? account.InitialBalance + txSum
      : (decimal?)null;

    return Result.Ok(new AccountDto(
      account.Id,
      account.Name,
      account.Type,
      account.Currency,
      account.InitialBalance,
      balance,
      null));
  }
}
