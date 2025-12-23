using Finance.Application.Categories.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Categories.List;

public sealed record ListCategoriesQuery() : IRequest<Result<IReadOnlyList<CategoryDto>>>;

