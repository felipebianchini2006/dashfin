using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Accounts.List;

public sealed record ListAccountsQuery : IRequest<Result<IReadOnlyList<AccountDto>>>;

