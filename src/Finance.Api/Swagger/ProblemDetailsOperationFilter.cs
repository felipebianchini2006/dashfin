using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Finance.Api.Swagger;

public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    Add(operation, context, "400", typeof(ValidationProblemDetails));
    Add(operation, context, "401", typeof(ProblemDetails));
    Add(operation, context, "403", typeof(ProblemDetails));
    Add(operation, context, "404", typeof(ProblemDetails));
    Add(operation, context, "409", typeof(ProblemDetails));
    Add(operation, context, "500", typeof(ProblemDetails));
  }

  private static void Add(OpenApiOperation operation, OperationFilterContext context, string status, Type type)
  {
    if (operation.Responses.ContainsKey(status))
      return;

    var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

    operation.Responses[status] = new OpenApiResponse
    {
      Description = status,
      Content =
      {
        ["application/problem+json"] = new OpenApiMediaType
        {
          Schema = schema
        }
      }
    };
  }
}
