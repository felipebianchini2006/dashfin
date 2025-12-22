using Finance.Api.Auth;
using Finance.Application.Auth.Login;
using Finance.Application.Auth.Logout;
using Finance.Application.Auth.Refresh;
using Finance.Application.Auth.Register;
using Finance.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Finance.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
  private readonly IMediator _mediator;
  private readonly AuthCookieOptions _cookieOptions;

  public AuthController(IMediator mediator, IOptions<AuthCookieOptions> cookieOptions)
  {
    _mediator = mediator;
    _cookieOptions = cookieOptions.Value;
  }

  public sealed record RegisterRequest(string Email, string Password);
  public sealed record LoginRequest(string Email, string Password);
  public sealed record AccessTokenResponse(string AccessToken);

  [HttpPost("register")]
  [AllowAnonymous]
  public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Created("/me", result.Value);
  }

  [HttpPost("login")]
  [AllowAnonymous]
  public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    SetRefreshTokenCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
    return Ok(new AccessTokenResponse(result.Value.AccessToken));
  }

  [HttpPost("refresh")]
  [AllowAnonymous]
  public async Task<IActionResult> Refresh(CancellationToken ct)
  {
    if (!Request.Cookies.TryGetValue(_cookieOptions.RefreshTokenName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
      return Unauthorized();

    var result = await _mediator.Send(new RefreshCommand(refreshToken), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    SetRefreshTokenCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
    return Ok(new AccessTokenResponse(result.Value.AccessToken));
  }

  [HttpPost("logout")]
  [AllowAnonymous]
  public async Task<IActionResult> Logout(CancellationToken ct)
  {
    if (Request.Cookies.TryGetValue(_cookieOptions.RefreshTokenName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
    {
      var result = await _mediator.Send(new LogoutCommand(refreshToken), ct);
      if (result.IsFailure)
        throw new AppException(result.Error!);
    }

    DeleteRefreshTokenCookie();
    return NoContent();
  }

  private void SetRefreshTokenCookie(string refreshToken, DateTimeOffset expiresAt)
  {
    Response.Cookies.Append(
      _cookieOptions.RefreshTokenName,
      refreshToken,
      new CookieOptions
      {
        HttpOnly = true,
        Secure = _cookieOptions.RefreshTokenSecure,
        SameSite = _cookieOptions.RefreshTokenSameSite,
        Path = _cookieOptions.RefreshTokenPath,
        Domain = _cookieOptions.RefreshTokenDomain,
        Expires = expiresAt
      });
  }

  private void DeleteRefreshTokenCookie()
  {
    Response.Cookies.Delete(
      _cookieOptions.RefreshTokenName,
      new CookieOptions
      {
        Secure = _cookieOptions.RefreshTokenSecure,
        SameSite = _cookieOptions.RefreshTokenSameSite,
        Path = _cookieOptions.RefreshTokenPath,
        Domain = _cookieOptions.RefreshTokenDomain
      });
  }
}
