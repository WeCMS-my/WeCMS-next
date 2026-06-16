namespace WeCms.Modules.System.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthUserDto(long Id, string Username, string DisplayName, bool IsSuperAdmin);

public sealed record AuthMenuDto(
    long Id,
    long? ParentId,
    string Type,
    string Name,
    string Path,
    string Title);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthUserDto User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AuthMenuDto> Menus);

public sealed record AuthMeResponse(
    AuthUserDto User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AuthMenuDto> Menus);
