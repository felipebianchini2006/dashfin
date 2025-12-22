using Finance.Application.Common;
using Finance.Application.Imports.Models;
using MediatR;

namespace Finance.Application.Imports.Get;

public sealed record GetImportQuery(Guid ImportId) : IRequest<Result<ImportDto>>;

