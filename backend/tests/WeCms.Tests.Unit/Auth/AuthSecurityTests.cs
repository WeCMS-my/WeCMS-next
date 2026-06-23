using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Auth;

public sealed class AuthSecurityTests
{
    [Fact]
    public void IssueAndValidate_IncludeSecurityStamp()
    {
        var options = CreateTokenOptions();
        var service = new AccessTokenService(options);
        var now = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);
        var user = new AuthUserRecord(
            1,
            "admin",
            "Administrator",
            string.Empty,
            "enabled",
            false,
            "stamp-1");
        var token = service.Issue(user, now);

        var principal = service.Validate(token.Token, now.AddMinutes(1));

        Assert.NotNull(principal);
        Assert.Equal(1, principal.UserId);
        Assert.Equal("admin", principal.Username);
        Assert.Equal("stamp-1", principal.SecurityStamp);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenPayloadSegmentCannotBeBase64Decoded()
    {
        var options = CreateTokenOptions();
        var service = new AccessTokenService(options);
        var now = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);

        var malformedPayload = "wecms-unit@bad"; // intentionally not Base64Url-safe
        var token = BuildMalformedPayloadToken(options.AccessTokenSecret, malformedPayload);

        var principal = service.Validate(token, now);

        Assert.Null(principal);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenUsernameFieldCannotBeBase64Decoded()
    {
        var options = CreateTokenOptions();
        var service = new AccessTokenService(options);
        var now = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);

        var payload = string.Join(
            ':',
            options.Issuer,
            "1",
            "!!_invalid_base64!!",
            now.Add(options.AccessTokenLifetime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var token = BuildToken(options.AccessTokenSecret, payload);

        var principal = service.Validate(token, now);

        Assert.Null(principal);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenTokenIsNullOrWhiteSpace()
    {
        var service = new AccessTokenService(CreateTokenOptions());

        Assert.Null(service.Validate("   ", DateTimeOffset.UtcNow));
        Assert.Null(service.Validate(null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordHashIsMalformed()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.bad-format"));
        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.600000.not-base64.salt"));
        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.600000.MjAyMzQ1.salt"));
        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.0.AQIDBAUGBwgJCgsMDQ4PEA==.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa="));
        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.600000.AQIDBAUGBwgJCgsMDQ4PEA==."));
        Assert.False(hasher.Verify("password", "wecms.pbkdf2-sha256.v1.600000..AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));
    }

    [Fact]
    public void AuthRequestContext_RejectsOverlongUserAgent()
    {
        var exception = Assert.Throws<DomainException>(
            () => new AuthRequestContext("192.168.101.199", new string('a', AuthRequestContext.MaxUserAgentLength + 1)));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private static AuthTokenOptions CreateTokenOptions()
    {
        return new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7));
    }

    private static string BuildToken(string secret, string payload)
    {
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signature = ComputeSignature(secret, encodedPayload);

        return $"wecms.at.{encodedPayload}.{signature}";
    }

    private static string BuildMalformedPayloadToken(string secret, string malformedPayloadSegment)
    {
        var signature = ComputeSignature(secret, malformedPayloadSegment);

        return $"wecms.at.{malformedPayloadSegment}.{signature}";
    }

    private static string ComputeSignature(string secret, string encodedPayload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload)));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
