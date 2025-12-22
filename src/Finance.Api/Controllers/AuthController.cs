using Finance.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
  private readonly ITokenService _tokens;

  public AuthController(ITokenService tokens) => _tokens = tokens;

  public sealed record LoginRequest(string Email, string Password);
  public sealed record LoginResponse(string AccessToken);

  [HttpPost("login")]
  public IActionResult Login([FromBody] LoginRequest request)
  {
    // Template only:
    // - validate credentials against your user store
    // - issue access token
    // - issue refresh token cookie (httpOnly, secure in prod) and persist hashed refresh token
    var userId = Guid.NewGuid(); // TODO
    var accessToken = _tokens.CreateAccessToken(userId, request.Email);
    var refreshToken = _tokens.CreateRefreshToken();

    Response.Cookies.Append(
      "refresh_token",
      refreshToken,
      new CookieOptions
      {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddDays(30)
      });

    return Ok(new LoginResponse(accessToken));
  }

  [HttpPost("refresh")]
  public IActionResult Refresh()
  {
    // Template only:
    // - read refresh_token cookie
    // - validate against stored hash+expiry
    // - rotate refresh token and issue new access token
    return Problem("Not implemented", statusCode: StatusCodes.Status501NotImplemented);
  }

  [HttpPost("logout")]
  public IActionResult Logout()
  {
    Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });
    return NoContent();
  }
}
