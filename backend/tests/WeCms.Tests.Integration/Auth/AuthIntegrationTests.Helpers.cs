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
using SqlSugar;
using System.Text;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Auth;

public sealed partial class AuthIntegrationTests
{
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
        DateTimeOffset now,
        LoginFailurePolicyOptions? loginFailureOptions = null,
        TwoFactorChallengeOptions? challengeOptions = null)
    {
        var securityBanRepository = new SecurityBanRepository(db);
        var twoFactorOptions = new TwoFactorOptions("integration-two-factor-secret-key-32", "WeCMS", 30, 6, 1, 10);
        var twoFactorRepository = new UserTwoFactorRepository(db);
        var authRepository = new AuthRepository(db);
        var unitOfWork = new SqlSugarUnitOfWork(db);
        var clock = new FixedAuthClock(now);
        var accessTokenService = new AccessTokenService(tokenOptions);
        var refreshTokenService = new RefreshTokenService(tokenOptions.RefreshTokenLifetime, new AuthTokenEntropy());
        var loginFailureLimiter = new LoginFailureLimiter(
            new LoginFailureCounterRepository(db),
            new SecurityBanService(securityBanRepository),
            loginFailureOptions ?? new LoginFailurePolicyOptions(true, TimeSpan.FromMinutes(10), 5, 20, 10, TimeSpan.FromMinutes(15)));
        var twoFactorService = new TwoFactorService(
            twoFactorRepository,
            new TotpService(twoFactorOptions, new TwoFactorEntropy()),
            new SecretProtector(twoFactorOptions),
            new RecoveryCodeService(twoFactorOptions, new TwoFactorEntropy()),
            twoFactorOptions);
        var sessionIssuer = new AuthSessionIssuer(authRepository, accessTokenService, refreshTokenService, unitOfWork, loginFailureLimiter, clock);
        return new AuthService(
            authRepository,
            new PasswordHasher(),
            accessTokenService,
            refreshTokenService,
            clock,
            unitOfWork,
            loginFailureLimiter,
            sessionIssuer,
            new AuthTwoFactorChallengeService(
                authRepository,
                twoFactorRepository,
                twoFactorService,
                new AuthChallengeRepository(db),
                new AuthChallengeEntropy(),
                sessionIssuer,
                loginFailureLimiter,
                unitOfWork,
                clock,
                challengeOptions ?? new TwoFactorChallengeOptions(TimeSpan.FromMinutes(5), 5)));
    }

    private static UserService CreateUserService(SqlSugar.ISqlSugarClient db, UserRepository? repository = null)
    {
        var twoFactorOptions = TwoFactorOptions();
        var twoFactorService = new TwoFactorService(
            new UserTwoFactorRepository(db),
            new TotpService(twoFactorOptions, new TwoFactorEntropy()),
            new SecretProtector(twoFactorOptions),
            new RecoveryCodeService(twoFactorOptions, new TwoFactorEntropy()),
            twoFactorOptions);

        return new UserService(
            repository ?? new UserRepository(db),
            new PasswordHasher(),
            new SqlSugarUnitOfWork(db),
            twoFactorService);
    }

    private static async Task<TwoFactorSetupResult> EnableTwoFactorAsync(
        SqlSugar.ISqlSugarClient db,
        long userId,
        DateTimeOffset now)
    {
        var options = TwoFactorOptions();
        var repository = new UserTwoFactorRepository(db);
        var service = new TwoFactorService(
            repository,
            new TotpService(options, new TwoFactorEntropy()),
            new SecretProtector(options),
            new RecoveryCodeService(options, new TwoFactorEntropy()),
            options);
        var setup = await service.BeginSetupAsync(userId, "admin", now, CancellationToken.None);
        var code = new TotpService(options).GenerateCode(setup.Secret, now);
        await service.ConfirmSetupAsync(userId, code, now, CancellationToken.None);
        return setup;
    }

    private static TwoFactorOptions TwoFactorOptions()
    {
        return new TwoFactorOptions("integration-two-factor-secret-key-32", "WeCMS", 30, 6, 1, 10);
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
            await service.RefreshAsync(refreshToken,
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
