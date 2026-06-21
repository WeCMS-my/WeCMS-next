using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Permissions;

[Collection(nameof(SharedMySqlCollection))]
public sealed class PermissionVersionRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task BumpMethods_IncrementAffectedUsersPermissionVersion()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var userId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin'");
        var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = 'super_admin'");
        var permissionId = Scalar<long>(db, "SELECT id FROM sys_permission WHERE code = 'sys:system:secure-ping'");
        var menuId = Scalar<long>(db, "SELECT id FROM sys_menu WHERE name = 'sys.system'");
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_role_menu (role_id, menu_id, created_at)
            SELECT @roleId, @menuId, NOW(6)
            WHERE NOT EXISTS (
              SELECT 1 FROM sys_role_menu WHERE role_id = @roleId AND menu_id = @menuId
            )
            """,
            new SugarParameter("@roleId", roleId),
            new SugarParameter("@menuId", menuId));

        var repository = new PermissionVersionRepository(db);
        var now = new DateTimeOffset(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal(0, Version(db, userId));
        await repository.BumpUserAsync(userId, now, CancellationToken.None);
        Assert.Equal(1, Version(db, userId));
        await repository.BumpUsersByRoleAsync(roleId, now, CancellationToken.None);
        Assert.Equal(2, Version(db, userId));
        await repository.BumpUsersByPermissionAsync(permissionId, now, CancellationToken.None);
        Assert.Equal(3, Version(db, userId));
        await repository.BumpUsersByMenuAsync(menuId, now, CancellationToken.None);
        Assert.Equal(4, Version(db, userId));
    }

    private static long Version(ISqlSugarClient db, long userId)
    {
        return Scalar<long>(
            db,
            "SELECT permission_version FROM sys_user WHERE id = @userId",
            new SugarParameter("@userId", userId));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);
        if (scalar is T value)
        {
            return value;
        }

        return (T)Convert.ChangeType(scalar, typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
