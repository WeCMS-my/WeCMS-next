using Dapper;
using WeCms.Shared.Data;
using WeCms.Shared.Security;

namespace WeCms.Modules.System.Permissions;

public sealed class PermissionChecker : IPermissionChecker
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionChecker(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PermissionCheckResult> CheckAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenAsync(cancellationToken);

        var user = await connection.QuerySingleOrDefaultAsync<UserStatusRow>(
            new CommandDefinition("""
                SELECT id, status, permission_version, security_stamp
                FROM sys_user
                WHERE id = @userId
                  AND deleted_at IS NULL
                LIMIT 1
                """,
                new { userId },
                cancellationToken: cancellationToken));

        if (user is null || user.Status != 1)
            return new PermissionCheckResult(false, false);

        var hasPermission = await connection.QuerySingleOrDefaultAsync<int>(
            new CommandDefinition("""
                SELECT 1
                FROM sys_user_role ur
                INNER JOIN sys_role_permission rp ON rp.role_id = ur.role_id
                INNER JOIN sys_permission p ON p.id = rp.permission_id
                    AND p.status = 1 AND p.deleted_at IS NULL
                INNER JOIN sys_role r ON r.id = ur.role_id
                    AND r.status = 1 AND r.deleted_at IS NULL
                WHERE ur.user_id = @userId
                  AND p.code = @permissionCode
                LIMIT 1
                """,
                new { userId, permissionCode },
                cancellationToken: cancellationToken)) > 0;

        return new PermissionCheckResult(true, hasPermission);
    }

    private sealed record UserStatusRow(long Id, int Status, int PermissionVersion, string SecurityStamp);
}
