using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public sealed record LogoutRequest(string RefreshToken);

public sealed record MeUserInfo(long Id, string Username, string DisplayName);

public sealed record CurrentUserMenuDto(
    long Id,
    string Code,
    string Name,
    string Component,
    string RoutePath,
    IReadOnlyList<CurrentUserMenuDto> Children);

public sealed record CurrentUserResponse(
    MeUserInfo User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<CurrentUserMenuDto> Menus);
