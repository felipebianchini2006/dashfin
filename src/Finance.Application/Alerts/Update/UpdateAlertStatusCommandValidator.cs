using Finance.Domain.Enums;
using FluentValidation;

namespace Finance.Application.Alerts.Update;

internal sealed class UpdateAlertStatusCommandValidator : AbstractValidator<UpdateAlertStatusCommand>
{
  public UpdateAlertStatusCommandValidator()
  {
    RuleFor(x => x.AlertId).NotEmpty();
    RuleFor(x => x.Status)
      .Must(s => s is AlertEventStatus.Read or AlertEventStatus.Dismissed)
      .WithMessage("Only READ or DISMISSED is allowed.");
  }
}

