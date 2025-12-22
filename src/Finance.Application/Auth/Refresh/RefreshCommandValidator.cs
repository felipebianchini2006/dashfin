using FluentValidation;

namespace Finance.Application.Auth.Refresh;

internal sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
  public RefreshCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

