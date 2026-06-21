namespace WeCms.Modules.Identity.Services;

public interface IAccountTwoFactorService
{
    Task<AccountTwoFactorStatusResponse> StatusAsync(long userId, CancellationToken cancellationToken);

    Task<AccountTwoFactorSetupResponse> BeginSetupAsync(
        long userId,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AccountTwoFactorStatusResponse> ConfirmAsync(
        long userId,
        AccountTwoFactorConfirmRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task DisableAsync(
        long userId,
        AccountTwoFactorDisableRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AccountTwoFactorRecoveryCodesResponse> RegenerateRecoveryCodesAsync(
        long userId,
        AccountTwoFactorRegenerateRecoveryCodesRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}
