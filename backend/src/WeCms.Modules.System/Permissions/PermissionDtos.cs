namespace WeCms.Modules.System.Permissions;

public sealed record PermissionItem(long Id, string Code, string Name, string Module, string Resource, string Action, string Status);
