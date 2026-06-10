using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WeCms.Shared.Security;

namespace WeCms.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirySeconds;

    public JwtTokenService(string signingKey, string issuer, string audience, int expirySeconds)
    {
        _signingKey = signingKey;
        _issuer = issuer;
        _audience = audience;
        _expirySeconds = expirySeconds;
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
            expires: DateTime.UtcNow.AddSeconds(_expirySeconds),
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
