using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Imports.Upload;

public sealed record UploadImportCommand(
  Guid AccountId,
  string FileName,
  string ContentType,
  long? Length,
  Stream Content) : IRequest<Result<Guid>>;

