using Finance.Application.Common;

namespace Finance.Application.Common.Exceptions;

public sealed class AppException : Exception
{
  public AppException(Error error) : base(error.Message) => Error = error;
  public Error Error { get; }
}

