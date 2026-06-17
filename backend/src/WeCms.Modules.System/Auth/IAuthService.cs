namespace WeCms.Modules.System.Auth;

public interface IAuthService
{
    Task<AuthSessionResult> LoginAsync(
        LoginRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AuthSessionResult> RefreshAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AuthMeResponse> MeAsync(long userId, CancellationToken cancellationToken);
}
