using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password, string ip, CancellationToken ct);
    Task<LoginResponse?> VerifyTwoFactorAndLoginAsync(string? ticket, string username, string code, ITwoFactorService twoFactor, CancellationToken ct);
    Task<RefreshResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
    Task<CurrentUserResponse?> GetCurrentUserAsync(long userId, CancellationToken ct);
}
