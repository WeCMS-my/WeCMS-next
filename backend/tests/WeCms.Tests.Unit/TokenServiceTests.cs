using WeCms.Infrastructure;
using WeCms.Infrastructure.Security;
using WeCms.Shared.Contracts;
using Xunit;

namespace WeCms.Tests.Unit;

public class TokenServiceTests
{
    private const string Secret = "test-secret-at-least-32-chars-long!!";
    private readonly IClock _clock = new SystemClock();
    private readonly ITokenService _svc = new TokenService(Secret, new SystemClock(), 900);

    [Fact]
    public void GenerateTokenPair_ShouldReturnAccessAndRefreshTokens()
    {
        var pair = _svc.GenerateTokenPair(new TokenPrincipal(1, "admin", "stamp1", 1));

        Assert.NotNull(pair.AccessToken);
        Assert.NotEmpty(pair.AccessToken);
        Assert.NotNull(pair.RefreshToken);
        Assert.NotEmpty(pair.RefreshToken);
        Assert.Equal(900, pair.ExpiresIn);
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnPrincipal_WhenTokenIsValid()
    {
        var pair = _svc.GenerateTokenPair(new TokenPrincipal(1, "admin", "stamp1", 1));
        var principal = _svc.ValidateAccessToken(pair.AccessToken);

        Assert.NotNull(principal);
        Assert.Equal(1, principal.UserId);
        Assert.Equal("admin", principal.Username);
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnNull_WhenTokenIsTampered()
    {
        var pair = _svc.GenerateTokenPair(new TokenPrincipal(1, "admin", "stamp1", 1));
        var result = _svc.ValidateAccessToken(pair.AccessToken + "x");

        Assert.Null(result);
    }

    [Fact]
    public void GenerateTokenPair_ShouldIncludeClaims()
    {
        var pair = _svc.GenerateTokenPair(new TokenPrincipal(2, "super", "s2", 5));
        var principal = _svc.ValidateAccessToken(pair.AccessToken);

        Assert.NotNull(principal);
        Assert.Equal(2, principal.UserId);
        Assert.Equal("super", principal.Username);
        Assert.Equal("s2", principal.SecurityStamp);
        Assert.Equal(5, principal.PermissionVersion);
    }

    [Fact]
    public void RefreshTokens_ShouldBeDifferentEachTime()
    {
        var p1 = _svc.GenerateTokenPair(new TokenPrincipal(1, "a", "s", 1));
        var p2 = _svc.GenerateTokenPair(new TokenPrincipal(1, "a", "s", 1));

        Assert.NotEqual(p1.RefreshToken, p2.RefreshToken);
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnNull_WhenTokenIsInvalid()
    {
        var result = _svc.ValidateAccessToken("not-a-valid-jwt-token");

        Assert.Null(result);
    }
}
