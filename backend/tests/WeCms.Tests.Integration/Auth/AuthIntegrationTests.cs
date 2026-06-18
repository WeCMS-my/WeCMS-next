using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Users;
using WeCms.Modules.System.Permissions;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Auth;
using WeCms.Persistence.Modules.System.Permissions;
using WeCms.Persistence.Modules.System.Security;
using WeCms.Persistence.Modules.System.TwoFactor;
using WeCms.Persistence.Modules.System.Users;
using WeCms.Modules.System.Security;
using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;
using WeCms.Shared.Security;
using SqlSugar;
using System.Text;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Auth;

[Collection(nameof(SharedMySqlCollection))]
public sealed partial class AuthIntegrationTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task AuthService_LoginFailureAndSuccessPersistExpectedAuditAndTokenState()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

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
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'login_failure'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'failed'"));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("192.168.101.199", "integration"),
                CancellationToken.None);

            Assert.NotEmpty(login.Response.AccessToken);
            Assert.NotEmpty(login.RefreshToken);
            Assert.Equal(["super_admin"], login.Response.Roles);
            Assert.Equal(
                Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission"),
                login.Response.Permissions.Count);
            Assert.Contains("sys:system:secure-ping", login.Response.Permissions);
            Assert.Contains("sys:user:list", login.Response.Permissions);
            Assert.NotEqual(login.RefreshToken, Scalar<string>(db, "SELECT token_hash FROM sys_refresh_token LIMIT 1"));
            Assert.NotEmpty(login.Response.Menus);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin' AND last_login_at IS NOT NULL AND last_login_ip = '192.168.101.199'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'success'"));

            var principal = accessTokenService.Validate(login.Response.AccessToken, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            Assert.NotNull(principal);

            var me = await service.MeAsync(principal.UserId, CancellationToken.None);
            Assert.Equal("admin", me.User.Username);
            Assert.Equal(["super_admin"], me.Roles);
            Assert.Equal(login.Response.Permissions, me.Permissions);
            Assert.NotEmpty(me.Menus);

            var refreshed = await service.RefreshAsync(login.RefreshToken,
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
                () => service.RefreshAsync(login.RefreshToken,
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, reused.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_concurrent_replay'"));
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse'"));
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
                () => service.RefreshAsync(expiredLogin.RefreshToken,
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
                () => service.RefreshAsync(disabledLogin.RefreshToken,
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
    public async Task AuthService_TwoFactorTotpChallengeCompletesLoginAndRejectsReplay()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        await PrepareDatabaseWithSeedsAsync(db);
        var tokenOptions = TokenOptions();
        var loginNow = new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero);
        var verifyNow = loginNow.AddSeconds(30);
        var userId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin'");
        var setup = await EnableTwoFactorAsync(db, userId, loginNow.AddMinutes(-2));

        var login = await CreateService(db, tokenOptions, loginNow).LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        Assert.True(login.Response.RequiresTwoFactor);
        Assert.NotNull(login.Response.TwoFactorChallengeId);
        Assert.NotEmpty(login.Response.TwoFactorChallengeId);
        Assert.Empty(login.Response.AccessToken);
        Assert.Null(login.Response.User);
        Assert.Empty(login.RefreshToken);
        Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_refresh_token"));

        var code = new TotpService(TwoFactorOptions()).GenerateCode(setup.Secret, verifyNow);
        var verified = await CreateService(db, tokenOptions, verifyNow).VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(login.Response.TwoFactorChallengeId!, code),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        Assert.False(verified.Response.RequiresTwoFactor);
        Assert.NotEmpty(verified.Response.AccessToken);
        Assert.NotEmpty(verified.RefreshToken);
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_auth_challenge WHERE challenge_id = @challengeId AND status = 'consumed'", new SugarParameter("@challengeId", login.Response.TwoFactorChallengeId)));
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'two-factor-verify' AND result = 'success'"));

        await Assert.ThrowsAsync<DomainException>(() => CreateService(db, tokenOptions, verifyNow.AddSeconds(30)).VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(login.Response.TwoFactorChallengeId!, new TotpService(TwoFactorOptions()).GenerateCode(setup.Secret, verifyNow.AddSeconds(30))),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None));

        var replayLogin = await CreateService(db, tokenOptions, verifyNow).LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => CreateService(db, tokenOptions, verifyNow).VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(replayLogin.Response.TwoFactorChallengeId!, code),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None));

        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'two_factor_replay'"));
    }

    [DbFact]
    public async Task AuthService_TwoFactorRecoveryCodeCompletesLoginOnce()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        await PrepareDatabaseWithSeedsAsync(db);
        var tokenOptions = TokenOptions();
        var loginNow = new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero);
        var userId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin'");
        var setup = await EnableTwoFactorAsync(db, userId, loginNow.AddMinutes(-2));
        var challenge = await CreateService(db, tokenOptions, loginNow).LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        var verified = await CreateService(db, tokenOptions, loginNow.AddSeconds(10)).VerifyTwoFactorRecoveryCodeAsync(
            new TwoFactorRecoveryCodeRequest(challenge.Response.TwoFactorChallengeId!, setup.RecoveryCodes[0]),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        Assert.NotEmpty(verified.Response.AccessToken);
        Assert.Equal(9, Scalar<int>(db, "SELECT JSON_LENGTH(recovery_codes_hash_json) FROM sys_user_two_factor WHERE user_id = @userId", new SugarParameter("@userId", userId)));
        Assert.Equal(1, Scalar<int>(db, "SELECT recovery_codes_used_count FROM sys_user_two_factor WHERE user_id = @userId", new SugarParameter("@userId", userId)));
    }

    [DbFact]
    public async Task AuthService_TwoFactorChallengeRejectsExpiredAndOverLimitAttempts()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        await PrepareDatabaseWithSeedsAsync(db);
        var tokenOptions = TokenOptions();
        var loginNow = new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero);
        var userId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin'");
        await EnableTwoFactorAsync(db, userId, loginNow.AddMinutes(-2));

        var expiredChallenge = await CreateService(db, tokenOptions, loginNow).LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => CreateService(db, tokenOptions, loginNow.AddMinutes(6)).VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(expiredChallenge.Response.TwoFactorChallengeId!, "000000"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None));

        var limitedChallenge = await CreateService(db, tokenOptions, loginNow.AddMinutes(1), challengeOptions: new TwoFactorChallengeOptions(TimeSpan.FromMinutes(5), 2)).LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None);
        var limitedService = CreateService(db, tokenOptions, loginNow.AddMinutes(1).AddSeconds(30), challengeOptions: new TwoFactorChallengeOptions(TimeSpan.FromMinutes(5), 2));

        await Assert.ThrowsAsync<DomainException>(() => limitedService.VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(limitedChallenge.Response.TwoFactorChallengeId!, "000000"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None));
        await Assert.ThrowsAsync<DomainException>(() => limitedService.VerifyTwoFactorAsync(
            new TwoFactorVerifyRequest(limitedChallenge.Response.TwoFactorChallengeId!, "111111"),
            new AuthRequestContext("192.168.101.199", "integration"),
            CancellationToken.None));

        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_auth_challenge WHERE challenge_id = @challengeId AND status = 'failed'", new SugarParameter("@challengeId", limitedChallenge.Response.TwoFactorChallengeId)));
        Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_log WHERE reason = 'two_factor_failed'"));
        Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'two_factor_failed'"));
    }

    [DbFact]
    public async Task AuthService_RepeatedLoginFailuresCreateCountersSecurityEventsAndTemporaryBans()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);
        var service = CreateService(
            db,
            TokenOptions(),
            new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero),
            new LoginFailurePolicyOptions(true, TimeSpan.FromMinutes(10), 2, 3, 3, TimeSpan.FromMinutes(15)));

        var first = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), new AuthRequestContext("192.168.101.199", "integration", "trace-1"), CancellationToken.None));
        var second = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), new AuthRequestContext("192.168.101.199", "integration", "trace-2"), CancellationToken.None));
        var third = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), new AuthRequestContext("192.168.101.199", "integration", "trace-3"), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, first.Code);
        Assert.Equal(ApiCodes.TooManyRequests, second.Code);
        Assert.Equal(ApiCodes.TooManyRequests, third.Code);
        Assert.Equal("Invalid username or password.", third.Message);
        Assert.Equal(3, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_log WHERE username = 'admin' AND result = 'failed'"));
        Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_failure_counter WHERE target IN ('admin', '192.168.101.199')"));
        Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.login_rate_limited'"));
        Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_ban WHERE source = 'auth.login_failure' AND revoked_at IS NULL"));
    }

    [DbFact]
    public async Task AuthService_UserWithoutMenuTreePermissionStillReceivesVisibleMenusFromAuth()
    {
        var baseConnectionString = RequiredConnectionString();

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

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

            Assert.DoesNotContain("sys:menu:tree", login.Response.Permissions);
            Assert.NotEmpty(login.Response.Menus);

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
            await PrepareDatabaseWithSeedsAsync(db);

            var tokenOptions = TokenOptions();
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
            var accessTokenService = new AccessTokenService(tokenOptions);
            var userRepository = new UserRepository(db, new SecurityEventClassifier());
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

            var userService = CreateUserService(db, userRepository);
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
                () => service.RefreshAsync(login.RefreshToken,
                    new AuthRequestContext("192.168.101.199", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, refreshAfterDelete.Code);

            var principal = accessTokenService.Validate(login.Response.AccessToken, new DateTimeOffset(2026, 6, 16, 0, 0, 59, TimeSpan.Zero));
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
            await PrepareDatabaseWithSeedsAsync(db);

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
            var userService = CreateUserService(db);
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
                () => service.RefreshAsync(login.RefreshToken,
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
            await PrepareDatabaseWithSeedsAsync(db);

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

            var userService = CreateUserService(db);
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
                () => service.RefreshAsync(login.RefreshToken,
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

            var userService = CreateUserService(db);
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
                () => service.RefreshAsync(login.RefreshToken,
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
}
