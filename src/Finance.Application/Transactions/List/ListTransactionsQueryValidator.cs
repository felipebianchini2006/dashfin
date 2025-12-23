using FluentValidation;

namespace Finance.Application.Transactions.List;

internal sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
  public ListTransactionsQueryValidator()
  {
    RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

    RuleFor(x => x)
      .Must(x => x.From is null || x.To is null || x.From <= x.To)
      .WithMessage("from must be <= to.");

    RuleFor(x => x.MinAmount)
      .Must(x => x is null || x.Value >= 0m)
      .WithMessage("min must be >= 0.");

    RuleFor(x => x.MaxAmount)
      .Must(x => x is null || x.Value >= 0m)
      .WithMessage("max must be >= 0.");

    RuleFor(x => x)
      .Must(x => x.MinAmount is null || x.MaxAmount is null || x.MinAmount <= x.MaxAmount)
      .WithMessage("min must be <= max.");

    RuleFor(x => x.Search).MaximumLength(200);
  }
}

