namespace WeCms.Shared.Security;

public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth_login_policy";
    public const string AuthRefresh = "auth_refresh_policy";
    public const string AuthTwoFactor = "auth_2fa_policy";
    public const string AdminWrite = "admin_write_policy";
    public const string FileUpload = "file_upload_policy";
    public const string SecurityUnban = "security_unban_policy";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        AuthLogin,
        AuthRefresh,
        AuthTwoFactor,
        AdminWrite,
        FileUpload,
        SecurityUnban
    };
}
