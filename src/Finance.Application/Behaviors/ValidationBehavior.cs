using Finance.Application.Common;
using FluentValidation;
using MediatR;

namespace Finance.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private readonly IEnumerable<IValidator<TRequest>> _validators;

  public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
  {
    if (!_validators.Any())
      return await next();

    var context = new ValidationContext<TRequest>(request);
    var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
    var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToArray();

    if (failures.Length == 0)
      return await next();

    // Convention: commands/queries should return Result/Result<T>.
    // If you use a different response shape, map failures in API.
    var error = Error.Validation(string.Join("; ", failures.Select(f => f.ErrorMessage)));
    if (typeof(TResponse) == typeof(Result))
      return (TResponse)(object)Result.Fail(error);

    if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
    {
      var valueType = typeof(TResponse).GetGenericArguments()[0];
      var failMethod = typeof(Result)
        .GetMethods()
        .Single(m => m.Name == nameof(Result.Fail) && m.IsGenericMethodDefinition);
      var generic = failMethod.MakeGenericMethod(valueType);
      return (TResponse)generic.Invoke(null, new object[] { error })!;
    }

    throw new ValidationException(failures);
  }
}
