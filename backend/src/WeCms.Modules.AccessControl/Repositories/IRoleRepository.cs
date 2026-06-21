using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;
using WeCms.Shared;

namespace WeCms.Modules.AccessControl.Repositories;

public interface IRoleRepository
{
    Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListCriteria criteria, CancellationToken cancellationToken);

    Task<RoleDetailDto?> GetAsync(long id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, long? exceptRoleId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingPermissionIdsAsync(IReadOnlyList<long> permissionIds, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingMenuIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken);

    Task<long> CreateAsync(RoleCreateRecord record, CancellationToken cancellationToken);

    Task UpdateAsync(RoleUpdateRecord record, CancellationToken cancellationToken);

    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplacePermissionsAsync(long id, IReadOnlyList<long> permissionIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplaceMenusAsync(long id, IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task RecordAuditAsync(RoleAuditRecord record, CancellationToken cancellationToken);
}
