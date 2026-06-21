namespace WeCms.Modules.Identity.Endpoints;

public static class IdentityEndpointRateLimitPolicyNames
{
    public const string AuthLogin = "auth_login_policy";
    public const string AuthRefresh = "auth_refresh_policy";
    public const string AuthTwoFactor = "auth_2fa_policy";
    public const string AdminWrite = "admin_write_policy";
    public const string FileUpload = "file_upload_policy";
}
