using Microsoft.AspNetCore.Http;

namespace WeCms.Modules.Identity.Services;

public interface IIdentityCookieAuthOriginValidator
{
    Task ValidateAsync(
        HttpContext context,
        string endpointName,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public static class IdentityCookieAuthOriginEndpoints
{
    public const string Refresh = "auth.refresh";
    public const string Logout = "auth.logout";
    public const string TwoFactorVerify = "auth.2fa.verify";
    public const string TwoFactorRecoveryCode = "auth.2fa.recovery-code";
}
