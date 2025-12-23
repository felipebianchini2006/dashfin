using Finance.Application.Alerts.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Alerts.List;

public sealed record ListAlertsQuery(AlertEventStatus? Status) : IRequest<Result<IReadOnlyList<AlertEventDto>>>;

