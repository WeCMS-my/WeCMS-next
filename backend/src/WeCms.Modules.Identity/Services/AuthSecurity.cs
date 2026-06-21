using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WeCms.Modules.Identity.Services;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public interface IAccessTokenService
{
    IssuedAccessToken Issue(AuthUserRecord user, DateTimeOffset now);

    AccessTokenPrincipal? Validate(string token, DateTimeOffset now);
}

public interface IRefreshTokenService
{
    IssuedRefreshToken Issue(DateTimeOffset now);

    string Hash(string token);
}

public interface IAuthClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IAuthTokenEntropy
{
    byte[] GetBytes(int count);

    string NewFamilyId();
}

public sealed class PasswordHasher : IPasswordHasher
{
    private const int ExpectedSaltSizeBytes = 16;
    private const int ExpectedHashSizeBytes = 32;
    private const int PasswordIterations = 600_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(ExpectedSaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            ExpectedHashSizeBytes);

        return $"wecms.pbkdf2-sha256.v1.{PasswordIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 6
                || parts[0] != "wecms"
                || parts[1] != "pbkdf2-sha256"
                || parts[2] != "v1"
                || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
                || iterations <= 0)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);
            if (salt.Length != ExpectedSaltSizeBytes || expectedHash.Length != ExpectedHashSizeBytes)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string HashForTest(string password)
    {
        var salt = Convert.FromBase64String("AQIDBAUGBwgJCgsMDQ4PEA==");
        const int iterations = 600_000;
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"wecms.pbkdf2-sha256.v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}

public sealed class AccessTokenService : IAccessTokenService
{
    private const string Prefix = "wecms.at";
    private readonly AuthTokenOptions _options;

    public AccessTokenService(AuthTokenOptions options)
    {
        if (options.AccessTokenSecret.Length < 32)
        {
            throw new ArgumentException("Access token secret must be at least 32 characters.", nameof(options));
        }

        _options = options;
    }

    public IssuedAccessToken Issue(AuthUserRecord user, DateTimeOffset now)
    {
        var expiresAt = now.Add(_options.AccessTokenLifetime);
        var payload = string.Join(
            ':',
            _options.Issuer,
            user.Id.ToString(CultureInfo.InvariantCulture),
            Base64UrlEncode(Encoding.UTF8.GetBytes(user.Username)),
            Base64UrlEncode(Encoding.UTF8.GetBytes(user.SecurityStamp)),
            expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var encodedPayload = Base64UrlEncode(payloadBytes);
        var signature = Sign(encodedPayload);

        return new IssuedAccessToken($"{Prefix}.{encodedPayload}.{signature}", expiresAt);
    }

    public AccessTokenPrincipal? Validate(string token, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 4 || parts[0] != "wecms" || parts[1] != "at")
            {
                return null;
            }

            if (!FixedTimeEquals(Sign(parts[2]), parts[3]))
            {
                return null;
            }

            var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[2]));
            var fields = payload.Split(':');
            if (fields.Length != 5
                || fields[0] != _options.Issuer
                || !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                || !long.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresUnix))
            {
                return null;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
            if (expiresAt <= now)
            {
                return null;
            }

            var username = Encoding.UTF8.GetString(Base64UrlDecode(fields[2]));
            var securityStamp = Encoding.UTF8.GetString(Base64UrlDecode(fields[3]));

            return new AccessTokenPrincipal(userId, username, securityStamp, expiresAt);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string Sign(string encodedPayload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.AccessTokenSecret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        return Convert.FromBase64String(padded);
    }
}

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenBytes = 32;
    private readonly TimeSpan _lifetime;
    private readonly IAuthTokenEntropy _entropy;

    public RefreshTokenService(IAuthTokenEntropy entropy)
        : this(TimeSpan.FromDays(7), entropy)
    {
    }

    public RefreshTokenService(TimeSpan lifetime, IAuthTokenEntropy entropy)
    {
        _lifetime = lifetime;
        _entropy = entropy;
    }

    public IssuedRefreshToken Issue(DateTimeOffset now)
    {
        var token = Base64UrlEncode(_entropy.GetBytes(TokenBytes));
        var hash = Hash(token);

        return new IssuedRefreshToken(token, hash, _entropy.NewFamilyId(), now.Add(_lifetime));
    }

    public string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public sealed class SystemAuthClock : IAuthClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class AuthTokenEntropy : IAuthTokenEntropy
{
    public byte[] GetBytes(int count)
    {
        return RandomNumberGenerator.GetBytes(count);
    }

    public string NewFamilyId()
    {
        return new Guid(RandomNumberGenerator.GetBytes(16)).ToString("D");
    }
}

public sealed class AuthChallengeEntropy : IAuthChallengeEntropy
{
    private const int ChallengeBytes = 32;

    public string NewChallengeId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(ChallengeBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
