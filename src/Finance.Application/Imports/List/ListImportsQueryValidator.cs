using FluentValidation;

namespace Finance.Application.Imports.List;

internal sealed class ListImportsQueryValidator : AbstractValidator<ListImportsQuery>
{
  public ListImportsQueryValidator()
  {
    RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
  }
}

