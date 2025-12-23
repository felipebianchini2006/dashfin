using Finance.Domain.Enums;
using FluentValidation;

namespace Finance.Application.CategoryRules.Create;

internal sealed class CreateCategoryRuleCommandValidator : AbstractValidator<CreateCategoryRuleCommand>
{
  public CreateCategoryRuleCommandValidator()
  {
    RuleFor(x => x.Pattern).NotEmpty().MaximumLength(200);
    RuleFor(x => x.MatchType).IsInEnum();
    RuleFor(x => x.CategoryId).NotEmpty();
    RuleFor(x => x.Priority).InclusiveBetween(0, 10000);
  }
}

