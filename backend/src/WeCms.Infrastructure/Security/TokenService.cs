using WeCms.Shared.Contracts;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WeCms.Infrastructure.Security;

public sealed class TokenService : ITokenService
{
    private readonly string _jwtSecret;
    private readonly int _accessTokenExpirySeconds;
    private const string Issuer = "wecms";
    private const string Audience = "wecms-admin";
    private readonly IClock _clock;

    public TokenService(string jwtSecret, IClock clock, int accessTokenExpirySeconds = 900)
    {
        _jwtSecret = jwtSecret;
        _clock = clock;
        _accessTokenExpirySeconds = accessTokenExpirySeconds;
    }

    public TokenPair GenerateTokenPair(TokenPrincipal principal)
    {
        var accessToken = GenerateAccessToken(principal);
        var refreshToken = GenerateRefreshToken();
        return new TokenPair(accessToken, refreshToken, _accessTokenExpirySeconds);
    }

    public TokenPrincipal? ValidateAccessToken(string accessToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var result = handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var userId = long.Parse(result.Claims.First(c => c.Type == "sub").Value);
            var username = result.Claims.First(c => c.Type == "username").Value;
            var securityStamp = result.Claims.First(c => c.Type == "security_stamp").Value;
            var permissionVersion = long.Parse(result.Claims.First(c => c.Type == "permission_version").Value);
            return new TokenPrincipal(userId, username, securityStamp, permissionVersion);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private string GenerateAccessToken(TokenPrincipal principal)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var claims = new[]
        {
            new Claim("sub", principal.UserId.ToString()),
            new Claim("username", principal.Username),
            new Claim("security_stamp", principal.SecurityStamp),
            new Claim("permission_version", principal.PermissionVersion.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: _clock.UtcNow.DateTime.AddSeconds(_accessTokenExpirySeconds),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
