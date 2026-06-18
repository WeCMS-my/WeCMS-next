using WeCms.Modules.System.Menus;

namespace WeCms.Modules.System.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record TwoFactorVerifyRequest(string ChallengeId, string Code);

public sealed record TwoFactorRecoveryCodeRequest(string ChallengeId, string RecoveryCode);

public sealed record AuthUserDto(long Id, string Username, string DisplayName, bool IsSuperAdmin);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthUserDto? User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<MenuTreeDto> Menus,
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
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<MenuTreeDto> Menus);
