using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Finance.Api.Swagger;

public sealed class ExamplesOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var path = "/" + (context.ApiDescription.RelativePath ?? string.Empty).Trim('/');
    var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

    if (operation.RequestBody is not null &&
        operation.RequestBody.Content.TryGetValue("application/json", out var reqContent))
    {
      var reqExample = GetRequestExample(method, path);
      if (reqExample is not null)
        reqContent.Example = reqExample;
    }

    foreach (var (code, response) in operation.Responses)
    {
      if (!response.Content.TryGetValue("application/json", out var respContent) &&
          !response.Content.TryGetValue("application/problem+json", out respContent))
      {
        continue;
      }

      var respExample = GetResponseExample(method, path, code);
      if (respExample is not null)
        respContent.Example = respExample;
    }
  }

  private static IOpenApiAny? GetRequestExample(string method, string path)
  {
    return (method, path) switch
    {
      ("POST", "/auth/register") => Obj(
        ("email", "user@example.com"),
        ("password", "Password1")),
      ("POST", "/auth/login") => Obj(
        ("email", "user@example.com"),
        ("password", "Password1")),
      ("POST", "/accounts") => Obj(
        ("name", "Banco"),
        ("type", 1),
        ("initialBalance", 100.00m)),
      ("PATCH", var p) when p.StartsWith("/accounts/", StringComparison.Ordinal) => Obj(
        ("name", "Banco Principal"),
        ("initialBalance", 150.00m)),
      ("POST", "/categories") => Obj(
        ("name", "Alimentação"),
        ("parent_id", null)),
      ("POST", "/category-rules") => Obj(
        ("pattern", "UBER"),
        ("match_type", 1),
        ("category_id", "00000000-0000-0000-0000-000000000000"),
        ("priority", 50),
        ("is_active", true)),
      ("POST", "/budgets") => Obj(
        ("category_id", "00000000-0000-0000-0000-000000000000"),
        ("month", "2025-01-01"),
        ("amount", 300.00m)),
      ("PATCH", var p) when p.StartsWith("/transactions/", StringComparison.Ordinal) => Obj(
        ("category_id", "00000000-0000-0000-0000-000000000000"),
        ("notes", "Almoço"),
        ("ignore_in_dashboard", false)),
      ("PATCH", var p) when p.StartsWith("/alerts/", StringComparison.Ordinal) => Obj(
        ("status", 2)),
      _ => null
    };
  }

  private static IOpenApiAny? GetResponseExample(string method, string path, string statusCode)
  {
    if (statusCode.StartsWith("4", StringComparison.Ordinal) || statusCode.StartsWith("5", StringComparison.Ordinal))
    {
      if (statusCode == "400")
      {
        return Obj(
          ("type", "about:blank"),
          ("title", "Validation failed"),
          ("status", 400),
          ("detail", "One or more validation errors occurred."),
          ("traceId", "00-..."),
          ("code", "validation_error"),
          ("errors", Obj(
            ("field", Arr("message"))));
      }

      return Obj(
        ("type", "about:blank"),
        ("title", "Not Found"),
        ("status", int.TryParse(statusCode, out var s) ? s : 500),
        ("detail", "Not found"),
        ("traceId", "00-..."),
        ("code", "not_found"));
    }

    return (method, path, statusCode) switch
    {
      ("GET", "/me", "200") => Obj(
        ("email", "user@example.com"),
        ("timezone", "America/Sao_Paulo"),
        ("currency", "BRL"),
        ("displayPreferences", Obj(
          ("theme", "light"),
          ("compactMode", false)))),
      ("POST", "/auth/login", "200") => Obj(("accessToken", "access:...")),
      ("POST", "/auth/refresh", "200") => Obj(("accessToken", "access:...")),
      ("GET", "/accounts", "200") => Arr(
        Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("name", "Banco"),
          ("type", 1),
          ("currency", "BRL"),
          ("initialBalance", 100.00m),
          ("balance", 250.00m),
          ("creditCardSpendThisMonth", null))),
      ("POST", "/accounts", "201") => Obj(
        ("id", "00000000-0000-0000-0000-000000000000"),
        ("name", "Banco"),
        ("type", 1),
        ("currency", "BRL"),
        ("initialBalance", 100.00m),
        ("balance", 100.00m),
        ("creditCardSpendThisMonth", null)),
      ("GET", "/imports/{id}", "200") => Obj(
        ("id", "00000000-0000-0000-0000-000000000000"),
        ("accountId", "00000000-0000-0000-0000-000000000000"),
        ("status", "DONE"),
        ("fileName", "extrato.pdf"),
        ("processedAt", "2025-01-10T00:00:00+00:00"),
        ("summaryJson", "{\"counts\":{\"parsed\":10,\"inserted\":8,\"deduped\":2}}")),
      ("GET", "/imports/{id}/rows", "200") => Arr(
        Obj(
          ("id", 1),
          ("rowIndex", 1),
          ("pageNumber", 1),
          ("status", "ERROR"),
          ("errorCode", "parse_error"),
          ("errorMessage", "Invalid amount."),
          ("createdAt", "2025-01-10T00:00:00+00:00"))),
      ("GET", "/categories", "200") => Arr(
        Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("name", "Alimentação"),
          ("parentId", null),
          ("color", null))),
      ("POST", "/categories", "201") => Obj(
        ("id", "00000000-0000-0000-0000-000000000000"),
        ("name", "Alimentação"),
        ("parentId", null),
        ("color", null)),
      ("GET", "/category-rules", "200") => Arr(
        Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("matchType", 1),
          ("pattern", "UBER"),
          ("priority", 50),
          ("isActive", true),
          ("accountId", null),
          ("minAmount", null),
          ("maxAmount", null),
          ("createdAt", "2025-01-10T00:00:00+00:00"))),
      ("POST", "/category-rules", "201") => Obj(
        ("id", "00000000-0000-0000-0000-000000000000"),
        ("categoryId", "00000000-0000-0000-0000-000000000000"),
        ("matchType", 1),
        ("pattern", "UBER"),
        ("priority", 50),
        ("isActive", true),
        ("accountId", null),
        ("minAmount", null),
        ("maxAmount", null),
        ("createdAt", "2025-01-10T00:00:00+00:00")),
      ("GET", "/transactions", "200") => Obj(
        ("items", Arr(Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("accountId", "00000000-0000-0000-0000-000000000000"),
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("occurredAt", "2025-01-05T00:00:00+00:00"),
          ("description", "UBER TRIP"),
          ("notes", "work"),
          ("amount", -42.10m),
          ("currency", "BRL"),
          ("type", 2),
          ("ignoreInDashboard", false)))),
        ("page", 1),
        ("pageSize", 50),
        ("totalCount", 1)),
      ("PATCH", "/transactions/{id}", "200") => Obj(
        ("transaction", Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("accountId", "00000000-0000-0000-0000-000000000000"),
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("occurredAt", "2025-01-05T00:00:00+00:00"),
          ("description", "UBER TRIP"),
          ("notes", "work"),
          ("amount", -42.10m),
          ("currency", "BRL"),
          ("type", 2),
          ("ignoreInDashboard", false))),
        ("suggestedRule", Obj(
          ("reason", "transaction_recategorized"),
          ("pattern", "UBER TRIP"),
          ("matchType", 1),
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("priority", 100),
          ("isActive", true)))),
      ("GET", "/budgets", "200") => Arr(
        Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("month", "2025-01-01"),
          ("limitAmount", 300.00m),
          ("spentAmount", 120.00m))),
      ("POST", "/budgets", "200") => Obj(
        ("id", "00000000-0000-0000-0000-000000000000"),
        ("categoryId", "00000000-0000-0000-0000-000000000000"),
        ("month", "2025-01-01"),
        ("limitAmount", 300.00m),
        ("spentAmount", 120.00m)),
      ("GET", "/alerts", "200") => Arr(
        Obj(
          ("id", "00000000-0000-0000-0000-000000000000"),
          ("alertRuleId", "00000000-0000-0000-0000-000000000000"),
          ("fingerprint", new string('a', 64)),
          ("status", 1),
          ("occurredAt", "2025-01-15T00:00:00+00:00"),
          ("title", "Orçamento perto do limite: Alimentação (2025-01)"),
          ("body", "Gasto atual: 80.00 de 100.00 (80%)."),
          ("payloadJson", "{\"kind\":\"OVER_BUDGET_WARNING\"}"))),
      ("GET", "/dashboard/categories", "200") => Obj(
        ("month", "2025-01-01"),
        ("items", Arr(Obj(
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("categoryName", "Alimentação"),
          ("spentAmount", 150.00m))))),
      ("GET", "/dashboard/timeseries", "200") => Obj(
        ("month", "2025-01-01"),
        ("items", Arr(Obj(
          ("date", "2025-01-01"),
          ("spentAmount", 10.00m))))),
      ("GET", "/dashboard/balances", "200") => Obj(
        ("checkingAccounts", Arr(Obj(
          ("accountId", "00000000-0000-0000-0000-000000000000"),
          ("name", "Banco"),
          ("currency", "BRL"),
          ("balance", 120.00m)))),
        ("totalSaved", 60.00m),
        ("creditCardOpen", 30.00m),
        ("netWorth", 150.00m)),
      ("GET", "/dashboard/summary", "200") => Obj(
        ("month", "2025-01-01"),
        ("incomeAmount", 500.00m),
        ("checkingOutAmount", 200.00m),
        ("creditCardSpendAmount", 300.00m),
        ("netCashAmount", 300.00m),
        ("topCategories", Arr(Obj(
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("categoryName", "Alimentação"),
          ("spentAmount", 150.00m)))),
        ("budgetProgress", Arr(Obj(
          ("categoryId", "00000000-0000-0000-0000-000000000000"),
          ("categoryName", "Alimentação"),
          ("spentAmount", 150.00m),
          ("limitAmount", 200.00m),
          ("progressPercent", 75.0m),
          ("isOverBudget", false)))),
      _ => null
    };
  }

  private static OpenApiObject Obj(params (string Key, object? Value)[] props)
  {
    var o = new OpenApiObject();
    foreach (var (key, value) in props)
      o[key] = Any(value);
    return o;
  }

  private static OpenApiArray Arr(params OpenApiObject[] items)
  {
    var a = new OpenApiArray();
    foreach (var i in items)
      a.Add(i);
    return a;
  }

  private static IOpenApiAny Any(object? value)
  {
    return value switch
    {
      null => new OpenApiNull(),
      string s => new OpenApiString(s),
      bool b => new OpenApiBoolean(b),
      int i => new OpenApiInteger(i),
      long l => new OpenApiLong(l),
      decimal d => new OpenApiDouble((double)d),
      double d => new OpenApiDouble(d),
      OpenApiObject o => o,
      OpenApiArray a => a,
      _ => new OpenApiString(value.ToString() ?? string.Empty)
    };
  }
}
