using Attendence_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Attendence_Management_System.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly JwtOptions _opts;

    public AuthApiController(TokenService tokenService, IOptions<JwtOptions> opts)
    {
        _tokenService = tokenService;
        _opts = opts.Value;
    }

    // Uses refresh token cookie to mint a new access token (sliding session)
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(_opts.RefreshCookieName, out var refreshPlain))
            return Unauthorized();

        var user = await _tokenService.ValidateRefreshTokenAsync(refreshPlain);
        if (user == null)
            return Unauthorized();

        var accessToken = await _tokenService.CreateAccessTokenAsync(user);

        Response.Cookies.Append("ams.access", accessToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
            IsEssential = true
        });

        // caller can also read token from response body if preferred
        return Ok(new { accessToken, expiresInMinutes = _opts.AccessTokenMinutes });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name = User.Identity?.Name,
            roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray()
        });
    }
}
