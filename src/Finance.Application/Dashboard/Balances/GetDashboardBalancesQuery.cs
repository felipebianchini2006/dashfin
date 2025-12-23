using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Dashboard.Balances;

public sealed record GetDashboardBalancesQuery() : IRequest<Result<DashboardBalancesDto>>;

