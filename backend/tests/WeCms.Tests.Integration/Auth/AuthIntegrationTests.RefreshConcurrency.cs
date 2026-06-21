using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Security;
using WeCms.Modules.Security.SqlSugar.Repositories;
using WeCms.Shared;
using SqlSugar;
using System.Text;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Auth;

public sealed partial class AuthIntegrationTests
{
    [DbFact]
    public async Task AuthService_ProductionAdminSeedRequiresPasswordRotationBeforeLogin()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db, new SeedRunnerOptions("Production", "AdminRotation123!"));

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
            await PrepareDatabaseWithSeedsAsync(setupDb);

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
            await PrepareDatabaseWithSeedsAsync(db);

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

            await service.LogoutAsync(login.RefreshToken,
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
                () => service.RefreshAsync(login.RefreshToken,
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
            await PrepareDatabaseWithSeedsAsync(db);

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

            await service.LogoutAsync("invalid-refresh-token",
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
            await PrepareDatabaseWithSeedsAsync(setupDb);

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
            await PrepareDatabaseWithSeedsAsync(setupDb);

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
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_concurrent_replay'"));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_refresh_token WHERE family_id = @familyId AND replaced_by_token_hash IS NOT NULL", new SugarParameter("@familyId", familyId)));
            Assert.Equal(0, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'auth.refresh_reuse'"));
            Assert.Equal(1, Scalar<int>(setupDb, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'refresh' AND result = 'failed'"));
        }
        finally
        {
        }
    }
}
