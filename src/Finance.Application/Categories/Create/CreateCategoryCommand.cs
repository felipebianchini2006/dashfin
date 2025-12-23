using Finance.Application.Categories.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Categories.Create;

public sealed record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<Result<CategoryDto>>;

