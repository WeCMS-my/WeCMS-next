using System.Linq;
using SqlSugar;
using WeCms.Api.Security;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;
using WeCms.Modules.Organization;
using WeCms.Modules.Organization.SqlSugar.Repositories;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Authz;

[Collection(nameof(SharedMySqlCollection))]
public sealed class LockedRoleIntegrationTests : PerTestDatabaseResetBase
{

    [DbFact]
    public async Task RoleService_CannotModifyLockedSuperAdminPermissions()
    {
        var baseConnectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareSharedTestDatabaseAsync(db);

        try
        {
            var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = 'super_admin'");
            var originalPermissionCount = Scalar<int>(db, "SELECT COUNT(1) FROM sys_role_permission WHERE role_id = @roleId", new SugarParameter("@roleId", roleId));
            var service = new RoleService(
                new RoleRepository(db),
                new SqlSugarUnitOfWork(db),
                new PermissionVersionService(new PermissionVersionRepository(db)),
                new WeCms.EventBus.SqlSugar.SqlSugarOutboxWriter(new WeCms.EventBus.SqlSugar.Repositories.OutboxMessageRepository(db)),
                new WeCms.Infrastructure.Id.SystemIdGenerator());

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
        }
    }

    [DbFact]
    public async Task UserService_ProtectsAndAllowsLockedRoleHolderTransitions()
    {
        var baseConnectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareSharedTestDatabaseAsync(db);

        var fixture = await CreateLockedRoleUsersAsync(db, $"lrc_{Guid.NewGuid():N}");
        try
        {
            var roleId = fixture.LockedRoleId;
            var service = CreateUserService(db);

            await service.AssignRolesAsync(fixture.PrimaryUserId, new AssignUserRolesRequest([]), UserContext(fixture.SecondaryUserId), CancellationToken.None);

            var blocked = await Assert.ThrowsAsync<DomainException>(
                () => service.AssignRolesAsync(fixture.SecondaryUserId, new AssignUserRolesRequest([]), UserContext(fixture.PrimaryUserId), CancellationToken.None));
            Assert.Equal(ApiCodes.BusinessError, blocked.Code);
            Assert.Equal("Locked role must have at least one enabled user.", blocked.Message);
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role WHERE user_id = @userId AND role_id = @roleId", new SugarParameter("@userId", fixture.PrimaryUserId), new SugarParameter("@roleId", roleId)));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role WHERE user_id = @userId AND role_id = @roleId", new SugarParameter("@userId", fixture.SecondaryUserId), new SugarParameter("@roleId", roleId)));
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
            await CleanupLockedRoleUsersAsync(db, fixture.PrimaryUserId, fixture.SecondaryUserId);
        }
    }

    [DbFact]
    public async Task UserService_ConcurrentAssignRolesCannotRemoveLastEnabledLockedRoleHolder()
    {
        var baseConnectionString = RequiredConnectionString();
        using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareSharedTestDatabaseAsync(setupDb);
        var fixture = await CreateLockedRoleUsersAsync(setupDb, $"w_lrc_{Guid.NewGuid():N}");

        using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
        using var thirdDb = new SqlSugarClientFactory(baseConnectionString).Create();
        var firstService = CreateUserService(secondDb);
        var secondService = CreateUserService(thirdDb);

        using var startSignal = new SemaphoreSlim(0, 2);
        using var executeSignal = new SemaphoreSlim(0, 2);

        Task<ConcurrentLockedRoleOutcome> TryRemoveLockedRoleAsync(UserService service, long actorId, long targetId)
        {
            return Task.Run(async () =>
            {
                startSignal.Release();
                await executeSignal.WaitAsync(TestContext.Current.CancellationToken);
                try
                {
                    await service.AssignRolesAsync(targetId, new AssignUserRolesRequest([]), UserContext(actorId), CancellationToken.None);
                    return new ConcurrentLockedRoleOutcome(true, ApiCodes.Success);
                }
                catch (DomainException ex)
                {
                    return new ConcurrentLockedRoleOutcome(false, ex.Code);
                }
                catch
                {
                    return new ConcurrentLockedRoleOutcome(false, ApiCodes.BusinessError);
                }
            });
        }

        try
        {
            var first = TryRemoveLockedRoleAsync(firstService, fixture.PrimaryUserId, fixture.PrimaryUserId);
            var second = TryRemoveLockedRoleAsync(secondService, fixture.SecondaryUserId, fixture.SecondaryUserId);

            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            executeSignal.Release(2);

            var outcomes = await Task.WhenAll(first, second);

            Assert.Equal(1, outcomes.Count(outcome => outcome.Success));
            Assert.Equal(1, outcomes.Count(outcome => outcome.Code == ApiCodes.BusinessError));
            Assert.Equal(1, Scalar<int>(
                setupDb,
                """
                SELECT COUNT(1)
                FROM sys_user_role ur
                INNER JOIN sys_user u ON u.id = ur.user_id
                WHERE ur.role_id = @roleId
                  AND u.status = 'enabled'
                  AND u.deleted_at IS NULL
                """,
                new SugarParameter("@roleId", fixture.LockedRoleId)));
        }
        finally
        {
            await CleanupLockedRoleUsersAsync(setupDb, fixture.PrimaryUserId, fixture.SecondaryUserId);
        }
    }

    [DbFact]
    public async Task UserService_ConcurrentDeleteCannotRemoveLastEnabledLockedRoleHolder()
    {
        var baseConnectionString = RequiredConnectionString();
        using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareSharedTestDatabaseAsync(setupDb);
        var fixture = await CreateLockedRoleUsersAsync(setupDb, $"w_lrd_{Guid.NewGuid():N}");

        using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
        using var thirdDb = new SqlSugarClientFactory(baseConnectionString).Create();
        var firstService = CreateUserService(secondDb);
        var secondService = CreateUserService(thirdDb);

        using var startSignal = new SemaphoreSlim(0, 2);
        using var executeSignal = new SemaphoreSlim(0, 2);

        Task<ConcurrentLockedRoleOutcome> TryDeleteAsync(UserService service, long actorId, long targetId)
        {
            return Task.Run(async () =>
            {
                startSignal.Release();
                await executeSignal.WaitAsync(TestContext.Current.CancellationToken);
                try
                {
                    await service.DeleteAsync(targetId, UserContext(actorId), CancellationToken.None);
                    return new ConcurrentLockedRoleOutcome(true, ApiCodes.Success);
                }
                catch (DomainException ex)
                {
                    return new ConcurrentLockedRoleOutcome(false, ex.Code);
                }
                catch
                {
                    return new ConcurrentLockedRoleOutcome(false, ApiCodes.BusinessError);
                }
            });
        }

        try
        {
            var first = TryDeleteAsync(firstService, fixture.PrimaryUserId, fixture.SecondaryUserId);
            var second = TryDeleteAsync(secondService, fixture.SecondaryUserId, fixture.PrimaryUserId);

            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            executeSignal.Release(2);

            var outcomes = await Task.WhenAll(first, second);

            Assert.Equal(1, outcomes.Count(outcome => outcome.Success));
            Assert.Equal(1, outcomes.Count(outcome => outcome.Code == ApiCodes.BusinessError));
            Assert.Equal(1, Scalar<int>(
                setupDb,
                """
                SELECT COUNT(1)
                FROM sys_user_role ur
                INNER JOIN sys_user u ON u.id = ur.user_id
                WHERE ur.role_id = @roleId
                  AND u.status = 'enabled'
                  AND u.deleted_at IS NULL
                """,
                new SugarParameter("@roleId", fixture.LockedRoleId)));
        }
        finally
        {
            await CleanupLockedRoleUsersAsync(setupDb, fixture.PrimaryUserId, fixture.SecondaryUserId);
        }
    }

    [DbFact]
    public async Task UserService_ConcurrentDisableCannotRemoveLastEnabledLockedRoleHolder()
    {
        var baseConnectionString = RequiredConnectionString();
        using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareSharedTestDatabaseAsync(setupDb);
        var fixture = await CreateLockedRoleUsersAsync(setupDb, $"w_lrz_{Guid.NewGuid():N}");

        using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
        using var thirdDb = new SqlSugarClientFactory(baseConnectionString).Create();
        var firstService = CreateUserService(secondDb);
        var secondService = CreateUserService(thirdDb);

        using var startSignal = new SemaphoreSlim(0, 2);
        using var executeSignal = new SemaphoreSlim(0, 2);

        Task<ConcurrentLockedRoleOutcome> TryDisableAsync(UserService service, long actorId, long targetId)
        {
            return Task.Run(async () =>
            {
                startSignal.Release();
                await executeSignal.WaitAsync(TestContext.Current.CancellationToken);
                try
                {
                    await service.DisableAsync(targetId, UserContext(actorId), CancellationToken.None);
                    return new ConcurrentLockedRoleOutcome(true, ApiCodes.Success);
                }
                catch (DomainException ex)
                {
                    return new ConcurrentLockedRoleOutcome(false, ex.Code);
                }
                catch
                {
                    return new ConcurrentLockedRoleOutcome(false, ApiCodes.BusinessError);
                }
            });
        }

        try
        {
            var first = TryDisableAsync(firstService, fixture.PrimaryUserId, fixture.SecondaryUserId);
            var second = TryDisableAsync(secondService, fixture.SecondaryUserId, fixture.PrimaryUserId);

            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            await startSignal.WaitAsync(TestContext.Current.CancellationToken);
            executeSignal.Release(2);

            var outcomes = await Task.WhenAll(first, second);

            Assert.Equal(1, outcomes.Count(outcome => outcome.Success));
            Assert.Equal(1, outcomes.Count(outcome => outcome.Code == ApiCodes.BusinessError));
            Assert.Equal(1, Scalar<int>(
                setupDb,
                """
                SELECT COUNT(1)
                FROM sys_user_role ur
                INNER JOIN sys_user u ON u.id = ur.user_id
                WHERE ur.role_id = @roleId
                  AND u.status = 'enabled'
                  AND u.deleted_at IS NULL
                """,
                new SugarParameter("@roleId", fixture.LockedRoleId)));
        }
        finally
        {
            await CleanupLockedRoleUsersAsync(setupDb, fixture.PrimaryUserId, fixture.SecondaryUserId);
        }
    }

    private static async Task PrepareSharedTestDatabaseAsync(ISqlSugarClient db)
    {
        await PrepareDatabaseWithSeedsAsync(db);
    }

    private static async Task<LockedRoleFixture> CreateLockedRoleUsersAsync(ISqlSugarClient db, string prefix)
    {
        var adminId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
        var roleCode = $"{prefix}_locked_role";
        var roleName = $"Locked Test Role {prefix}";
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_role (code, name, status, is_builtin, is_locked, created_at, updated_at, deleted_at)
            SELECT @roleCode, @roleName, 'enabled', FALSE, TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
            WHERE NOT EXISTS (
                SELECT 1
                FROM sys_role
                WHERE code = @roleCode
            )
            """,
            new SugarParameter("@roleCode", roleCode),
            new SugarParameter("@roleName", roleName));
        var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = @roleCode", new SugarParameter("@roleCode", roleCode));

        var service = CreateUserService(db);
        var first = await service.CreateAsync(
            new CreateUserRequest($"{prefix}_holder_a", "Locked Role Holder A", "Backup@123", null, null, null, [roleId], []),
            UserContext(adminId),
            CancellationToken.None);

        var second = await service.CreateAsync(
            new CreateUserRequest($"{prefix}_holder_b", "Locked Role Holder B", "Backup@123", null, null, null, [roleId], []),
            UserContext(adminId),
            CancellationToken.None);

        return new LockedRoleFixture(first.Id, second.Id, roleId);
    }

    private static Task CleanupLockedRoleUsersAsync(ISqlSugarClient db, params long[] userIds)
    {
        var ids = userIds.Distinct().ToArray();
        foreach (var userId in ids)
        {
            db.Ado.ExecuteCommand("DELETE FROM sys_refresh_token WHERE user_id = @userId", new SugarParameter("@userId", userId));
            db.Ado.ExecuteCommand("DELETE FROM sys_audit_log WHERE user_id = @userId", new SugarParameter("@userId", userId));
            db.Ado.ExecuteCommand("DELETE FROM sys_user_role WHERE user_id = @userId", new SugarParameter("@userId", userId));
            db.Ado.ExecuteCommand("DELETE FROM sys_user_position WHERE user_id = @userId", new SugarParameter("@userId", userId));
            db.Ado.ExecuteCommand("DELETE FROM sys_user WHERE id = @userId", new SugarParameter("@userId", userId));
        }

        return Task.CompletedTask;
    }

    private static RoleRequestContext RoleContext()
    {
        return new RoleRequestContext(1, "admin", "test-host", "integration", "trace", DateTimeOffset.UtcNow);
    }

    private static UserRequestContext UserContext(long actorUserId)
    {
        return new UserRequestContext(actorUserId, "admin", "test-host", "integration", "trace", DateTimeOffset.UtcNow);
    }

    private static UserService CreateUserService(ISqlSugarClient db)
    {
        return new UserService(
            new UserRepository(db, new SecurityEventClassifier(), new WeCms.Infrastructure.Id.SystemIdGenerator()),
            new PasswordHasher(),
            new SqlSugarUnitOfWork(db),
            new FakeTwoFactorService(),
            new PermissionVersionService(new PermissionVersionRepository(db)),
            new OrganizationLookupService(new DepartmentRepository(db), new PositionRepository(db)),
            new WeCms.EventBus.SqlSugar.SqlSugarOutboxWriter(new WeCms.EventBus.SqlSugar.Repositories.OutboxMessageRepository(db)),
            new WeCms.Infrastructure.Id.SystemIdGenerator());
    }

    private sealed record ConcurrentLockedRoleOutcome(bool Success, int Code);

    private sealed record LockedRoleFixture(long PrimaryUserId, long SecondaryUserId, long LockedRoleId);

    private sealed class FakeTwoFactorService : ITwoFactorService
    {
        public Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        return (T)Convert.ChangeType(db.Ado.GetScalar(sql, parameters), typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
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
