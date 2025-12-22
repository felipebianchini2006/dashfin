using FluentValidation;

namespace Finance.Application.Auth.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
  public RegisterCommandValidator()
  {
    RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    RuleFor(x => x.Password)
      .NotEmpty()
      .MinimumLength(8)
      .MaximumLength(128)
      .Must(p => p.Any(char.IsLetter)).WithMessage("Password must contain a letter.")
      .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain a digit.");
  }
}

