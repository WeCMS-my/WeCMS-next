using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WeCms.Shared.Time;
using WeCms.Shared.Security;

namespace WeCms.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirySeconds;
    private readonly IClock _clock;

    public JwtTokenService(IConfiguration configuration, IClock clock)
    {
        _signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("配置缺失：Jwt:SigningKey");
        _issuer = configuration["Jwt:Issuer"] ?? "WeCMS";
        _audience = configuration["Jwt:Audience"] ?? "WeCMS";
        _expirySeconds = int.Parse(configuration["Jwt:AccessTokenExpirySeconds"] ?? "1800", CultureInfo.InvariantCulture);
        _clock = clock;
    }

    public string GenerateAccessToken(CurrentUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim("permissionVersion", user.PermissionVersion.ToString()),
            new Claim("securityStamp", user.SecurityStamp),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: _clock.UtcNow.AddSeconds(_expirySeconds).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public WeCms.Shared.Security.TokenValidationResult ValidateAccessToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var userId = long.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!);
            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value!;
            var displayName = principal.FindFirst("displayName")?.Value ?? "";
            var permissionVersion = int.Parse(principal.FindFirst("permissionVersion")?.Value ?? "0");
            var securityStamp = principal.FindFirst("securityStamp")?.Value ?? "";

            var user = new CurrentUser(userId, username, displayName, permissionVersion, securityStamp);
            return new WeCms.Shared.Security.TokenValidationResult(true, user);
        }
        catch
        {
            return new WeCms.Shared.Security.TokenValidationResult(false);
        }
    }
}
