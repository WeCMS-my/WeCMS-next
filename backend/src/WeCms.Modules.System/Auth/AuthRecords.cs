namespace WeCms.Modules.System.Auth;

public sealed record AuthRequestContext(string Ip, string UserAgent);

public sealed record AuthUserRecord(
    long Id,
    string Username,
    string DisplayName,
    string PasswordHash,
    string Status,
    bool IsSuperAdmin);

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
    string? ReplacedByTokenHash);

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
