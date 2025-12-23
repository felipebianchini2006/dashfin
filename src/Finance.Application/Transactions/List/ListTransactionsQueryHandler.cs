using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Transactions.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Finance.Application.Transactions.List;

internal sealed class ListTransactionsQueryHandler : IRequestHandler<ListTransactionsQuery, Result<ListTransactionsResponse>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListTransactionsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<ListTransactionsResponse>> Handle(ListTransactionsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<ListTransactionsResponse>(Error.Unauthorized());

    var query = _db.Transactions
      .AsNoTracking()
      .Where(t => t.UserId == userId.Value);

    if (request.From is not null)
      query = query.Where(t => t.OccurredAt >= request.From.Value);
    if (request.To is not null)
      query = query.Where(t => t.OccurredAt <= request.To.Value);

    if (request.AccountId is not null)
      query = query.Where(t => t.AccountId == request.AccountId.Value);

    if (request.CategoryId is not null)
      query = query.Where(t => t.CategoryId == request.CategoryId.Value);

    if (request.Type is not null)
    {
      query = request.Type.Value switch
      {
        TransactionFlow.Entrada => query.Where(t => t.Amount > 0m),
        TransactionFlow.Saida => query.Where(t => t.Amount < 0m),
        _ => query
      };
    }

    if (request.MinAmount is not null)
      query = query.Where(t => Math.Abs(t.Amount) >= request.MinAmount.Value);
    if (request.MaxAmount is not null)
      query = query.Where(t => Math.Abs(t.Amount) <= request.MaxAmount.Value);

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var term = request.Search.Trim();
      if (term.Length != 0)
      {
        var provider = _db.Database.ProviderName ?? string.Empty;
        var isNpgsql = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        var pattern = $"%{term}%";
        var termUpper = term.ToUpper();

        query = isNpgsql
          ? query.Where(t =>
            EF.Functions.ILike(t.Description, pattern) ||
            (t.Notes != null && EF.Functions.ILike(t.Notes, pattern)))
          : query.Where(t =>
            t.Description.ToUpper().Contains(termUpper) ||
            (t.Notes != null && t.Notes.ToUpper().Contains(termUpper)));
      }
    }

    query = query.OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.CreatedAt);

    var total = await query.CountAsync(ct);
    var page = request.Page;
    var pageSize = request.PageSize;

    var items = await query
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(t => new TransactionDto(
        t.Id,
        t.AccountId,
        t.CategoryId,
        t.OccurredAt,
        t.Description,
        t.Notes,
        t.Amount,
        t.Currency,
        t.Amount >= 0m ? TransactionFlow.Entrada : TransactionFlow.Saida,
        t.IgnoreInDashboard))
      .ToListAsync(ct);

    return Result.Ok(new ListTransactionsResponse(items, page, pageSize, total));
  }
}
