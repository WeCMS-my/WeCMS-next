 namespace WeCms.Modules.System.Auth;
 
 public interface IAuthService
 {
     Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct);
     Task<RefreshResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct);
     Task LogoutAsync(string accessToken, CancellationToken ct);
     Task<CurrentUserResponse?> GetCurrentUserAsync(long userId, CancellationToken ct);
 }
