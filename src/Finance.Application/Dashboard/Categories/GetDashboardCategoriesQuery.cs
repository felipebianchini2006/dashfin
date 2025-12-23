using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Dashboard.Categories;

public sealed record GetDashboardCategoriesQuery(DateOnly Month) : IRequest<Result<DashboardCategoriesDto>>;

