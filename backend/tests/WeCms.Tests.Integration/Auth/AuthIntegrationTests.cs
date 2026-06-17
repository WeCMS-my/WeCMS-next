using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Users;
using WeCms.Modules.System.Permissions;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Auth;
using WeCms.Persistence.Modules.System.Permissions;
using WeCms.Persistence.Modules.System.Users;
using WeCms.Shared;
using SqlSugar;
using System.Text;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Auth;

public sealed class AuthIntegrationTests : global::Xunit.IAsyncLifetime
{

    public Task InitializeAsync()
    {
        return IntegrationTestDatabase.ResetDatabaseAsync(RequiredConnectionString());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DbFact]
    public async Task AuthService_LoginFailureAndSuccessPersistExpectedAuditAndTokenState()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var accessTokenService = new AccessTokenService(tokenOptions);
            var refreshTokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var failed = await Assert.ThrowsAsync<DomainException>(
                () => service.LoginAsync(
                    new LoginRequest("admin", "wrong"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.Unauthorized, failed.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_log WHERE username = 'admin' AND result = 'failed'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.login_failed'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'failed'"));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            Assert.NotEmpty(login.AccessToken);
            Assert.NotEmpty(login.RefreshToken);
            Assert.Equal(["super_admin"], login.Roles);
            Assert.Equal(
                Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission"),
                login.Permissions.Count);
            Assert.Contains("sys:system:secure-ping", login.Permissions);
            Assert.Contains("sys:user:list", login.Permissions);
            Assert.NotEqual(login.RefreshToken, Scalar<string>(db, "SELECT token_hash FROM sys_refresh_token LIMIT 1"));
            Assert.NotEmpty(login.Menus);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin' AND last_login_at IS NOT NULL AND last_login_ip = '192.168.101.199'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'success'"));

            var principal = accessTokenService.Validate(login.AccessToken, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            Assert.NotNull(principal);

            var me = await service.MeAsync(principal.UserId, CancellationToken.None);
            Assert.Equal("admin", me.User.Username);
            Assert.Equal(["super_admin"], me.Roles);
            Assert.Equal(login.Permissions, me.Permissions);
            Assert.NotEmpty(me.Menus);

            var refreshed = await service.RefreshAsync(
                new RefreshTokenRequest(login.RefreshToken),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            var oldRefreshHash = refreshTokenService.Hash(login.RefreshToken);
            var newRefreshHash = refreshTokenService.Hash(refreshed.RefreshToken);

            Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
            Assert.Equal(0, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", login.RefreshToken)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE token_hash = @oldHash AND revoked_at IS NOT NULL AND replaced_by_token_hash = @newHash",
                new SugarParameter("@oldHash", oldRefreshHash),
                new SugarParameter("@newHash", newRefreshHash)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE token_hash = @newHash AND revoked_at IS NULL",
                new SugarParameter("@newHash", newRefreshHash)));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'success'"));

            var reused = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, reused.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed' AND target_id = 'admin'"));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = (SELECT family_id FROM sys_refresh_token WHERE token_hash = @oldHash) AND revoked_at IS NULL",
                new SugarParameter("@oldHash", oldRefreshHash)));

            var expiredLogin = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            var expiredHash = refreshTokenService.Hash(expiredLogin.RefreshToken);
            db.Ado.ExecuteCommand(
                "UPDATE sys_refresh_token SET expires_at = @expiresAt WHERE token_hash = @tokenHash",
                new SugarParameter("@expiresAt", new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)),
                new SugarParameter("@tokenHash", expiredHash));
            var expired = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(expiredLogin.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, expired.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_expired'"));
            Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed' AND target_id = 'admin'"));

            var disabledLogin = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            db.Ado.ExecuteCommand("UPDATE sys_user SET status = 'disabled' WHERE username = 'admin'");
            var disabled = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(disabledLogin.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, disabled.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_user_disabled'"));
            Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed' AND target_id = 'admin'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'blocked' AND target_id = 'admin'"));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_UserWithoutMenuTreePermissionStillReceivesVisibleMenusFromAuth()
    {
        var baseConnectionString = RequiredConnectionString();

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var now = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);
            var roleId = await CreateRoleAsync(db, "limited_ops", "Limited Ops", now);
            var userId = await CreateUserAsync(db, "limited_ops_user", "LimitedOps@123", now);

            await AssignRolePermissionsAsync(
                db,
                roleId,
                ["sys:user:list", "sys:role:list", "sys:file:list"],
                now);
            await AssignRoleMenusAsync(
                db,
                roleId,
                ["sys.system", "sys.users", "sys.roles", "sys.files"],
                now);
            await AssignUserRoleAsync(db, userId, roleId, now);

            var service = CreateService(db, TokenOptions(), now);

            var login = await service.LoginAsync(
                new LoginRequest("limited_ops_user", "LimitedOps@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            Assert.DoesNotContain("sys:menu:tree", login.Permissions);
            Assert.NotEmpty(login.Menus);

            var me = await service.MeAsync(userId, CancellationToken.None);
            Assert.DoesNotContain("sys:menu:tree", me.Permissions);
            Assert.NotEmpty(me.Menus);
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_SoftDeletedUserCannotLoginRefreshMeOrUsePermissions()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var accessTokenService = new AccessTokenService(tokenOptions);
            var userRepository = new UserRepository(db);
            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            var targetUserId = await CreateUserAsync(
                db,
                "soft_deleted_target",
                "SoftDeleted@123",
                new DateTimeOffset(2026, 6, 16, 0, 0, 10, TimeSpan.Zero));

            var login = await service.LoginAsync(
                new LoginRequest("soft_deleted_target", "SoftDeleted@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            var userService = new UserService(userRepository, new PasswordHasher(), new SqlSugarUnitOfWork(db));
            await userService.DeleteAsync(
                targetUserId,
                new UserRequestContext(
                    adminUserId,
                    "admin",
                    "192.168.101.199",
                    "integration",
                    "integration-soft-delete",
                    new DateTimeOffset(2026, 6, 16, 0, 0, 30, TimeSpan.Zero)),
                CancellationToken.None);

            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_user WHERE id = @userId AND status = 'disabled' AND deleted_at IS NOT NULL",
                new SugarParameter("@userId", targetUserId)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE user_id = @userId AND revoked_at IS NOT NULL",
                new SugarParameter("@userId", targetUserId)));

            var loginAfterDelete = await Assert.ThrowsAsync<DomainException>(
                () => service.LoginAsync(
                    new LoginRequest("soft_deleted_target", "SoftDeleted@123"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, loginAfterDelete.Code);

            var refreshAfterDelete = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, refreshAfterDelete.Code);

            var principal = accessTokenService.Validate(login.AccessToken, new DateTimeOffset(2026, 6, 16, 0, 0, 59, TimeSpan.Zero));
            Assert.NotNull(principal);
            var meAfterDelete = await Assert.ThrowsAsync<DomainException>(
                () => service.MeAsync(principal!.UserId, CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, meAfterDelete.Code);

            var checker = new PermissionChecker(new PermissionRepository(db));
            var permissionResult = await checker.CheckAsync(targetUserId, SystemPermissions.SecurePing, CancellationToken.None);
            Assert.Equal(PermissionCheckResult.UserDisabled, permissionResult);
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_ResetPasswordRevokesExistingRefreshToken()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var now = new DateTimeOffset(2026, 6, 16, 0, 0, 10, TimeSpan.Zero);
            var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            var targetPassword = "Reset@123";
            var userId = await CreateUserAsync(db, "reset_target", targetPassword, now);
            var login = await service.LoginAsync(
                new LoginRequest("reset_target", targetPassword),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            var oldRefreshHash = tokenService.Hash(login.RefreshToken);
            var userService = new UserService(new UserRepository(db), new PasswordHasher(), new SqlSugarUnitOfWork(db));
            await userService.ResetPasswordAsync(
                userId,
                new ResetUserPasswordRequest("NewAdmin@123"),
                new UserRequestContext(
                    actorUserId,
                    "admin",
                    "192.168.101.199",
                    "integration",
                    "integration-reset-pass",
                    now),
                CancellationToken.None);

            var refreshError = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, refreshError.Code);
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE user_id = @userId AND token_hash = @tokenHash AND revoked_at IS NOT NULL",
                    new SugarParameter("@userId", userId),
                    new SugarParameter("@tokenHash", oldRefreshHash)));

            await Assert.ThrowsAsync<DomainException>(
                () => service.LoginAsync(
                    new LoginRequest("reset_target", targetPassword),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            var newLogin = await service.LoginAsync(
                new LoginRequest("reset_target", "NewAdmin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            Assert.NotNull(newLogin);
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_DisabledUserRefreshTokenIsRevoked()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var now = new DateTimeOffset(2026, 6, 16, 0, 0, 20, TimeSpan.Zero);
            var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            var targetPassword = "Disabled@123";
            var userId = await CreateUserAsync(db, "disabled_target", targetPassword, now);
            var login = await service.LoginAsync(
                new LoginRequest("disabled_target", targetPassword),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            var oldRefreshHash = tokenService.Hash(login.RefreshToken);

            var userService = new UserService(new UserRepository(db), new PasswordHasher(), new SqlSugarUnitOfWork(db));
            await userService.DisableAsync(
                userId,
                new UserRequestContext(
                    actorUserId,
                    "admin",
                    "192.168.101.199",
                    "integration",
                    "integration-disable",
                    now),
                CancellationToken.None);

            var refreshError = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, refreshError.Code);
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE user_id = @userId AND token_hash = @tokenHash AND revoked_at IS NOT NULL",
                new SugarParameter("@userId", userId),
                new SugarParameter("@tokenHash", oldRefreshHash)));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_DeletedUserRefreshTokenIsRevoked()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var now = new DateTimeOffset(2026, 6, 16, 0, 0, 30, TimeSpan.Zero);
            var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            var targetPassword = "Deleted@123";
            var userId = await CreateUserAsync(db, "deleted_target", targetPassword, now);
            var login = await service.LoginAsync(
                new LoginRequest("deleted_target", targetPassword),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            var oldRefreshHash = tokenService.Hash(login.RefreshToken);

            var userService = new UserService(new UserRepository(db), new PasswordHasher(), new SqlSugarUnitOfWork(db));
            await userService.DeleteAsync(
                userId,
                new UserRequestContext(
                    actorUserId,
                    "admin",
                    "192.168.101.199",
                    "integration",
                    "integration-delete",
                    now),
                CancellationToken.None);

            var refreshError = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, refreshError.Code);
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE user_id = @userId AND token_hash = @tokenHash AND revoked_at IS NOT NULL",
                new SugarParameter("@userId", userId),
                new SugarParameter("@tokenHash", oldRefreshHash)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_user WHERE id = @userId AND deleted_at IS NOT NULL",
                new SugarParameter("@userId", userId)));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_ProductionAdminSeedRequiresPasswordRotationBeforeLogin()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Production", "AdminRotation123!"));

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_user WHERE username = 'admin' AND must_change_password = TRUE"));

            var exception = await Assert.ThrowsAsync<DomainException>(
                () => service.LoginAsync(
                    new LoginRequest("admin", "AdminRotation123!"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.BusinessError, exception.Code);
            Assert.Equal("Password change required.", exception.Message);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.password_change_required'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'blocked'"));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task RefreshAsync_ConcurrentRefreshAllowsOnlyOneSuccess()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(setupDb).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(setupDb).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var login = await CreateService(setupDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero))
                .LoginAsync(
                    new LoginRequest("admin", "Admin@123"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None);

            using var firstDb = new SqlSugarClientFactory(baseConnectionString).Create();
            using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
            var firstService = CreateService(firstDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            var secondService = CreateService(secondDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));

            var results = await Task.WhenAll(
                TryRefreshAsync(firstService, login.RefreshToken),
                TryRefreshAsync(secondService, login.RefreshToken));

            Assert.Equal(1, results.Count(success => success));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_LogoutRevokesRefreshTokenFamily()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            var loginRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                db,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", loginRefreshHash));

            await service.LogoutAsync(
                new LogoutRequest(login.RefreshToken),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            Assert.Equal(0, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND revoked_at IS NULL",
                new SugarParameter("@familyId", familyId)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.logout'"));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'logout' AND result = 'success'"));

            await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(login.RefreshToken),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task AuthService_LogoutUnknownTokenDoesNotAffectFamily()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);
            var loginRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                db,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", loginRefreshHash));

            await service.LogoutAsync(
                new LogoutRequest("invalid-refresh-token"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND revoked_at IS NULL",
                new SugarParameter("@familyId", familyId)));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.logout_unknown_token'"));
            Assert.Equal(1, Scalar<int>(
                db,
                "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'logout' AND result = 'failed'"));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task RefreshAsync_ConcurrentRefreshLongAfterWindowRevokesFamily()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(setupDb).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(setupDb).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var login = await CreateService(
                    setupDb,
                    tokenOptions,
                    new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero))
                .LoginAsync(
                    new LoginRequest("admin", "Admin@123"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None);

            var oldRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                setupDb,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", oldRefreshHash));

            using var firstDb = new SqlSugarClientFactory(baseConnectionString).Create();
            using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
            var firstService = CreateService(firstDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            var secondService = CreateService(secondDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 10, 0, TimeSpan.Zero));

            var results = await Task.WhenAll(
                TryRefreshAsync(firstService, login.RefreshToken),
                TryRefreshAsync(secondService, login.RefreshToken));

            Assert.Equal(1, results.Count(success => success));
            Assert.Equal(0, Scalar<int>(
                setupDb,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND revoked_at IS NULL",
                new SugarParameter("@familyId", familyId)));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse'"));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed'"));
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task RefreshAsync_ConcurrentRefreshWithinWindowKeepsFamilyPartiallyActive()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var setupDb = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(setupDb).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(setupDb).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var login = await CreateService(
                    setupDb,
                    tokenOptions,
                    new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero))
                .LoginAsync(
                    new LoginRequest("admin", "Admin@123"),
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None);

            var oldRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                setupDb,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", oldRefreshHash));

            using var firstDb = new SqlSugarClientFactory(baseConnectionString).Create();
            using var secondDb = new SqlSugarClientFactory(baseConnectionString).Create();
            var firstService = CreateService(firstDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var secondService = CreateService(secondDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 1, TimeSpan.Zero));

            var results = await Task.WhenAll(
                TryRefreshAsync(firstService, login.RefreshToken),
                TryRefreshAsync(secondService, login.RefreshToken));

            Assert.Equal(1, results.Count(success => success));
            Assert.Equal(1, Scalar<int>(
                setupDb,
                "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND revoked_at IS NULL",
                new SugarParameter("@familyId", familyId)));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse'"));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND replaced_by_token_hash IS NOT NULL", new SugarParameter("@familyId", familyId)));
            Assert.Equal(0, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse' AND username IS NULL"));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed'"));
        }
        finally
        {
        }
    }
    private static T Scalar<T>(SqlSugar.ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);
        if (scalar == null || scalar == DBNull.Value)
        {
            throw new InvalidOperationException("Expected scalar query to return a value.");
        }

        if (scalar is T value)
        {
            return value;
        }

        if (typeof(T) == typeof(string))
        {
            if (scalar is byte[] bytes)
            {
                return (T)(object)Encoding.UTF8.GetString(bytes);
            }

            if (scalar is Guid guid)
            {
                return (T)(object)guid.ToString("D");
            }

            var scalarText = scalar.ToString() ?? throw new InvalidOperationException("Scalar value could not be converted to string.");
            return (T)(object)scalarText;
        }

        if (scalar is byte[] textBytes && typeof(T) != typeof(string))
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(Encoding.UTF8.GetString(textBytes), System.Globalization.CultureInfo.InvariantCulture);
            }

            if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Parse(Encoding.UTF8.GetString(textBytes), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
    private static AuthService CreateService(
        SqlSugar.ISqlSugarClient db,
        AuthTokenOptions tokenOptions,
        DateTimeOffset now)
    {
        return new AuthService(
            new AuthRepository(db),
            new PasswordHasher(),
            new AccessTokenService(tokenOptions),
            new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy()),
            new FixedAuthClock(now),
            new SqlSugarUnitOfWork(db));
    }
    private static AuthTokenOptions TokenOptions()
    {
        return new AuthTokenOptions(
            "integration-test-secret-with-more-than-32-characters",
            "wecms-integration",
            TimeSpan.FromMinutes(15),
            TimeSpan.FromDays(7));
    }
    private static async Task<bool> TryRefreshAsync(AuthService service, string refreshToken)
    {
        try
        {
            await service.RefreshAsync(
                new RefreshTokenRequest(refreshToken),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            return true;
        }
        catch
        {
            return false;
        }
    }
    private static async Task<long> CreateUserAsync(
        SqlSugar.ISqlSugarClient db,
        string username,
        string password,
        DateTimeOffset now)
    {
        await db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user (
                username,
                display_name,
                password_hash,
                status,
                is_super_admin,
                must_change_password,
                security_stamp,
                permission_version,
                created_at,
                updated_at,
                deleted_at
            )
            VALUES (
                @username,
                @displayName,
                @passwordHash,
                'enabled',
                FALSE,
                FALSE,
                @securityStamp,
                0,
                @now,
                @now,
                NULL
            )
            """,
            new SugarParameter("@username", username),
            new SugarParameter("@displayName", $"{username} user"),
            new SugarParameter("@passwordHash", new PasswordHasher().Hash(password)),
            new SugarParameter("@securityStamp", System.Guid.NewGuid().ToString("N")),
            new SugarParameter("@now", now.UtcDateTime));

        return Scalar<long>(
            db,
            "SELECT id FROM sys_user WHERE username = @username",
            new SugarParameter("@username", username));
    }

    private static async Task<long> CreateRoleAsync(
        SqlSugar.ISqlSugarClient db,
        string code,
        string name,
        DateTimeOffset now)
    {
        await db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_role (
                code,
                name,
                status,
                is_builtin,
                is_locked,
                created_at,
                updated_at,
                deleted_at
            )
            VALUES (
                @code,
                @name,
                'enabled',
                FALSE,
                FALSE,
                @now,
                @now,
                NULL
            )
            """,
            new SugarParameter("@code", code),
            new SugarParameter("@name", name),
            new SugarParameter("@now", now.UtcDateTime));

        return Scalar<long>(
            db,
            "SELECT id FROM sys_role WHERE code = @code",
            new SugarParameter("@code", code));
    }

    private static async Task AssignRolePermissionsAsync(
        SqlSugar.ISqlSugarClient db,
        long roleId,
        IReadOnlyList<string> permissionCodes,
        DateTimeOffset now)
    {
        foreach (var permissionCode in permissionCodes)
        {
            await db.Ado.ExecuteCommandAsync(
                """
                INSERT INTO sys_role_permission (role_id, permission_id, created_at)
                SELECT @roleId, p.id, @now
                FROM sys_permission p
                WHERE p.code = @permissionCode
                """,
                new SugarParameter("@roleId", roleId),
                new SugarParameter("@permissionCode", permissionCode),
                new SugarParameter("@now", now.UtcDateTime));
        }
    }

    private static async Task AssignRoleMenusAsync(
        SqlSugar.ISqlSugarClient db,
        long roleId,
        IReadOnlyList<string> menuCodes,
        DateTimeOffset now)
    {
        foreach (var menuCode in menuCodes)
        {
            await db.Ado.ExecuteCommandAsync(
                """
                INSERT INTO sys_role_menu (role_id, menu_id, created_at)
                SELECT @roleId, m.id, @now
                FROM sys_menu m
                WHERE m.name = @menuCode
                  AND m.deleted_at IS NULL
                """,
                new SugarParameter("@roleId", roleId),
                new SugarParameter("@menuCode", menuCode),
                new SugarParameter("@now", now.UtcDateTime));
        }
    }

    private static Task AssignUserRoleAsync(
        SqlSugar.ISqlSugarClient db,
        long userId,
        long roleId,
        DateTimeOffset now)
    {
        return db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user_role (user_id, role_id, created_at)
            VALUES (@userId, @roleId, @now)
            """,
            new SugarParameter("@userId", userId),
            new SugarParameter("@roleId", roleId),
            new SugarParameter("@now", now.UtcDateTime));
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
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class FixedAuthClock : IAuthClock
    {
        public FixedAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

}
