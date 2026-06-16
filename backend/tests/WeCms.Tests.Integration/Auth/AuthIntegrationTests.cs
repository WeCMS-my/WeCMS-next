using WeCms.Modules.System.Auth;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Auth;
using WeCms.Shared;
using SqlSugar;
using System.Text;

namespace WeCms.Tests.Integration.Auth;

public sealed class AuthIntegrationTests
{
    [Fact]
    public async Task AuthService_LoginFailureAndSuccessPersistExpectedAuditAndTokenState()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_auth_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var accessTokenService = new AccessTokenService(tokenOptions);
            var refreshTokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var failed = await Assert.ThrowsAsync<DomainException>(
                () => service.LoginAsync(
                    new LoginRequest("admin", "wrong"),
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.Unauthorized, failed.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_log WHERE username = 'admin' AND result = 'failed'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.login_failed'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'failed'"));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("127.0.0.1", "integration"),
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
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin' AND last_login_at IS NOT NULL AND last_login_ip = '127.0.0.1'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'success'"));

            var principal = accessTokenService.Validate(login.AccessToken, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            Assert.NotNull(principal);

            var me = await service.MeAsync(principal.UserId, CancellationToken.None);
            Assert.Equal("admin", me.User.Username);
            Assert.Equal(["super_admin"], me.Roles);
            Assert.Equal(login.Permissions, me.Permissions);
            Assert.Empty(me.Menus);

            var refreshed = await service.RefreshAsync(
                new RefreshTokenRequest(login.RefreshToken),
                new AuthRequestContext("127.0.0.1", "integration"),
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
                    new AuthRequestContext("127.0.0.1", "integration"),
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
                new AuthRequestContext("127.0.0.1", "integration"),
                CancellationToken.None);
            var expiredHash = refreshTokenService.Hash(expiredLogin.RefreshToken);
            db.Ado.ExecuteCommand(
                "UPDATE sys_refresh_token SET expires_at = @expiresAt WHERE token_hash = @tokenHash",
                new SugarParameter("@expiresAt", new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)),
                new SugarParameter("@tokenHash", expiredHash));
            var expired = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(expiredLogin.RefreshToken),
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, expired.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_expired'"));
            Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed' AND target_id = 'admin'"));

            var disabledLogin = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("127.0.0.1", "integration"),
                CancellationToken.None);
            db.Ado.ExecuteCommand("UPDATE sys_user SET status = 'disabled' WHERE username = 'admin'");
            var disabled = await Assert.ThrowsAsync<DomainException>(
                () => service.RefreshAsync(
                    new RefreshTokenRequest(disabledLogin.RefreshToken),
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None));
            Assert.Equal(ApiCodes.Unauthorized, disabled.Code);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_user_disabled'"));
            Assert.Equal(2, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed' AND target_id = 'admin'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'blocked' AND target_id = 'admin'"));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task AuthService_ProductionAdminSeedRequiresPasswordRotationBeforeLogin()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_admin_password_change_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
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
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.BusinessError, exception.Code);
            Assert.Equal("Password change required.", exception.Message);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.password_change_required'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'login' AND result = 'blocked'"));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentRefreshAllowsOnlyOneSuccess()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_refresh_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            var connectionString = WithDatabase(baseConnectionString, databaseName);
            using var setupDb = new SqlSugarClientFactory(connectionString).Create();
            await new DbMigrationRunner(setupDb).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(setupDb).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var login = await CreateService(setupDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero))
                .LoginAsync(
                    new LoginRequest("admin", "Admin@123"),
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None);

            using var firstDb = new SqlSugarClientFactory(connectionString).Create();
            using var secondDb = new SqlSugarClientFactory(connectionString).Create();
            var firstService = CreateService(firstDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));
            var secondService = CreateService(secondDb, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero));

            var results = await Task.WhenAll(
                TryRefreshAsync(firstService, login.RefreshToken),
                TryRefreshAsync(secondService, login.RefreshToken));

            Assert.Equal(1, results.Count(success => success));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task AuthService_LogoutRevokesRefreshTokenFamily()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_logout_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("127.0.0.1", "integration"),
                CancellationToken.None);

            var loginRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                db,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", loginRefreshHash));

            await service.LogoutAsync(
                new LogoutRequest(login.RefreshToken),
                new AuthRequestContext("127.0.0.1", "integration"),
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
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task AuthService_LogoutUnknownTokenDoesNotAffectFamily()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_logout_unknown_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var tokenOptions = TokenOptions();
            var tokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
            var service = CreateService(db, tokenOptions, new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

            var login = await service.LoginAsync(
                new LoginRequest("admin", "Admin@123"),
                new AuthRequestContext("127.0.0.1", "integration"),
                CancellationToken.None);
            var loginRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                db,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", loginRefreshHash));

            await service.LogoutAsync(
                new LogoutRequest("invalid-refresh-token"),
                new AuthRequestContext("127.0.0.1", "integration"),
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
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentRefreshLongAfterWindowRevokesFamily()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_refresh_revoke_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            var connectionString = WithDatabase(baseConnectionString, databaseName);
            using var setupDb = new SqlSugarClientFactory(connectionString).Create();
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
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None);

            var oldRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                setupDb,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", oldRefreshHash));

            using var firstDb = new SqlSugarClientFactory(connectionString).Create();
            using var secondDb = new SqlSugarClientFactory(connectionString).Create();
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
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentRefreshWithinWindowKeepsFamilyPartiallyActive()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_refresh_replay_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            var connectionString = WithDatabase(baseConnectionString, databaseName);
            using var setupDb = new SqlSugarClientFactory(connectionString).Create();
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
                    new AuthRequestContext("127.0.0.1", "integration"),
                    CancellationToken.None);

            var oldRefreshHash = tokenService.Hash(login.RefreshToken);
            var familyId = Scalar<string>(
                setupDb,
                "SELECT family_id FROM sys_refresh_token WHERE token_hash = @tokenHash",
                new SugarParameter("@tokenHash", oldRefreshHash));

            using var firstDb = new SqlSugarClientFactory(connectionString).Create();
            using var secondDb = new SqlSugarClientFactory(connectionString).Create();
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
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
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
                new AuthRequestContext("127.0.0.1", "integration"),
                CancellationToken.None);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string RequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("WECMS_TEST_MYSQL_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING to run MySQL integration tests.");

        return connectionString;
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("database=", StringComparison.OrdinalIgnoreCase)
                && !part.StartsWith("initial catalog=", StringComparison.OrdinalIgnoreCase))
            .Append($"database={databaseName}");

        return string.Join(';', parts);
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
