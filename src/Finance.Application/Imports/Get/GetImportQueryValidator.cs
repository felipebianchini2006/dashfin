using FluentValidation;

namespace Finance.Application.Imports.Get;

internal sealed class GetImportQueryValidator : AbstractValidator<GetImportQuery>
{
  public GetImportQueryValidator() => RuleFor(x => x.ImportId).NotEmpty();
}

