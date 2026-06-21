using SqlSugar;
using WeCms.Modules.AccessControl.Permissions;

namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;

public sealed class PermissionVersionRepository : IPermissionVersionRepository
{
    private readonly ISqlSugarClient _db;

    public PermissionVersionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user
            SET permission_version = permission_version + 1,
                updated_at = @updatedAt
            WHERE id = @userId
              AND deleted_at IS NULL
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@userId", userId));
    }

    public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user u
            INNER JOIN sys_user_role ur ON ur.user_id = u.id
            SET u.permission_version = u.permission_version + 1,
                u.updated_at = @updatedAt
            WHERE ur.role_id = @roleId
              AND u.deleted_at IS NULL
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@roleId", roleId));
    }

    public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user u
            INNER JOIN sys_user_role ur ON ur.user_id = u.id
            INNER JOIN sys_role_permission rp ON rp.role_id = ur.role_id
            SET u.permission_version = u.permission_version + 1,
                u.updated_at = @updatedAt
            WHERE rp.permission_id = @permissionId
              AND u.deleted_at IS NULL
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@permissionId", permissionId));
    }

    public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user u
            INNER JOIN sys_user_role ur ON ur.user_id = u.id
            INNER JOIN sys_role_menu rm ON rm.role_id = ur.role_id
            SET u.permission_version = u.permission_version + 1,
                u.updated_at = @updatedAt
            WHERE rm.menu_id = @menuId
              AND u.deleted_at IS NULL
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@menuId", menuId));
    }
}
