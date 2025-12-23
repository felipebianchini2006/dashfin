using FluentValidation;

namespace Finance.Application.Budgets.Get;

internal sealed class GetBudgetsQueryValidator : AbstractValidator<GetBudgetsQuery>
{
  public GetBudgetsQueryValidator()
  {
    RuleFor(x => x.Month).NotEmpty();
  }
}

