namespace WeCms.Modules.Identity.Contracts;

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? Status = null,
    long? DeptId = null);

public sealed record UserSummaryDto(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId,
    string Status,
    bool IsSuperAdmin,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record UserDetailDto(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId,
    string Status,
    bool IsSuperAdmin,
    long PermissionVersion,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<long> RoleIds,
    IReadOnlyList<long> PositionIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Password,
    string? Email,
    string? Phone,
    long? DeptId,
    IReadOnlyList<long>? RoleIds,
    IReadOnlyList<long>? PositionIds);

public sealed record UpdateUserRequest(
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId);

public sealed record ResetUserPasswordRequest(string Password);

public sealed record ResetUserTwoFactorRequest(string Reason);

public sealed record AssignUserRolesRequest(IReadOnlyList<long> RoleIds);

public sealed record AssignUserPositionsRequest(IReadOnlyList<long> PositionIds);

public sealed record UserMutationResponse(long Id);
