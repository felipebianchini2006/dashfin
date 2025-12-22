using FluentValidation;

namespace Finance.Application.Auth.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
  public LoginCommandValidator()
  {
    RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
  }
}

