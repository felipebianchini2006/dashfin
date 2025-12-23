using FluentValidation;

namespace Finance.Application.Accounts.Update;

internal sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
  public UpdateAccountCommandValidator()
  {
    RuleFor(x => x.AccountId).NotEmpty();
    RuleFor(x => x.Name).MaximumLength(200);
    RuleFor(x => x)
      .Must(x => x.Name is not null || x.Type is not null || x.InitialBalance is not null)
      .WithMessage("At least one field must be provided.");
  }
}
