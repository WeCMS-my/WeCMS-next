namespace WeCms.Modules.System.Permissions;

public enum PermissionCheckResult
{
    Allowed,
    UserDisabled,
    Forbidden
}

public sealed record PermissionUserRecord(long Id, string Status);

public sealed record SecurePingResponse(string Status);
