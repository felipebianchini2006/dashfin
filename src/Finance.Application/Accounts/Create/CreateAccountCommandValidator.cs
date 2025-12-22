using Finance.Domain.Enums;
using FluentValidation;

namespace Finance.Application.Accounts.Create;

internal sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
  public CreateAccountCommandValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    RuleFor(x => x.Type).IsInEnum();
    RuleFor(x => x.InitialBalance)
      .Must(x => x is null || x.Value >= -999999999999m)
      .WithMessage("InitialBalance is out of range.");
  }
}

