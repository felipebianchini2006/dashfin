using FluentValidation;

namespace Finance.Application.Dashboard.Timeseries;

internal sealed class GetDashboardTimeseriesQueryValidator : AbstractValidator<GetDashboardTimeseriesQuery>
{
  public GetDashboardTimeseriesQueryValidator()
  {
    RuleFor(x => x.Month).NotEmpty();
  }
}

