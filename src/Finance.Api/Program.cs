using System.Text;
using Finance.Application.Abstractions;
using Finance.Api.Auth;
using Finance.Api.Errors;
using Finance.Api.Middleware;
using Finance.Application;
using Finance.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
  cfg.ReadFrom.Configuration(ctx.Configuration);
  cfg.Enrich.FromLogContext();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddTransient<CorrelationIdMiddleware>();

builder.Services
  .AddApplication()
  .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));

builder.Services.AddCors(options =>
{
  options.AddPolicy("nextjs", policy =>
  {
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
    policy
      .WithOrigins(origins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
  options.CustomizeProblemDetails = ctx =>
  {
    ctx.ProblemDetails.Extensions["correlationId"] =
      ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId)
        ? correlationId
        : ctx.HttpContext.TraceIdentifier;
  };
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
  options.InvalidModelStateResponseFactory = context =>
  {
    var problem = new ValidationProblemDetails(context.ModelState)
    {
      Title = "Validation failed",
      Status = StatusCodes.Status400BadRequest
    };
    return new BadRequestObjectResult(problem);
  };
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey") ?? "CHANGE_ME_DEV_ONLY";
var issuer = jwtSection.GetValue<string>("Issuer") ?? "dashfin";
var audience = jwtSection.GetValue<string>("Audience") ?? "dashfin";

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidIssuer = issuer,
      ValidateAudience = true,
      ValidAudience = audience,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
      ValidateLifetime = true,
      ClockSkew = TimeSpan.FromSeconds(30)
    };
  });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
  var http = context.HttpContext;
  var correlationId =
    http.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid) ? cid : http.TraceIdentifier;

  var status = http.Response.StatusCode;
  var problem = new ProblemDetails
  {
    Title = ReasonPhrases.GetReasonPhrase(status),
    Status = status
  };
  problem.Extensions["correlationId"] = correlationId;

  http.Response.ContentType = "application/problem+json";
  await http.Response.WriteAsJsonAsync(problem);
});

app.UseCors("nextjs");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
