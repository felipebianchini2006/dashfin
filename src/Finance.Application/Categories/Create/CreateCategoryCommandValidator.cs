using FluentValidation;

namespace Finance.Application.Categories.Create;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
  public CreateCategoryCommandValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
  }
}

