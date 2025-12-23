using FluentValidation;

namespace Finance.Application.Alerts.List;

internal sealed class ListAlertsQueryValidator : AbstractValidator<ListAlertsQuery>
{
  public ListAlertsQueryValidator()
  {
    RuleFor(x => x.Status)
      .Must(s => s is null || s == Finance.Domain.Enums.AlertEventStatus.New || s == Finance.Domain.Enums.AlertEventStatus.Read || s == Finance.Domain.Enums.AlertEventStatus.Dismissed)
      .WithMessage("Invalid status.");
  }
}

