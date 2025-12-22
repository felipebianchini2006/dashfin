using Finance.Domain.Enums;
using FluentValidation;

namespace Finance.Application.Imports.Rows;

internal sealed class ListImportRowsQueryValidator : AbstractValidator<ListImportRowsQuery>
{
  public ListImportRowsQueryValidator()
  {
    RuleFor(x => x.ImportId).NotEmpty();
    RuleFor(x => x.Status)
      .Must(s => s is null || s == ImportRowStatus.Error)
      .WithMessage("Only status=ERROR is supported.");
  }
}

