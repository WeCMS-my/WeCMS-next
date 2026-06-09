 namespace WeCms.Modules.System.Users;
 
 public sealed record UserListItem(long Id, string Username, string DisplayName, string? Email, string Status, DateTime CreatedAt);
public sealed record UserDetail(long Id, string Username, string DisplayName, string? Email, string? Phone, long? AvatarFileId, string Status, bool TwoFactorEnabled, DateTime? LastLoginAt, string? LastLoginIp, DateTime CreatedAt, DateTime UpdatedAt);
 public sealed record CreateUserRequest(string Username, string DisplayName, string Password, string? Email, string? Phone, long[]? RoleIds);
 public sealed record UpdateUserRequest(string? DisplayName, string? Email, string? Phone, string? Status, long[]? RoleIds);
 public sealed record UserQueryParams(string? Keyword, string? Status, int Page = 1, int PageSize = 20, string? SortBy = null, bool SortDesc = true);
