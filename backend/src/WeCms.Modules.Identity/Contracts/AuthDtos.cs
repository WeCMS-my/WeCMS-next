namespace WeCms.Modules.Identity.Contracts;

public sealed record AuthMenuTreeDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    bool IsBuiltin,
    IReadOnlyList<AuthMenuTreeDto> Children);

public sealed record LoginRequest(string Username, string Password);

public sealed record TwoFactorVerifyRequest(string ChallengeId, string Code);

public sealed record TwoFactorRecoveryCodeRequest(string ChallengeId, string RecoveryCode);

public sealed record AuthUserDto(long Id, string Username, string DisplayName);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthUserDto? User,
    long PermissionVersion,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AuthMenuTreeDto> Menus,
    bool RequiresTwoFactor = false,
    string? TwoFactorChallengeId = null,
    DateTimeOffset? TwoFactorChallengeExpiresAt = null);

public sealed record AuthSessionResult(
    LoginResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    TimeSpan RefreshTokenMaxAge);

public sealed record AuthMeResponse(
    AuthUserDto User,
    long PermissionVersion,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AuthMenuTreeDto> Menus);
