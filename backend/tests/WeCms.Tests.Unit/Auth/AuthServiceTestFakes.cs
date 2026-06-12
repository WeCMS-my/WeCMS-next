using System.Data.Common;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Id;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Tests.Unit.Auth;

internal sealed class TrackingAuthRepository : IAuthRepository
{
    public readonly Dictionary<string, IDbTransactionFacade?> Transactions = new();

    public UserRow? UserByUsernameResult;
    public UserRow? UserByIdResult;
    public RefreshTokenRow? GetRefreshTokenByHashResult;
    public long InsertRefreshTokenResult;
    public int UpdateUserLastLoginResult;
    public long InsertLoginLogResult;
    public long InsertSecurityEventResult = 1;
    public int RevokeRefreshTokenResult;
    public IReadOnlyList<CurrentUserMenuRow> MenuRows = Array.Empty<CurrentUserMenuRow>();
    public SecurityEventInsertRow? LastSecurityEvent;
    public int CountRecentFailedLoginAttemptsResult { get; set; }
    public int CountRecentSecurityEventsResult { get; set; }

    public Task<UserRow?> GetUserByUsernameAsync(
        IDbTransactionFacade? transaction,
        string username,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(GetUserByUsernameAsync)] = transaction;
        return Task.FromResult(UserByUsernameResult);
    }

    public Task<UserRow?> GetUserByIdAsync(
        IDbTransactionFacade? transaction,
        long id,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(GetUserByIdAsync)] = transaction;
        return Task.FromResult(UserByIdResult);
    }

    public Task<long> InsertRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        RefreshTokenInsertRow row,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(InsertRefreshTokenAsync)] = transaction;
        return Task.FromResult(InsertRefreshTokenResult);
    }

    public Task<RefreshTokenRow?> GetRefreshTokenByHashAsync(
        IDbTransactionFacade? transaction,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(GetRefreshTokenByHashAsync)] = transaction;
        return Task.FromResult(GetRefreshTokenByHashResult);
    }

    public Task<int> RevokeRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        long tokenId,
        DateTimeOffset revokedAt,
        long? replacedByTokenId,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(RevokeRefreshTokenAsync)] = transaction;
        return Task.FromResult(RevokeRefreshTokenResult);
    }

    public Task<int> RevokeRefreshTokenFamilyAsync(
        IDbTransactionFacade? transaction,
        string familyId,
        long exceptTokenId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(RevokeRefreshTokenFamilyAsync)] = transaction;
        return Task.FromResult(0);
    }

    public Task<long> InsertLoginLogAsync(
        IDbTransactionFacade? transaction,
        LoginLogInsertRow row,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(InsertLoginLogAsync)] = transaction;
        return Task.FromResult(InsertLoginLogResult);
    }

    public Task<long> InsertSecurityEventAsync(
        IDbTransactionFacade? transaction,
        SecurityEventInsertRow row,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(InsertSecurityEventAsync)] = transaction;
        LastSecurityEvent = row;
        return Task.FromResult(InsertSecurityEventResult);
    }

    public Task<int> CountRecentFailedLoginAttemptsAsync(
        IDbTransactionFacade? transaction,
        string? username,
        string? ipAddress,
        DateTimeOffset since,
        CancellationToken cancellationToken)
        => Task.FromResult(CountRecentFailedLoginAttemptsResult);

    public Task<int> CountRecentSecurityEventsAsync(
        IDbTransactionFacade? transaction,
        string eventType,
        long? userId,
        string? ipAddress,
        DateTimeOffset since,
        CancellationToken cancellationToken)
        => Task.FromResult(CountRecentSecurityEventsResult);

    public Task<IReadOnlyList<string>> GetUserRoleCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken)
        => Task.FromResult(Array.Empty<string>() as IReadOnlyList<string>);

    public Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken)
        => Task.FromResult(Array.Empty<string>() as IReadOnlyList<string>);

    public Task<IReadOnlyList<CurrentUserMenuRow>> GetUserMenusAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken)
        => Task.FromResult(MenuRows);

    public Task<int> UpdateUserLastLoginAsync(
        IDbTransactionFacade? transaction,
        long userId,
        DateTimeOffset loginAt,
        string ip,
        CancellationToken cancellationToken)
    {
        Transactions[nameof(UpdateUserLastLoginAsync)] = transaction;
        return Task.FromResult(UpdateUserLastLoginResult);
    }
}

