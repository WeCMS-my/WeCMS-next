using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;

namespace WeCms.Modules.AccessControl.Repositories;

public interface IPermissionRepository
{
    Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken);

    Task<bool> UserHasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionSummaryDto>> ListManagementAsync(CancellationToken cancellationToken);

    Task<PermissionDetailDto?> GetManagementAsync(long id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, long? exceptPermissionId, CancellationToken cancellationToken);

    Task<long> CreateManagementAsync(PermissionCreateRecord record, CancellationToken cancellationToken);

    Task UpdateManagementAsync(PermissionUpdateRecord record, CancellationToken cancellationToken);

    Task SoftDeleteManagementAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task SetManagementStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);

    Task RecordManagementAuditAsync(PermissionAuditRecord record, CancellationToken cancellationToken);
}
