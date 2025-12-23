using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Transactions.Models;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Transactions.Update;

internal sealed class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, Result<UpdateTransactionResultDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpdateTransactionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<UpdateTransactionResultDto>> Handle(UpdateTransactionCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<UpdateTransactionResultDto>(Error.Unauthorized());

    var tx = await _db.Transactions.SingleOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == userId.Value, ct);
    if (tx is null)
      return Result.Fail<UpdateTransactionResultDto>(Error.NotFound("Transaction not found."));

    var categoryChanged = false;
    if (request.CategoryId is not null)
    {
      if (request.CategoryId.Value == Guid.Empty)
      {
        categoryChanged = tx.CategoryId is not null;
        tx.CategoryId = null;
      }
      else
      {
        var exists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId.Value, ct);
        if (!exists)
          return Result.Fail<UpdateTransactionResultDto>(Error.Validation("Category not found."));
        categoryChanged = tx.CategoryId != request.CategoryId.Value;
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

    CategoryRuleSuggestionDto? suggestion = null;
    if (categoryChanged && tx.CategoryId is not null)
    {
      var pattern = CategoryRuleSuggestionBuilder.BuildContainsPattern(tx.Description);
      if (pattern is not null)
      {
        suggestion = new CategoryRuleSuggestionDto(
          Reason: "transaction_recategorized",
          Pattern: pattern,
          MatchType: CategoryRuleMatchType.Contains,
          CategoryId: tx.CategoryId.Value,
          Priority: 100,
          IsActive: true);
      }
    }

    var dto = new TransactionDto(
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

    return Result.Ok(new UpdateTransactionResultDto(dto, suggestion));
  }
}
