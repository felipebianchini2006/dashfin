using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Alerts.Update;

public sealed record UpdateAlertStatusCommand(Guid AlertId, AlertEventStatus Status) : IRequest<Result>;

