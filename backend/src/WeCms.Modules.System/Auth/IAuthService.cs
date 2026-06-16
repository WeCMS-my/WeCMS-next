namespace WeCms.Modules.System.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AuthMeResponse> MeAsync(long userId, CancellationToken cancellationToken);
}
