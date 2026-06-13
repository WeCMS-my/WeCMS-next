using WeCms.Shared.Id;
using WeCms.Shared.Security;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Time;

namespace WeCms.Tests.Unit.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldExecutePersistenceStepsInSameTransaction()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.UserByUsernameResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);

        var idGenerator = new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            clock,
            idGenerator);

        hasher.Verifications["hash-admin:Admin@123"] = true;
        repository.InsertRefreshTokenResult = 100;
        repository.UpdateUserLastLoginResult = 1;
        repository.InsertLoginLogResult = 999;

        var response = await service.LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            "127.0.0.1",
            "agent",
            default);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);

        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);

        var refreshCallTx = repository.Transactions["InsertRefreshTokenAsync"];
        Assert.NotNull(refreshCallTx);
        Assert.Same(refreshCallTx, repository.Transactions["UpdateUserLastLoginAsync"]);
        Assert.Same(refreshCallTx, repository.Transactions["InsertLoginLogAsync"]);
    }

    [Fact]
    public async Task RefreshAsync_ShouldQueryAndRotateTokenInSameTransaction()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 200;
        repository.RevokeRefreshTokenResult = 1;

        var idGenerator = new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            clock,
            idGenerator);

        var response = await service.RefreshAsync(
            new RefreshRequest("refresh-token"),
            "127.0.0.1",
            "agent",
            default);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);

        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);

        var queryTx = repository.Transactions["GetRefreshTokenByHashAsync"];
        Assert.NotNull(queryTx);
        Assert.Same(queryTx, repository.Transactions["GetUserByIdAsync"]);
        Assert.Same(queryTx, repository.Transactions["InsertRefreshTokenAsync"]);
        Assert.Same(queryTx, repository.Transactions["RevokeRefreshTokenAsync"]);
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokeRowsIsZero_ShouldRollbackAndThrowUnauthorized()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 200;
        repository.RevokeRefreshTokenResult = 0;

        var idGenerator = new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            clock,
            idGenerator);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshRequest("refresh-token"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.Unauthorized, ex.Code);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task LoginAsync_WhenInsertRefreshTokenFails_ShouldRollbackAndThrowSystemError()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.UserByUsernameResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 0;
        hasher.Verifications["hash-admin:Admin@123"] = true;

        var idGenerator = new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            clock,
            idGenerator);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "Admin@123"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.SystemError, ex.Code);
        Assert.Equal("登录会话创建失败", ex.Message);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task RefreshAsync_WhenInsertNewTokenFails_ShouldRollbackAndThrowSystemError()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 0;

        var idGenerator = new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            clock,
            idGenerator);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshRequest("refresh-token"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.SystemError, ex.Code);
        Assert.Equal("刷新令牌写入失败", ex.Message);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task LogoutAsync_WhenTokenExistsAndRevokeRowsIsNotOne_ShouldThrowSystemError()
    {
        var repository = new TrackingAuthRepository();
        var service = CreateService(repository);
        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "hashed:refresh-token",
            "family-id",
            new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero).AddHours(1),
            null,
            null);
        repository.RevokeRefreshTokenResult = 0;

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LogoutAsync("refresh-token", "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.SystemError, ex.Code);
        Assert.Equal("登出令牌吊销失败", ex.Message);
    }

    [Fact]
    public async Task LogoutAsync_WhenRefreshTokenDoesNotExist_ShouldAuditAndThrowUnauthorized()
    {
        var repository = new TrackingAuthRepository();
        var service = CreateService(repository);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LogoutAsync("refresh-token", "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.Unauthorized, ex.Code);
        Assert.Equal("刷新令牌无效或已失效", ex.Message);
        Assert.Equal("logout_refresh_invalid", repository.LastSecurityEvent?.EventType);
        Assert.Equal("127.0.0.1", repository.LastSecurityEvent?.IpAddress);
        Assert.Equal("agent", repository.LastSecurityEvent?.UserAgent);
    }

    [Fact]
    public async Task LogoutAsync_WhenRefreshTokenAlreadyRevoked_ShouldAuditAndThrowUnauthorized()
    {
        var repository = new TrackingAuthRepository();
        var service = CreateService(repository);
        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "hashed:refresh-token",
            "family-id",
            new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero).AddHours(1),
            new DateTimeOffset(2026, 06, 10, 9, 0, 0, TimeSpan.Zero),
            null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LogoutAsync("refresh-token", "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.Unauthorized, ex.Code);
        Assert.Equal("刷新令牌无效或已失效", ex.Message);
        Assert.Equal("logout_refresh_revoked", repository.LastSecurityEvent?.EventType);
        Assert.Equal(1, repository.LastSecurityEvent?.UserId);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnRoleMenuTree()
    {
        var repository = new TrackingAuthRepository();
        var service = CreateService(repository);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.MenuRows =
        [
            new CurrentUserMenuRow(10, null, "system", "系统管理", "layout.base", "/system", 1),
            new CurrentUserMenuRow(11, 10, "system.user", "用户管理", "view.system_user", "/system/user", 2),
            new CurrentUserMenuRow(12, 10, "system.role", "角色管理", "view.system_role", "/system/role", 1)
        ];

        var response = await service.GetCurrentUserAsync(1, default);

        var root = Assert.Single(response.Menus);
        Assert.Equal(10, root.Id);
        Assert.Equal("system", root.Code);
        Assert.Equal("系统管理", root.Name);
        Assert.Equal("layout.base", root.Component);
        Assert.Equal("/system", root.RoutePath);

        Assert.Collection(
            root.Children,
            first => Assert.Equal("system.role", first.Code),
            second => Assert.Equal("system.user", second.Code));
    }

    private static AuthService CreateService(TrackingAuthRepository repository)
    {
        return new AuthService(
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));
    }

    [Fact]
    public async Task LoginAsync_WhenUsernameIpRiskIsBlocked_ShouldReturnTooManyRequestsAndWriteHighSeverityEvent()
    {
        var repository = new TrackingAuthRepository();
        var service = new AuthService(
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new BlockAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.TooManyRequests, ex.Code);
        Assert.Equal("登录失败过多，请稍后再试", ex.Message);
        Assert.Equal("login_rate_limited_username_ip", repository.LastSecurityEvent?.EventType);
        Assert.Equal(3, repository.LastSecurityEvent?.Severity);
        Assert.Null(repository.Transactions.GetValueOrDefault(nameof(TrackingAuthRepository.GetUserByUsernameAsync)));
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokedTokenIsReusedRepeatedly_ShouldWriteEscalatedSecurityEvent()
    {
        var repository = new TrackingAuthRepository();
        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero).AddHours(1),
            new DateTimeOffset(2026, 06, 10, 9, 0, 0, TimeSpan.Zero),
            null);
        var service = new AuthService(
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new EscalatedRefreshReuseAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshRequest("refresh-token"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.Unauthorized, ex.Code);
        Assert.Equal("token_reuse", repository.LastSecurityEvent?.EventType);
        Assert.Equal(4, repository.LastSecurityEvent?.Severity);
    }

    [Fact]
    public async Task AuthRiskService_WhenUsernameIpFailuresReachThreshold_ShouldBlockLogin()
    {
        var repository = new TrackingAuthRepository
        {
            CountRecentFailedLoginAttemptsResult = 5
        };
        var riskService = new AuthRiskService(
            repository,
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)));

        var decision = await riskService.EvaluateLoginAsync("admin", "127.0.0.1", default);

        Assert.True(decision.IsBlocked);
        Assert.Equal("login_rate_limited_username_ip", decision.EventType);
        Assert.Equal(3, decision.Severity);
    }

    [Fact]
    public async Task LoginAsync_WhenCaptchaIsRequiredAndMissing_ShouldRejectBeforePasswordVerification()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var service = new AuthService(
            repository,
            hasher,
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new CaptchaRequiredAuthRiskService(),
            new FailingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.ValidationError, ex.Code);
        Assert.Equal("验证码无效或已过期", ex.Message);
        Assert.Empty(hasher.Verifications);
        Assert.Equal("login_captcha_failed", repository.LastSecurityEvent?.EventType);
    }

    [Fact]
    public async Task LoginAsync_WhenTwoFactorIsEnabled_ShouldReturnChallengeWithoutIssuingTokens()
    {
        var repository = new TrackingAuthRepository();
        repository.UserByUsernameResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1,
            true);
        var hasher = new FakePasswordHasher();
        hasher.Verifications["hash-admin:Admin@123"] = true;
        var service = new AuthService(
            repository,
            hasher,
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var response = await service.LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            "127.0.0.1",
            "agent",
            default);

        Assert.True(response.RequiresTwoFactor);
        Assert.Equal("two-factor-challenge", response.TwoFactorChallengeId);
        Assert.Null(response.AccessToken);
        Assert.Null(response.RefreshToken);
        Assert.Equal("two_factor_login_required", repository.LastSecurityEvent?.EventType);
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_WhenChallengeIsValid_ShouldIssueTokens()
    {
        var repository = new TrackingAuthRepository();
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1,
            true);
        repository.InsertRefreshTokenResult = 100;
        repository.UpdateUserLastLoginResult = 1;
        repository.InsertLoginLogResult = 999;
        var service = new AuthService(
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new TrackingUnitOfWork(),
            new AllowAuthRiskService(),
            new PassingCaptchaService(),
            new FakeTwoFactorLoginService(),
            new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero)),
            new FakeIdGenerator(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var response = await service.VerifyTwoFactorAsync(
            new VerifyTwoFactorRequest("two-factor-challenge", "123456"),
            "127.0.0.1",
            "agent",
            default);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);
    }

}
