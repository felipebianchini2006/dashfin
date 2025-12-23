using FluentValidation;

namespace Finance.Application.Dashboard.Summary;

internal sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
  public GetDashboardSummaryQueryValidator()
  {
    RuleFor(x => x.Month).NotEmpty();
  }
}

