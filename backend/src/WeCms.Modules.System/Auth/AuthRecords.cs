using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public sealed record AuthRequestContext
{
    public const int MaxIpLength = 45;
    public const int MaxUserAgentLength = 500;
    public const int MaxTraceIdLength = 64;

    public AuthRequestContext(string ip, string userAgent, string traceId = "")
    {
        Ip = NormalizeOptional(ip, nameof(ip), MaxIpLength);
        UserAgent = NormalizeOptional(userAgent, nameof(userAgent), MaxUserAgentLength);
        TraceId = NormalizeOptional(traceId, nameof(traceId), MaxTraceIdLength);
    }

    public string Ip { get; }

    public string UserAgent { get; }

    public string TraceId { get; }

    private static string NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}

public sealed record AuthUserRecord(
    long Id,
    string Username,
    string DisplayName,
    string PasswordHash,
    string Status,
    bool IsSuperAdmin,
    bool MustChangePassword = false);

public sealed record FailedLoginRecord(
    string Username,
    string Ip,
    string UserAgent,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record SecurityEventRecord(
    string EventType,
    long? UserId,
    string? Username,
    string Ip,
    string Severity,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record SuccessfulLoginRecord(
    long UserId,
    string Ip,
    string RefreshTokenHash,
    string RefreshTokenFamilyId,
    DateTimeOffset RefreshTokenExpiresAt,
    DateTimeOffset UpdatedAt);

public sealed record AuditLogRecord(
    long? UserId,
    string? Username,
    string Module,
    string Resource,
    string Action,
    string? TargetId,
    string RequestMethod,
    string RequestPath,
    string Ip,
    string UserAgent,
    string TraceId,
    string Result,
    string Detail,
    DateTimeOffset CreatedAt);

public sealed record RefreshTokenRecord(
    long Id,
    long UserId,
    string Username,
    string DisplayName,
    string UserStatus,
    bool IsSuperAdmin,
    string TokenHash,
    string FamilyId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? ReplacedByTokenHash,
    bool MustChangePassword = false);

public sealed record RefreshRotationRecord(
    long OldRefreshTokenId,
    long UserId,
    string NewRefreshTokenHash,
    string FamilyId,
    DateTimeOffset NewRefreshTokenExpiresAt,
    DateTimeOffset RotatedAt);

public sealed class RefreshTokenAlreadyRevokedException : Exception
{
    public RefreshTokenAlreadyRevokedException(string familyId)
        : base("Refresh token was already revoked.")
    {
        FamilyId = familyId;
    }

    public string FamilyId { get; }
}

public sealed record AccessTokenPrincipal(long UserId, string Username, DateTimeOffset ExpiresAt);

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record IssuedRefreshToken(string Token, string Hash, string FamilyId, DateTimeOffset ExpiresAt);

public sealed record AuthTokenOptions(
    string AccessTokenSecret,
    string Issuer,
    TimeSpan AccessTokenLifetime,
    TimeSpan RefreshTokenLifetime);
