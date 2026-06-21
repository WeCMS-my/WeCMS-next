using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Auth;

public sealed partial class AuthServiceTests
{
    private static AuthService CreateService(FakeAuthRepository repository)
    {
        repository.LoginFailureLimiter = new FakeLoginFailureLimiter();
        return CreateService(repository, repository.LoginFailureLimiter);
    }

    private static AuthService CreateService(FakeAuthRepository repository, FakeLoginFailureLimiter limiter)
    {
        repository.LoginFailureLimiter = limiter;
        var clock = new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
        var unitOfWork = new FakeUnitOfWork();
        var tokenOptions = new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7));
        var accessTokenService = new AccessTokenService(tokenOptions);
        var refreshTokenService = new RefreshTokenService(new FixedAuthTokenEntropy());
        var auditWriter = new AuthAuditWriter(repository, clock);
        var securityAlertService = new FakeSecurityAlertService();
        var securityEventWriter = new AuthSecurityEventWriter(repository, securityAlertService);
        var refreshTokenRotationService = new RefreshTokenRotationService(
            repository,
            repository,
            accessTokenService,
            refreshTokenService,
            clock,
            unitOfWork,
            auditWriter,
            securityEventWriter);
        var logoutTokenRevoker = new LogoutTokenRevoker(
            repository,
            refreshTokenService,
            clock,
            auditWriter,
            securityEventWriter);
        var sessionIssuer = new AuthSessionIssuer(repository, repository, accessTokenService, refreshTokenService, unitOfWork, limiter, clock);
        return new AuthService(
            repository,
            repository,
            new PasswordHasher(),
            clock,
            limiter,
            auditWriter,
            securityEventWriter,
            refreshTokenRotationService,
            logoutTokenRevoker,
            sessionIssuer,
            new AuthTwoFactorChallengeService(
                repository,
                new FakeTwoFactorRepository(),
                new FakeTwoFactorService(),
                new FakeAuthChallengeRepository(),
                new FixedAuthChallengeEntropy(),
                sessionIssuer,
                limiter,
                unitOfWork,
                clock,
                new TwoFactorChallengeOptions(TimeSpan.FromMinutes(5), 5),
                securityAlertService));
    }

    private static AuthService CreateService(IAuthRepository repository, IAuthClock clock)
    {
        var unitOfWork = new FakeUnitOfWork();
        var limiter = new FakeLoginFailureLimiter();
        var accessProfileService = repository as IAccessProfileService ?? EmptyAccessProfileService.Instance;
        var tokenOptions = new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7));
        var accessTokenService = new AccessTokenService(tokenOptions);
        var refreshTokenService = new RefreshTokenService(new FixedAuthTokenEntropy());
        var auditWriter = new AuthAuditWriter(repository, clock);
        var securityAlertService = new FakeSecurityAlertService();
        var securityEventWriter = new AuthSecurityEventWriter(repository, securityAlertService);
        var refreshTokenRotationService = new RefreshTokenRotationService(
            repository,
            accessProfileService,
            accessTokenService,
            refreshTokenService,
            clock,
            unitOfWork,
            auditWriter,
            securityEventWriter);
        var logoutTokenRevoker = new LogoutTokenRevoker(
            repository,
            refreshTokenService,
            clock,
            auditWriter,
            securityEventWriter);
        var sessionIssuer = new AuthSessionIssuer(repository, accessProfileService, accessTokenService, refreshTokenService, unitOfWork, limiter, clock);
        return new AuthService(
            repository,
            accessProfileService,
            new PasswordHasher(),
            clock,
            limiter,
            auditWriter,
            securityEventWriter,
            refreshTokenRotationService,
            logoutTokenRevoker,
            sessionIssuer,
            new AuthTwoFactorChallengeService(
                repository,
                new FakeTwoFactorRepository(),
                new FakeTwoFactorService(),
                new FakeAuthChallengeRepository(),
                new FixedAuthChallengeEntropy(),
                sessionIssuer,
                limiter,
                unitOfWork,
                clock,
                new TwoFactorChallengeOptions(TimeSpan.FromMinutes(5), 5),
                securityAlertService));
    }

    private static AuthRequestContext RequestContext()
    {
        return new AuthRequestContext("192.168.101.199", "unit-test");
    }

    private static async Task<AuthSessionResult?> TryRefreshAsync(AuthService service, string refreshToken)
    {
        try
        {
            return await service.RefreshAsync(refreshToken,
                RequestContext(),
                CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private sealed class FakeSecurityAlertService : IIdentitySecurityAlertService
    {
        public Task PublishIfRequiredAsync(
            string eventType,
            string severity,
            string message,
            string traceId,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthRepository : IAuthRepository, IAccessProfileService
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

        public FakeLoginFailureLimiter LoginFailureLimiter { get; set; } = new();

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

        public Task<AccessProfileDto> GetAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AccessProfileDto(
                User?.PermissionVersion ?? 0,
                Roles,
                Permissions,
                Permissions.Where(static permission => permission.Contains(":button:", StringComparison.Ordinal)).ToArray(),
                MenuTreeBuilder.Build(VisibleMenus)));
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

    private sealed class FakeLoginFailureLimiter : ILoginFailureLimiter
    {
        public LoginFailureDecision Decision { get; init; } = LoginFailureDecision.Allowed;

        public int RecordFailureCalls { get; private set; }

        public int ResetCalls { get; private set; }

        public Task<LoginFailureDecision> RecordFailureAsync(LoginFailureContext context, CancellationToken cancellationToken)
        {
            RecordFailureCalls++;
            return Task.FromResult(Decision);
        }

        public Task ResetAsync(string username, string ip, CancellationToken cancellationToken)
        {
            ResetCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ConcurrentReplayAuthRepository : IAuthRepository, IAccessProfileService
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

        public Task<AccessProfileDto> GetAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AccessProfileDto(0, [], [], [], []));
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

    private sealed class FixedAuthChallengeEntropy : IAuthChallengeEntropy
    {
        public string NewChallengeId() => "unit-test-two-factor-challenge-id-00000001";
    }

    private sealed class FakeTwoFactorRepository : IUserTwoFactorRepository
    {
        public Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<UserTwoFactorRecord?>(null);

        public Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTwoFactorService : ITwoFactorService
    {
        public Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(new TwoFactorRecoveryCodeUseResult(false));

        public Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(new TwoFactorVerificationResult(false));

        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAuthChallengeRepository : IAuthChallengeRepository
    {
        public Task CreateAsync(CreateAuthChallengeRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AuthChallengeRecord?> FindByChallengeIdAsync(string challengeId, CancellationToken cancellationToken) => Task.FromResult<AuthChallengeRecord?>(null);

        public Task<int> IncrementFailedAttemptsAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(1);

        public Task MarkFailedAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ConsumeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(true);
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

    private sealed class EmptyAccessProfileService : IAccessProfileService
    {
        public static readonly EmptyAccessProfileService Instance = new();

        public Task<AccessProfileDto> GetAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AccessProfileDto(0, [], [], [], []));
        }
    }
}
