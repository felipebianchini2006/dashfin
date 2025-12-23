using FluentValidation;

namespace Finance.Application.Auth.Update;

internal sealed class UpdateMeCommandValidator : AbstractValidator<UpdateMeCommand>
{
  private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase)
  {
    "system",
    "light",
    "dark"
  };

  public UpdateMeCommandValidator()
  {
    RuleFor(x => x)
      .Must(x => x.Timezone is not null || x.Currency is not null || x.Theme is not null || x.CompactMode is not null)
      .WithMessage("At least one field must be provided.");

    RuleFor(x => x.Timezone).MaximumLength(64);
    RuleFor(x => x.Currency).MaximumLength(8);

    RuleFor(x => x.Theme)
      .Must(t => t is null || AllowedThemes.Contains(t.Trim()))
      .WithMessage("theme must be system|light|dark.");
  }
}

