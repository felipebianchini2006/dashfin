using FluentValidation;

namespace Finance.Application.Dashboard.Get;

internal sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
  public GetDashboardQueryValidator()
  {
    RuleFor(x => x.Month).NotEmpty();
  }
}

