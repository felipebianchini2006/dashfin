using FluentValidation;

namespace Finance.Application.Budgets.Upsert;

internal sealed class UpsertBudgetCommandValidator : AbstractValidator<UpsertBudgetCommand>
{
  public UpsertBudgetCommandValidator()
  {
    RuleFor(x => x.CategoryId).NotEmpty();
    RuleFor(x => x.Amount)
      .Must(x => x >= 0m && x <= 999999999999m)
      .WithMessage("amount is out of range.");
  }
}

