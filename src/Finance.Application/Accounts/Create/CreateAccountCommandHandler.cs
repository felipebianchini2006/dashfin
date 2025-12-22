using Finance.Application.Abstractions;
using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Accounts.Create;

internal sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public CreateAccountCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<AccountDto>> Handle(CreateAccountCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<AccountDto>(Error.Unauthorized());

    var initial = request.InitialBalance ?? 0m;
    if (request.Type == AccountType.CreditCard)
      initial = 0m;

    var account = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId.Value,
      Type = request.Type,
      Name = request.Name,
      Currency = "BRL",
      InitialBalance = initial
    };

    _db.Accounts.Add(account);
    await _db.SaveChangesAsync(ct);

    return Result.Ok(new AccountDto(
      account.Id,
      account.Name,
      account.Type,
      account.Currency,
      account.InitialBalance,
      account.Type is AccountType.Checking or AccountType.Savings ? account.InitialBalance : null,
      null));
  }
}

