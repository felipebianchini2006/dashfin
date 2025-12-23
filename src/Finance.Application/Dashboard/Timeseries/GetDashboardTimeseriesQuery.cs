using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Dashboard.Timeseries;

public sealed record GetDashboardTimeseriesQuery(DateOnly Month) : IRequest<Result<DashboardTimeseriesDto>>;

