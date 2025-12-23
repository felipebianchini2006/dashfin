using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Finance.Api.Swagger;

public sealed class AuthOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();
    if (hasAllowAnonymous)
      return;

    var hasAuthorize =
      context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
      context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

    if (!hasAuthorize)
      return;

    operation.Security ??= [];
    operation.Security.Add(new OpenApiSecurityRequirement
    {
      [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
  }
}

