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

}
