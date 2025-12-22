using Finance.Application.Common;
using Finance.Application.Imports.Models;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Imports.Rows;

public sealed record ListImportRowsQuery(Guid ImportId, ImportRowStatus? Status) : IRequest<Result<IReadOnlyList<ImportRowAuditDto>>>;

