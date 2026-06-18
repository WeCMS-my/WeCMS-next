using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Modules.System.TwoFactor;

namespace WeCms.Modules.System.Auth;

public interface IAuthTwoFactorChallengeService
{
    Task<bool> RequiresTwoFactorAsync(long userId, CancellationToken cancellationToken);

    Task<AuthSessionResult> CreateChallengeAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<AuthSessionResult> VerifyCodeAsync(
        TwoFactorVerifyRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<AuthSessionResult> VerifyRecoveryCodeAsync(
        TwoFactorRecoveryCodeRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public sealed class AuthTwoFactorChallengeService : IAuthTwoFactorChallengeService
{
    private const string EnabledStatus = "enabled";
    private const string TwoFactorChallengeType = "totp";
    private const string LoginPath = "/api/v1/auth/login";
    private const string TwoFactorVerifyPath = "/api/v1/auth/2fa/verify";
    private const string TwoFactorRecoveryCodePath = "/api/v1/auth/2fa/recovery-code";
    private const string LoginAuditAction = "login";
    private const string TwoFactorVerifyAuditAction = "two-factor-verify";
    private const string TwoFactorRecoveryCodeAuditAction = "two-factor-recovery-code";
    private const string AuditResultFailed = "failed";
    private const string AuditResultBlocked = "blocked";
    private const string AuditResultChallenge = "challenge";
    private const string AuthAuditModule = "auth";
    private const string AuthAuditResource = "auth";

    private readonly IAuthRepository _repository;
    private readonly IUserTwoFactorRepository _twoFactorRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuthChallengeRepository _challengeRepository;
    private readonly IAuthChallengeEntropy _challengeEntropy;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly ILoginFailureLimiter _loginFailureLimiter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthClock _clock;
    private readonly TwoFactorChallengeOptions _challengeOptions;

    public AuthTwoFactorChallengeService(
        IAuthRepository repository,
        IUserTwoFactorRepository twoFactorRepository,
        ITwoFactorService twoFactorService,
        IAuthChallengeRepository challengeRepository,
        IAuthChallengeEntropy challengeEntropy,
        IAuthSessionIssuer sessionIssuer,
        ILoginFailureLimiter loginFailureLimiter,
        IUnitOfWork unitOfWork,
        IAuthClock clock,
        TwoFactorChallengeOptions challengeOptions)
    {
        _repository = repository;
        _twoFactorRepository = twoFactorRepository;
        _twoFactorService = twoFactorService;
        _challengeRepository = challengeRepository;
        _challengeEntropy = challengeEntropy;
        _sessionIssuer = sessionIssuer;
        _loginFailureLimiter = loginFailureLimiter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _challengeOptions = challengeOptions;
    }

    public async Task<bool> RequiresTwoFactorAsync(long userId, CancellationToken cancellationToken)
    {
        var record = await _twoFactorRepository.GetByUserIdAsync(userId, cancellationToken);
        return record is { Enabled: true, ResetRequired: false };
    }

    public async Task<AuthSessionResult> CreateChallengeAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var challengeId = _challengeEntropy.NewChallengeId();
        var expiresAt = now.Add(_challengeOptions.Lifetime);

        await _challengeRepository.CreateAsync(
            new CreateAuthChallengeRecord(
                challengeId,
                user.Id,
                TwoFactorChallengeType,
                expiresAt,
                requestContext.Ip,
                requestContext.UserAgent,
                requestContext.TraceId,
                now),
            cancellationToken);
        await RecordAuditLogAsync(
            user.Id,
            user.Username,
            LoginAuditAction,
            AuditResultChallenge,
            "Login password verified and two-factor challenge issued.",
            LoginPath,
            requestContext,
            cancellationToken);
        await _loginFailureLimiter.ResetAsync(user.Username, requestContext.Ip, cancellationToken);

        return new AuthSessionResult(
            new LoginResponse(
                string.Empty,
                expiresAt,
                null,
                [],
                [],
                [],
                RequiresTwoFactor: true,
                TwoFactorChallengeId: challengeId,
                TwoFactorChallengeExpiresAt: expiresAt),
            string.Empty,
            expiresAt,
            TimeSpan.Zero);
    }

    public async Task<AuthSessionResult> VerifyCodeAsync(
        TwoFactorVerifyRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestContext);

        var challenge = await RequirePendingChallengeAsync(request.ChallengeId, requestContext, cancellationToken);
        var user = await RequireChallengeUserAsync(challenge, requestContext, cancellationToken);
        var code = NormalizeRequired(request.Code, nameof(request.Code), 16);
        var now = _clock.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var verified = await _twoFactorService.VerifyCodeAsync(user.Id, code, now, cancellationToken);
        if (!verified.Verified)
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecordTwoFactorFailureAsync(challenge, user, TwoFactorVerifyAuditAction, TwoFactorVerifyPath, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        var consumed = await _challengeRepository.ConsumeAsync(challenge.Id, now, cancellationToken);
        if (!consumed)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        var session = await _sessionIssuer.IssueInCurrentTransactionAsync(
            user,
            requestContext,
            new AuthSessionAudit(TwoFactorVerifyAuditAction, "Two-factor TOTP verification succeeded.", TwoFactorVerifyPath),
            now,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session;
    }

    public async Task<AuthSessionResult> VerifyRecoveryCodeAsync(
        TwoFactorRecoveryCodeRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestContext);

        var challenge = await RequirePendingChallengeAsync(request.ChallengeId, requestContext, cancellationToken);
        var user = await RequireChallengeUserAsync(challenge, requestContext, cancellationToken);
        var recoveryCode = NormalizeRequired(request.RecoveryCode, nameof(request.RecoveryCode), 64);
        var now = _clock.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var verified = await _twoFactorService.UseRecoveryCodeAsync(user.Id, recoveryCode, now, cancellationToken);
        if (!verified.Consumed)
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecordTwoFactorFailureAsync(challenge, user, TwoFactorRecoveryCodeAuditAction, TwoFactorRecoveryCodePath, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        var consumed = await _challengeRepository.ConsumeAsync(challenge.Id, now, cancellationToken);
        if (!consumed)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        var session = await _sessionIssuer.IssueInCurrentTransactionAsync(
            user,
            requestContext,
            new AuthSessionAudit(TwoFactorRecoveryCodeAuditAction, "Two-factor recovery code verification succeeded.", TwoFactorRecoveryCodePath),
            now,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session;
    }

    private async Task<AuthChallengeRecord> RequirePendingChallengeAsync(
        string challengeId,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var normalizedChallengeId = NormalizeRequired(challengeId, nameof(challengeId), 64);
        var challenge = await _challengeRepository.FindByChallengeIdAsync(normalizedChallengeId, cancellationToken);
        var now = _clock.UtcNow;
        if (challenge is null
            || !string.Equals(challenge.Status, "pending", StringComparison.Ordinal)
            || challenge.ExpiresAt <= now
            || challenge.FailedAttempts >= _challengeOptions.MaxFailedAttempts)
        {
            if (challenge is not null)
            {
                await RecordTwoFactorChallengeRejectedAsync(challenge.UserId, requestContext, now, cancellationToken);
            }

            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        return challenge;
    }

    private async Task<AuthUserRecord> RequireChallengeUserAsync(
        AuthChallengeRecord challenge,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var user = await _repository.FindUserByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal) || user.MustChangePassword)
        {
            await RecordTwoFactorChallengeRejectedAsync(challenge.UserId, requestContext, _clock.UtcNow, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Two-factor challenge is invalid.");
        }

        return user;
    }

    private async Task RecordTwoFactorFailureAsync(
        AuthChallengeRecord challenge,
        AuthUserRecord user,
        string auditAction,
        string requestPath,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var attempts = await _challengeRepository.IncrementFailedAttemptsAsync(challenge.Id, now, cancellationToken);
        if (attempts >= _challengeOptions.MaxFailedAttempts)
        {
            await _challengeRepository.MarkFailedAsync(challenge.Id, now, cancellationToken);
        }

        await _repository.RecordFailedLoginAsync(
            new FailedLoginRecord(user.Username, requestContext.Ip, requestContext.UserAgent, "two_factor_failed", now),
            cancellationToken);
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                "auth.two_factor_failed",
                user.Id,
                user.Username,
                requestContext.Ip,
                attempts >= _challengeOptions.MaxFailedAttempts ? "critical" : "warning",
                "Two-factor verification failed.",
                now),
            cancellationToken);
        await RecordAuditLogAsync(
            user.Id,
            user.Username,
            auditAction,
            attempts >= _challengeOptions.MaxFailedAttempts ? AuditResultBlocked : AuditResultFailed,
            "Two-factor verification failed.",
            requestPath,
            requestContext,
            cancellationToken);
    }

    private async Task RecordTwoFactorChallengeRejectedAsync(
        long userId,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                "auth.two_factor_challenge_rejected",
                userId,
                null,
                requestContext.Ip,
                "warning",
                "Two-factor challenge rejected.",
                now),
            cancellationToken);
    }

    private async Task RecordAuditLogAsync(
        long? userId,
        string? username,
        string action,
        string result,
        string detail,
        string requestPath,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        await _repository.RecordAuditLogAsync(
            new AuditLogRecord(
                userId,
                username,
                AuthAuditModule,
                AuthAuditResource,
                action,
                username,
                "POST",
                requestPath,
                requestContext.Ip,
                requestContext.UserAgent,
                requestContext.TraceId,
                result,
                detail,
                _clock.UtcNow),
            cancellationToken);
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
