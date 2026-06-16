using SqlSugar;
using WeCms.Modules.System.Permissions;

namespace WeCms.Persistence.Modules.System.Permissions;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly ISqlSugarClient _db;

    public PermissionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = _db.Ado.SqlQuerySingle<PermissionUserRow>(
            """
            SELECT id AS Id,
                   status AS Status
            FROM sys_user
            WHERE id = @userId
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return Task.FromResult(row is null ? null : new PermissionUserRecord(row.Id, row.Status));
    }

    public Task<bool> UserHasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = _db.Ado.GetScalar(
            """
            SELECT COUNT(1)
            FROM sys_permission p
            INNER JOIN sys_role_permission rp ON rp.permission_id = p.id
            INNER JOIN sys_user_role ur ON ur.role_id = rp.role_id
            INNER JOIN sys_role r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
              AND p.code = @permissionCode
              AND r.status = 'enabled'
            """,
            new SugarParameter("@userId", userId),
            new SugarParameter("@permissionCode", permissionCode));

        return Task.FromResult(Convert.ToInt32(count, global::System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    private sealed class PermissionUserRow
    {
        public long Id { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
