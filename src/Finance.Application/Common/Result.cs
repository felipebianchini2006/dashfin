namespace Finance.Application.Common;

public class Result
{
  protected Result(bool isSuccess, Error? error)
  {
    IsSuccess = isSuccess;
    Error = error;
  }

  public bool IsSuccess { get; }
  public bool IsFailure => !IsSuccess;
  public Error? Error { get; }

  public static Result Ok() => new(true, null);
  public static Result Fail(Error error) => new(false, error);
  public static Result<T> Ok<T>(T value) => new(value, true, null);
  public static Result<T> Fail<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
  internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error) => Value = value;
  public T? Value { get; }
}

