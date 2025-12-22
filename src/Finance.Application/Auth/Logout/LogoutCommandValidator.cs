using FluentValidation;

namespace Finance.Application.Auth.Logout;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
  public LogoutCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

