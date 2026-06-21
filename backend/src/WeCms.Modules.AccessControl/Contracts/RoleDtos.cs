namespace WeCms.Modules.AccessControl.Contracts;

public sealed record RoleListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? Status = null);

public sealed record RoleSummaryDto(
    long Id,
    string Code,
    string Name,
    string Status,
    bool IsBuiltin,
    bool IsLocked,
    DateTimeOffset CreatedAt);

public sealed record RoleDetailDto(
    long Id,
    string Code,
    string Name,
    string Status,
    bool IsBuiltin,
    bool IsLocked,
    IReadOnlyList<long> PermissionIds,
    IReadOnlyList<long> MenuIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    IReadOnlyList<long>? PermissionIds,
    IReadOnlyList<long>? MenuIds);

public sealed record UpdateRoleRequest(string Name);

public sealed record AssignRolePermissionsRequest(IReadOnlyList<long> PermissionIds);

public sealed record AssignRoleMenusRequest(IReadOnlyList<long> MenuIds);

public sealed record RoleMutationResponse(long Id);
