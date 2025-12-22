using Finance.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Finance.Application.Imports.Upload;

internal sealed class UploadImportCommandValidator : AbstractValidator<UploadImportCommand>
{
  public UploadImportCommandValidator(IOptions<ImportUploadOptions> options)
  {
    var max = options.Value.MaxUploadBytes;

    RuleFor(x => x.AccountId).NotEmpty();
    RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
    RuleFor(x => x.ContentType).NotEmpty().Equal("application/pdf");
    RuleFor(x => x.Length)
      .Must(len => len is null || len.Value > 0)
      .WithMessage("File is required.");
    RuleFor(x => x.Length)
      .Must(len => len is null || len.Value <= max)
      .WithMessage($"Max upload size is {max} bytes.");
  }
}

