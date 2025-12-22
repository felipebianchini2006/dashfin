namespace Finance.Application.Common;

public sealed record Error(string Code, string Message)
{
  public static Error Validation(string message) => new("validation_error", message);
  public static Error Unauthorized(string message = "Unauthorized") => new("unauthorized", message);
  public static Error Forbidden(string message = "Forbidden") => new("forbidden", message);
  public static Error NotFound(string message = "Not found") => new("not_found", message);
  public static Error Conflict(string message = "Conflict") => new("conflict", message);
  public static Error Unexpected(string message = "Unexpected error") => new("unexpected", message);
}

