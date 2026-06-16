using WeCms.Shared;

namespace WeCms.Modules.System.Roles;

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
    DateTimeOffset CreatedAt);

public sealed record RoleDetailDto(
    long Id,
    string Code,
    string Name,
    string Status,
    bool IsBuiltin,
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

public interface IRoleService
{
    Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListQuery query, CancellationToken cancellationToken);

    Task<RoleDetailDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<RoleMutationResponse> CreateAsync(CreateRoleRequest request, RoleRequestContext context, CancellationToken cancellationToken);

    Task<RoleMutationResponse> UpdateAsync(long id, UpdateRoleRequest request, RoleRequestContext context, CancellationToken cancellationToken);

    Task DeleteAsync(long id, RoleRequestContext context, CancellationToken cancellationToken);

    Task EnableAsync(long id, RoleRequestContext context, CancellationToken cancellationToken);

    Task DisableAsync(long id, RoleRequestContext context, CancellationToken cancellationToken);

    Task AssignPermissionsAsync(long id, AssignRolePermissionsRequest request, RoleRequestContext context, CancellationToken cancellationToken);

    Task AssignMenusAsync(long id, AssignRoleMenusRequest request, RoleRequestContext context, CancellationToken cancellationToken);
}
