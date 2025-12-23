using Finance.Application.Common;
using Finance.Application.Dashboard.Models;
using MediatR;

namespace Finance.Application.Dashboard.Get;

public sealed record GetDashboardQuery(DateOnly Month) : IRequest<Result<DashboardDto>>;

