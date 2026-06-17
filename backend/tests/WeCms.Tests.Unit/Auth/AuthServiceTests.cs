using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Menus;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_RejectsEmptyUsernameBeforeRepositoryLookup()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("", "password"), RequestContext(), CancellationToken.None));

        Assert.Equal(0, repository.FindUserCalls);
    }

    [Fact]
    public async Task LoginAsync_RejectsOverlongUsernameBeforeRepositoryLookup()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest(new string('a', 65), "password"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(0, repository.FindUserCalls);
    }

    [Fact]
    public async Task LoginAsync_FailedPasswordWritesAuditWithoutUserDisclosure()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", false)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("Invalid username or password.", exception.Message);
        Assert.Equal(1, repository.FailedLoginCount);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("login", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task LoginAsync_MustChangePasswordWritesSecurityEventAndRejectsLogin()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(
                1,
                "admin",
                "Administrator",
                PasswordHasher.HashForTest("correct"),
                "enabled",
                false,
                true)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "correct"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Password change required.", exception.Message);
        Assert.Equal("auth.password_change_required", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("login", repository.LastAuditAction);
        Assert.Equal("blocked", repository.LastAuditResult);
    }

    [Fact]
    public async Task LoginAsync_SuccessStoresRefreshTokenHashOnlyAndReturnsVisibleMenus()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", true),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"],
            VisibleMenus =
            [
                new MenuSummaryDto(1, null, "catalog", "sys.system", "/system", "layout.base", "System Management", "route.system", "material-symbols:settings", 100, false, false, null, null, "enabled", true),
                new MenuSummaryDto(2, 1, "menu", "sys.users", "/system/users", "system/users/index", "Users", "route.system.users", "material-symbols:group", 110, false, false, null, "sys:user:page", "enabled", true)
            ]
        };
        var service = CreateService(repository);

        var response = await service.LoginAsync(
            new LoginRequest("admin", "correct"),
            RequestContext(),
            CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.NotEqual(response.RefreshToken, repository.StoredRefreshTokenHash);
        Assert.Equal(64, repository.StoredRefreshTokenHash.Length);
        Assert.Equal(1, repository.SuccessfulLoginCount);
        Assert.Equal(["super_admin"], response.Roles);
        Assert.Equal(["sys:system:secure-ping"], response.Permissions);
        Assert.NotEmpty(response.Menus);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("login", repository.LastAuditAction);
        Assert.Equal("success", repository.LastAuditResult);
    }

    [Fact]
    public async Task MeAsync_ReturnsUserRolesPermissionsAndVisibleMenus()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", true),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"],
            VisibleMenus =
            [
                new MenuSummaryDto(1, null, "catalog", "sys.system", "/system", "layout.base", "System Management", "route.system", "material-symbols:settings", 100, false, false, null, null, "enabled", true),
                new MenuSummaryDto(2, 1, "menu", "sys.users", "/system/users", "system/users/index", "Users", "route.system.users", "material-symbols:group", 110, false, false, null, "sys:user:page", "enabled", true)
            ]
        };
        var service = CreateService(repository);

        var response = await service.MeAsync(1, CancellationToken.None);

        Assert.Equal("admin", response.User.Username);
        Assert.Equal(["super_admin"], response.Roles);
        Assert.Equal(["sys:system:secure-ping"], response.Permissions);
        Assert.NotEmpty(response.Menus);
    }

    [Fact]
    public async Task RefreshAsync_SuccessRotatesRefreshTokenHashOnly()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null, null),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"]
        };
        var service = CreateService(repository);

        var response = await service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(10, repository.RotatedOldRefreshTokenId);
        Assert.NotEqual(response.RefreshToken, repository.RotatedNewRefreshTokenHash);
        Assert.Equal(64, repository.RotatedNewRefreshTokenHash.Length);
        Assert.Equal("family-1", repository.RotatedFamilyId);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("success", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_RejectsOverlongRefreshTokenBeforeRepositoryLookup()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(new string('a', 129)), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(0, repository.FindRefreshTokenCalls);
    }

    [Fact]
    public async Task RefreshAsync_MustChangePasswordWritesSecurityEventAndRejectsRefresh()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(
                10,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                null,
                null,
                true)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Password change required.", exception.Message);
        Assert.Equal("auth.password_change_required", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("blocked", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_RevokedTokenRevokesFamilyAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero), null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentAlreadyRevokedFailureReturns401WithoutRevokingFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null, null),
            ThrowAlreadyRevokedOnRotation = true,
            RefreshTokenAfterRotationFailure = new RefreshTokenRecord(
                10,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 59, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentAlreadyRevokedFailureWithoutReplacedTokenStillRevokesFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero), null, null),
            ThrowAlreadyRevokedOnRotation = true,
            RefreshTokenAfterRotationFailure = new RefreshTokenRecord(
                10,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 16, 0, 0, 1, TimeSpan.Zero),
                null)
        };
        var service = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 2, TimeSpan.Zero)));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentAlreadyRevokedFailureLongAfterWindowStillRevokesFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null, null),
            ThrowAlreadyRevokedOnRotation = true,
            RefreshTokenAfterRotationFailure = new RefreshTokenRecord(
                10,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 0, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentRefreshLongAfterWindowStillRevokesFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new ConcurrentReplayAuthRepository(new RefreshTokenRecord(
            10,
            1,
            "admin",
            "Administrator",
            "enabled",
            true,
            issued.Hash,
            "family-1",
            new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
            null,
            null));

        var firstService = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero)));
        var secondService = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 10, 0, TimeSpan.Zero)));

        var results = await Task.WhenAll(
            TryRefreshAsync(firstService, issued.Token),
            TryRefreshAsync(secondService, issued.Token));

        Assert.Equal(1, results.Count(response => response is not null));
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentReplayWithinWindowDoesNotRevokeFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new ConcurrentReplayAuthRepository(new RefreshTokenRecord(
            10,
            1,
            "admin",
            "Administrator",
            "enabled",
            true,
            issued.Hash,
            "family-1",
            new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
            null,
            null));

        var firstService = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero)));
        var secondService = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 1, TimeSpan.Zero)));

        var results = await Task.WhenAll(
            TryRefreshAsync(firstService, issued.Token),
            TryRefreshAsync(secondService, issued.Token));

        Assert.Equal(1, results.Count(response => response is not null));
        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_InitialReadRevokedWithinConcurrentWindow_DoesNotRevokeFamily()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(
                10,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 59, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_AlreadyFullyRevokedFamilyStillWritesReuseSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero), null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredTokenReturnsUnauthorizedAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero), null, null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("auth.refresh_expired", repository.LastSecurityEventType);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task RefreshAsync_DisabledUserReturnsUnauthorizedAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "disabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null, null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("auth.refresh_user_disabled", repository.LastSecurityEventType);
        Assert.Equal("refresh", repository.LastAuditAction);
        Assert.Equal("blocked", repository.LastAuditResult);
    }

    [Fact]
    public async Task LogoutAsync_SuccessRevokesFamilyAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(
                12,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-logout",
                new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero),
                null,
                null)
        };
        var service = CreateService(repository);

        await service.LogoutAsync(new LogoutRequest(issued.Token), RequestContext(), CancellationToken.None);

        Assert.Equal("family-logout", repository.RevokedFamilyId);
        Assert.Equal("auth.logout", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("logout", repository.LastAuditAction);
        Assert.Equal("success", repository.LastAuditResult);
    }

    [Fact]
    public async Task LogoutAsync_UnknownTokenWritesSecurityEventWithoutRevocation()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        await service.LogoutAsync(new LogoutRequest("missing-refresh-token"), RequestContext(), CancellationToken.None);

        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.logout_unknown_token", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("logout", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    [Fact]
    public async Task LogoutAsync_RevokedTokenWritesSecurityEventWithoutRevocation()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(
                12,
                1,
                "admin",
                "Administrator",
                "enabled",
                true,
                issued.Hash,
                "family-logout",
                new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero),
                null)
        };
        var service = CreateService(repository);

        await service.LogoutAsync(new LogoutRequest(issued.Token), RequestContext(), CancellationToken.None);

        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.logout_replay_attempt", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("logout", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

    private static AuthService CreateService(FakeAuthRepository repository)
    {
        return new AuthService(
            repository,
            new PasswordHasher(),
            new AccessTokenService(new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7))),
            new RefreshTokenService(new FixedAuthTokenEntropy()),
            new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());
    }

    private static AuthService CreateService(IAuthRepository repository, IAuthClock clock)
    {
        return new AuthService(
            repository,
            new PasswordHasher(),
            new AccessTokenService(new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7))),
            new RefreshTokenService(new FixedAuthTokenEntropy()),
            clock,
            new FakeUnitOfWork());
    }

    private static AuthRequestContext RequestContext()
    {
        return new AuthRequestContext("192.168.101.199", "unit-test");
    }

    private static async Task<LoginResponse?> TryRefreshAsync(AuthService service, string refreshToken)
    {
        try
        {
            return await service.RefreshAsync(
                new RefreshTokenRequest(refreshToken),
                RequestContext(),
                CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public AuthUserRecord? User { get; init; }

        public RefreshTokenRecord? RefreshToken { get; set; }

        public IReadOnlyList<string> Roles { get; init; } = [];

        public IReadOnlyList<string> Permissions { get; init; } = [];

        public IReadOnlyList<MenuSummaryDto> VisibleMenus { get; init; } = [];

        public int FindUserCalls { get; private set; }

        public int FindRefreshTokenCalls { get; private set; }

        public int FailedLoginCount { get; private set; }

        public int SecurityEventCount { get; private set; }

        public int SuccessfulLoginCount { get; private set; }

        public int AuditLogCount { get; private set; }

        public string LastAuditAction { get; private set; } = string.Empty;

        public string LastAuditResult { get; private set; } = string.Empty;

        public string StoredRefreshTokenHash { get; private set; } = string.Empty;

        public long RotatedOldRefreshTokenId { get; private set; }

        public string RotatedNewRefreshTokenHash { get; private set; } = string.Empty;

        public string RotatedFamilyId { get; private set; } = string.Empty;

        public string RevokedFamilyId { get; private set; } = string.Empty;

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public RefreshTokenRecord? RefreshTokenAfterRotationFailure { get; init; }

        public bool ThrowAlreadyRevokedOnRotation { get; init; }

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            FindUserCalls++;
            return Task.FromResult(User?.Username == username ? User : null);
        }

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(User?.Id == userId ? User : null);
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            FindRefreshTokenCalls++;
            return Task.FromResult(RefreshToken?.TokenHash == tokenHash ? RefreshToken : null);
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Roles);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Permissions);
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            return Task.FromResult(VisibleMenus);
        }

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
        {
            FailedLoginCount++;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            return Task.CompletedTask;
        }

        public Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
        {
            AuditLogCount++;
            LastAuditAction = record.Action;
            LastAuditResult = record.Result;
            return Task.CompletedTask;
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
        {
            SuccessfulLoginCount++;
            StoredRefreshTokenHash = record.RefreshTokenHash;
            return Task.CompletedTask;
        }

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
        {
            if (ThrowAlreadyRevokedOnRotation)
            {
                if (RefreshTokenAfterRotationFailure is not null)
                {
                    RefreshToken = RefreshTokenAfterRotationFailure;
                }

                throw new RefreshTokenAlreadyRevokedException(record.FamilyId);
            }

            RotatedOldRefreshTokenId = record.OldRefreshTokenId;
            RotatedNewRefreshTokenHash = record.NewRefreshTokenHash;
            RotatedFamilyId = record.FamilyId;
            return Task.CompletedTask;
        }

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            RevokedFamilyId = familyId;
            return Task.CompletedTask;
        }
    }

    private sealed class ConcurrentReplayAuthRepository : IAuthRepository
    {
        private readonly object _lock = new();
        private readonly AuthUserRecord? _user;
        private RefreshTokenRecord _refreshToken;
        private int _rotationAttempts;

        public ConcurrentReplayAuthRepository(RefreshTokenRecord refreshToken)
        {
            _refreshToken = refreshToken;
            _user = new AuthUserRecord(
                refreshToken.UserId,
                refreshToken.Username,
                refreshToken.DisplayName,
                string.Empty,
                refreshToken.UserStatus,
                refreshToken.IsSuperAdmin);
        }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public int SecurityEventCount { get; private set; }

        public int AuditLogCount { get; private set; }

        public string LastAuditAction { get; private set; } = string.Empty;

        public string LastAuditResult { get; private set; } = string.Empty;

        public string RevokedFamilyId { get; private set; } = string.Empty;

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user?.Username == username ? _user : null);
        }

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user?.Id == userId ? _user : null);
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
            {
                return Task.FromResult(_refreshToken.TokenHash == tokenHash ? _refreshToken : null);
            }
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MenuSummaryDto>>([]);
        }

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            return Task.CompletedTask;
        }

        public Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
        {
            AuditLogCount++;
            LastAuditAction = record.Action;
            LastAuditResult = record.Result;
            return Task.CompletedTask;
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _rotationAttempts);
            if (attempt == 1)
            {
                lock (_lock)
                {
                    _refreshToken = _refreshToken with
                    {
                        RevokedAt = record.RotatedAt,
                        ReplacedByTokenHash = record.NewRefreshTokenHash
                    };
                }

                return Task.CompletedTask;
            }

            throw new RefreshTokenAlreadyRevokedException(record.FamilyId);
        }

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            RevokedFamilyId = familyId;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedAuthClock : IAuthClock
    {
        public FixedAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FixedAuthTokenEntropy : IAuthTokenEntropy
    {
        public byte[] GetBytes(int count)
        {
            return Enumerable.Range(1, count).Select(value => (byte)value).ToArray();
        }

        public string NewFamilyId()
        {
            return "test-family";
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext());
        }
    }

    private sealed class FakeTransactionContext : ITransactionContext
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
