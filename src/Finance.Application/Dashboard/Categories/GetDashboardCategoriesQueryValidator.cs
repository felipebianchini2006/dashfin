using FluentValidation;

namespace Finance.Application.Dashboard.Categories;

internal sealed class GetDashboardCategoriesQueryValidator : AbstractValidator<GetDashboardCategoriesQuery>
{
  public GetDashboardCategoriesQueryValidator()
  {
    RuleFor(x => x.Month).NotEmpty();
  }
}

