using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;

namespace WeCms.Modules.AccessControl.Permissions;

public interface IPermissionManagementService
{
    Task<IReadOnlyList<PermissionSummaryDto>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionTreeDto>> TreeAsync(CancellationToken cancellationToken);

    Task<PermissionDetailDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<PermissionMutationResponse> CreateAsync(CreatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken);

    Task<PermissionMutationResponse> UpdateAsync(long id, UpdatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken);

    Task DeleteAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);

    Task EnableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);

    Task DisableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);
}
