using FluentValidation;

namespace Finance.Application.Transactions.Update;

internal sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
  public UpdateTransactionCommandValidator()
  {
    RuleFor(x => x.TransactionId).NotEmpty();

    RuleFor(x => x)
      .Must(x => x.CategoryId is not null || x.Notes is not null || x.IgnoreInDashboard is not null)
      .WithMessage("At least one field must be provided.");

    RuleFor(x => x.Notes).MaximumLength(2000);
  }
}

