using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Transactions.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Transactions.Update;

internal sealed class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpdateTransactionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<TransactionDto>> Handle(UpdateTransactionCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<TransactionDto>(Error.Unauthorized());

    var tx = await _db.Transactions.SingleOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == userId.Value, ct);
    if (tx is null)
      return Result.Fail<TransactionDto>(Error.NotFound("Transaction not found."));

    if (request.CategoryId is not null)
    {
      if (request.CategoryId.Value == Guid.Empty)
      {
        tx.CategoryId = null;
      }
      else
      {
        var exists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId.Value, ct);
        if (!exists)
          return Result.Fail<TransactionDto>(Error.Validation("Category not found."));
        tx.CategoryId = request.CategoryId.Value;
      }
    }

    if (request.Notes is not null)
    {
      var trimmed = request.Notes.Trim();
      tx.Notes = trimmed.Length == 0 ? null : trimmed;
    }

    if (request.IgnoreInDashboard is not null)
      tx.IgnoreInDashboard = request.IgnoreInDashboard.Value;

    await _db.SaveChangesAsync(ct);

    return Result.Ok(new TransactionDto(
      tx.Id,
      tx.AccountId,
      tx.CategoryId,
      tx.OccurredAt,
      tx.Description,
      tx.Notes,
      tx.Amount,
      tx.Currency,
      tx.Amount >= 0m ? TransactionFlow.Entrada : TransactionFlow.Saida,
      tx.IgnoreInDashboard));
  }
}

