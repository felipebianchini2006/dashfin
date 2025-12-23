using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Imports.List;

public sealed record ListImportsQuery(
  ImportStatus? Status,
  int Page = 1,
  int PageSize = 50) : IRequest<Result<ListImportsResponse>>;

