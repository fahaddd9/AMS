using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Attendence_Management_System.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "AttendenceManagementSystem";
    public string Audience { get; set; } = "AttendenceManagementSystem";
    public string SigningKey { get; set; } = "CHANGE_ME_DEV_ONLY_CHANGE_ME_DEV_ONLY_CHANGE_ME";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public string RefreshCookieName { get; set; } = "ams.refresh";
}

public class TokenService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _opts;

    public TokenService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IOptions<JwtOptions> opts)
    {
        _db = db;
        _userManager = userManager;
        _opts = opts.Value;
    }

    public async Task<string> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<(string refreshTokenPlain, RefreshToken entity)> CreateAndStoreRefreshTokenAsync(ApplicationUser user)
    {
        var refreshTokenPlain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(refreshTokenPlain);

        var rt = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays)
        };

        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();

        return (refreshTokenPlain, rt);
    }

    public async Task<ApplicationUser?> ValidateRefreshTokenAsync(string refreshTokenPlain)
    {
        var hash = HashToken(refreshTokenPlain);

        var rt = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (rt == null) return null;
        if (rt.IsRevoked) return null;
        if (rt.ExpiresAtUtc <= DateTime.UtcNow) return null;

        return rt.User;
    }

    public async Task RevokeRefreshTokenAsync(string refreshTokenPlain)
    {
        var hash = HashToken(refreshTokenPlain);
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (rt == null) return;
        if (rt.RevokedAtUtc != null) return;
        rt.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public static string HashToken(string tokenPlain)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(tokenPlain));
        return Convert.ToBase64String(bytes);
    }
}
