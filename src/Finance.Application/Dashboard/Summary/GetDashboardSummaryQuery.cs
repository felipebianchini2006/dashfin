using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Dashboard.Summary;

public sealed record GetDashboardSummaryQuery(DateOnly Month) : IRequest<Result<DashboardSummaryDto>>;

