using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Auth;

public sealed partial class AuthServiceTests
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
    public async Task LoginAsync_FailedPasswordReturnsTooManyRequestsWhenFailureLimiterBlocks()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", false)
        };
        var limiter = new FakeLoginFailureLimiter { Decision = LoginFailureDecision.Blocked };
        var service = CreateService(repository, limiter);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.TooManyRequests, exception.Code);
        Assert.Equal("Invalid username or password.", exception.Message);
        Assert.Equal(1, repository.FailedLoginCount);
        Assert.Equal(1, limiter.RecordFailureCalls);
        Assert.Equal("login", repository.LastAuditAction);
        Assert.Equal("blocked", repository.LastAuditResult);
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
                false, true)
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

        Assert.NotEmpty(response.Response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.NotEqual(response.RefreshToken, repository.StoredRefreshTokenHash);
        Assert.Equal(64, repository.StoredRefreshTokenHash.Length);
        Assert.Equal(1, repository.SuccessfulLoginCount);
        Assert.Equal(["super_admin"], response.Response.Roles);
        Assert.Equal(["sys:system:secure-ping"], response.Response.Permissions);
        Assert.NotEmpty(response.Response.Menus);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("login", repository.LastAuditAction);
        Assert.Equal("success", repository.LastAuditResult);
        Assert.Equal(1, repository.LoginFailureLimiter.ResetCalls);
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

        var response = await service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None);

        Assert.NotEmpty(response.Response.AccessToken);
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
            () => service.RefreshAsync(new string('a', 129), RequestContext(), CancellationToken.None));

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
                true, issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                null,
                null,
                true)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
                true, issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 59, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_concurrent_replay", repository.LastSecurityEventType);
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
                true, issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 16, 0, 0, 1, TimeSpan.Zero),
                null)
        };
        var service = CreateService(repository, new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 2, TimeSpan.Zero)));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
                true, issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 0, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
            true, issued.Hash,
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
            true, issued.Hash,
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
        Assert.Equal("auth.refresh_concurrent_replay", repository.LastSecurityEventType);
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
                true, issued.Hash,
                "family-1",
                new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 15, 23, 59, 59, TimeSpan.Zero),
                "rotated-token")
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_concurrent_replay", repository.LastSecurityEventType);
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
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
            () => service.RefreshAsync(issued.Token, RequestContext(), CancellationToken.None));

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
                true, issued.Hash,
                "family-logout",
                new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero),
                null,
                null)
        };
        var service = CreateService(repository);

        await service.LogoutAsync(issued.Token, RequestContext(), CancellationToken.None);

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

        await service.LogoutAsync("missing-refresh-token", RequestContext(), CancellationToken.None);

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
                true, issued.Hash,
                "family-logout",
                new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 16, 0, 1, 0, TimeSpan.Zero),
                null)
        };
        var service = CreateService(repository);

        await service.LogoutAsync(issued.Token, RequestContext(), CancellationToken.None);

        Assert.Equal(string.Empty, repository.RevokedFamilyId);
        Assert.Equal("auth.logout_replay_attempt", repository.LastSecurityEventType);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(1, repository.AuditLogCount);
        Assert.Equal("logout", repository.LastAuditAction);
        Assert.Equal("failed", repository.LastAuditResult);
    }

}