internal sealed class TrackingUnitOfWork : IUnitOfWork
{
    private readonly IDbTransactionFacade _transaction = new TrackingTransactionFacade();
    public int BeginCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public IDbTransactionFacade Transaction => _transaction;

    public Task BeginAsync(CancellationToken cancellationToken) { BeginCount++; return Task.CompletedTask; }
    public Task CommitAsync(CancellationToken cancellationToken) { CommitCount++; return Task.CompletedTask; }
    public Task RollbackAsync(CancellationToken cancellationToken) { RollbackCount++; return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class TrackingTransactionFacade : IDbTransactionFacade
{
    public DbConnection Connection => null!;
    public DbTransaction? Inner => null;
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public Dictionary<string, bool> Verifications { get; } = new();

    public string Hash(string password) => password;

    public bool Verify(string password, string hash) =>
        Verifications.TryGetValue($"{hash}:{password}", out var ok) && ok;
}

internal sealed class FakeTokenService : ITokenService
{
    public string GenerateAccessToken(CurrentUser user) => "access-token";

    public TokenValidationResult ValidateAccessToken(string token) => new(false);
}

internal sealed class FakeTokenGenerator : ITokenGenerator
{
    private int _next = 1;

    public string GenerateRefreshToken() => $"refresh-token-{_next++}";
}

internal sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string token) => $"hashed:{token}";
}

internal sealed class FakeIdGenerator(Guid value) : IIdGenerator
{
    public Guid NewGuid() => value;
}

internal sealed class FixedClock : IClock
{
    private readonly DateTimeOffset _utcNow;

    public FixedClock(DateTimeOffset utcNow) => _utcNow = utcNow;

    public DateTimeOffset UtcNow => _utcNow;
}

internal sealed class AllowAuthRiskService : IAuthRiskService
{
    public Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(new AuthRiskDecision(false, false, "", "", 0));

    public Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(3);
}

internal sealed class CaptchaRequiredAuthRiskService : IAuthRiskService
{
    public Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(new AuthRiskDecision(
            false,
            true,
            "login_captcha_required",
            "username + IP 登录失败达到验证码阈值",
            1));

    public Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(3);
}

internal sealed class BlockAuthRiskService : IAuthRiskService
{
    public Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(new AuthRiskDecision(
            true,
            false,
            "login_rate_limited_username_ip",
            "username + IP 登录失败过多",
            3));

    public Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(3);
}

internal sealed class EscalatedRefreshReuseAuthRiskService : IAuthRiskService
{
    public Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(new AuthRiskDecision(false, false, "", "", 0));

    public Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken)
        => Task.FromResult(4);
}

internal sealed class PassingCaptchaService : ICaptchaService
{
    public Task<CaptchaChallenge> CreateChallengeAsync(CancellationToken cancellationToken)
        => Task.FromResult(new CaptchaChallenge("captcha-id", "data:image/svg+xml;base64,test", 300));

    public Task<bool> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken)
        => Task.FromResult(true);
}

internal sealed class FailingCaptchaService : ICaptchaService
{
    public Task<CaptchaChallenge> CreateChallengeAsync(CancellationToken cancellationToken)
        => Task.FromResult(new CaptchaChallenge("captcha-id", "data:image/svg+xml;base64,test", 300));

    public Task<bool> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken)
        => Task.FromResult(false);
}

internal sealed class FakeTwoFactorLoginService : ITwoFactorLoginService
{
    public Task<TwoFactorLoginChallenge> CreateChallengeAsync(
        long userId,
        string username,
        CancellationToken cancellationToken)
        => Task.FromResult(new TwoFactorLoginChallenge("two-factor-challenge", "one_time_code", 300));

    public Task<TwoFactorLoginVerification> VerifyChallengeAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken)
        => Task.FromResult(new TwoFactorLoginVerification(
            string.Equals(challengeId, "two-factor-challenge", StringComparison.Ordinal) &&
            string.Equals(code, "123456", StringComparison.Ordinal),
            1));
}
