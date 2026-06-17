using SqlSugar;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Users;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Roles;
using WeCms.Persistence.Modules.System.Users;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Authz;

public sealed class LockedRoleIntegrationTests
{

    [DbFact]
    public async Task RoleService_CannotModifyLockedSuperAdminPermissions()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_locked_role_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = 'super_admin'");
            var originalPermissionCount = Scalar<int>(db, "SELECT COUNT(1) FROM sys_role_permission WHERE role_id = @roleId", new SugarParameter("@roleId", roleId));
            var service = new RoleService(new RoleRepository(db), new SqlSugarUnitOfWork(db));

            var exception = await Assert.ThrowsAsync<DomainException>(
                () => service.AssignPermissionsAsync(
                    roleId,
                    new AssignRolePermissionsRequest([Scalar<long>(db, "SELECT id FROM sys_permission ORDER BY id LIMIT 1")]),
                    RoleContext(),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.BusinessError, exception.Code);
            Assert.Equal("Locked role permissions cannot be modified.", exception.Message);
            Assert.Equal(originalPermissionCount, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role_permission WHERE role_id = @roleId", new SugarParameter("@roleId", roleId)));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [DbFact]
    public async Task UserService_ProtectsAndAllowsLockedRoleHolderTransitions()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_locked_holder_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var adminId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin'");
            var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = 'super_admin'");
            var service = new UserService(new UserRepository(db), new PasswordHasher(), new SqlSugarUnitOfWork(db));

            var blocked = await Assert.ThrowsAsync<DomainException>(
                () => service.AssignRolesAsync(adminId, new AssignUserRolesRequest([]), UserContext(adminId), CancellationToken.None));
            Assert.Equal(ApiCodes.BusinessError, blocked.Code);
            Assert.Equal("Locked role must have at least one enabled user.", blocked.Message);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role WHERE user_id = @userId AND role_id = @roleId", new SugarParameter("@userId", adminId), new SugarParameter("@roleId", roleId)));

            var secondUserId = await service.CreateAsync(
                new CreateUserRequest("backup_admin", "Backup Admin", "Backup@123", null, null, null, [roleId], []),
                UserContext(adminId),
                CancellationToken.None);

            await service.AssignRolesAsync(adminId, new AssignUserRolesRequest([]), UserContext(secondUserId.Id), CancellationToken.None);

            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role WHERE user_id = @userId AND role_id = @roleId", new SugarParameter("@userId", adminId), new SugarParameter("@roleId", roleId)));
            Assert.Equal(1, Scalar<int>(
                db,
                """
                SELECT COUNT(1)
                FROM sys_user_role ur
                JOIN sys_user u ON u.id = ur.user_id
                WHERE ur.role_id = @roleId
                  AND u.status = 'enabled'
                  AND u.deleted_at IS NULL
                """,
                new SugarParameter("@roleId", roleId)));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    private static RoleRequestContext RoleContext()
    {
        return new RoleRequestContext(1, "admin", "127.0.0.1", "integration", "trace", DateTimeOffset.UtcNow);
    }

    private static UserRequestContext UserContext(long actorUserId)
    {
        return new UserRequestContext(actorUserId, "admin", "127.0.0.1", "integration", "trace", DateTimeOffset.UtcNow);
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        return (T)Convert.ChangeType(db.Ado.GetScalar(sql, parameters), typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "src", "WeCms.Api", "WeCms.Api.csproj")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
