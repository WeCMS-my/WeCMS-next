namespace WeCms.Shared.Security;

public sealed record CurrentUser(
    long Id,
    string Username,
    string DisplayName,
    int PermissionVersion,
    string SecurityStamp)
{
    public static readonly CurrentUser Anonymous = new(0, "", "", 0, "");

    public bool IsAuthenticated => Id > 0;
}
