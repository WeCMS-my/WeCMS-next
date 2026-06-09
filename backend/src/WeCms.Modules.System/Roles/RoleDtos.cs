 namespace WeCms.Modules.System.Roles;
 
 public sealed record RoleListItem(long Id, string Code, string Name, string Status, int Sort, DateTime CreatedAt);
 public sealed record RoleDetail(long Id, string Code, string Name, string? Description, string Status, int Sort, string DataScope, DateTime CreatedAt, DateTime UpdatedAt);
 public sealed record CreateRoleRequest(string Code, string Name, string? Description, int Sort = 0);
 public sealed record UpdateRoleRequest(string? Name, string? Description, string? Status, int? Sort);
 public sealed record AssignMenusRequest(long[] MenuIds);
 public sealed record AssignPermissionsRequest(long[] PermissionIds);
