using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;
using WeCms.Shared;

namespace WeCms.Modules.AccessControl.Roles;

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
