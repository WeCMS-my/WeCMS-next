using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Shared;

namespace WeCms.Modules.Identity.Services;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string EnabledStatus = "enabled";
    private const int MaxUsernameLength = 64;
    private const int MaxPasswordLength = 256;
    private const string PasswordChangeRequiredEvent = "auth.password_change_required";
    private const string LoginPath = "/api/v1/auth/login";
    private const string LoginAuditAction = "login";
    private const string AuditResultFailed = "failed";
    private const string AuditResultBlocked = "blocked";
    private const string DummyPasswordHash = "wecms.pbkdf2-sha256.v1.600000.AQIDBAUGBwgJCgsMDQ4PEA==.wISuiW68N/gKfYEP+E70NpYBON359gMJuv/HMsmhWRA=";

    private readonly IAuthRepository _repository;
    private readonly IAccessProfileService _accessProfileService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthClock _clock;
    private readonly ILoginFailureLimiter _loginFailureLimiter;
    private readonly IAuthAuditWriter _auditWriter;
    private readonly IAuthSecurityEventWriter _securityEventWriter;
    private readonly IRefreshTokenRotationService _refreshTokenRotationService;
    private readonly ILogoutTokenRevoker _logoutTokenRevoker;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IAuthTwoFactorChallengeService _twoFactorChallengeService;

    public AuthService(
        IAuthRepository repository,
        IAccessProfileService accessProfileService,
        IPasswordHasher passwordHasher,
        IAuthClock clock,
        ILoginFailureLimiter loginFailureLimiter,
        IAuthAuditWriter auditWriter,
        IAuthSecurityEventWriter securityEventWriter,
        IRefreshTokenRotationService refreshTokenRotationService,
        ILogoutTokenRevoker logoutTokenRevoker,
        IAuthSessionIssuer sessionIssuer,
        IAuthTwoFactorChallengeService twoFactorChallengeService)
    {
        _repository = repository;
        _accessProfileService = accessProfileService;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _loginFailureLimiter = loginFailureLimiter;
        _auditWriter = auditWriter;
        _securityEventWriter = securityEventWriter;
        _refreshTokenRotationService = refreshTokenRotationService;
        _logoutTokenRevoker = logoutTokenRevoker;
        _sessionIssuer = sessionIssuer;
        _twoFactorChallengeService = twoFactorChallengeService;
    }

    public async Task<AuthSessionResult> LoginAsync(
        LoginRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestContext);

        var username = NormalizeRequired(request.Username, nameof(request.Username), MaxUsernameLength);
        var password = NormalizeRequired(request.Password, nameof(request.Password), MaxPasswordLength);
        var user = await _repository.FindUserByUsernameAsync(username, cancellationToken);
        var passwordHash = user?.PasswordHash ?? DummyPasswordHash;
        var passwordValid = _passwordHasher.Verify(password, passwordHash);
        if (user is null
            || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal)
            || !passwordValid)
        {
            var decision = await RecordFailedLoginAsync(username, user?.Id, requestContext, cancellationToken);
            await _auditWriter.RecordAsync(
                user?.Id,
                username,
                LoginAuditAction,
                decision.IsBlocked ? AuditResultBlocked : AuditResultFailed,
                decision.IsBlocked ? "Login blocked due to repeated invalid credentials." : "Login rejected due to invalid credentials.",
                LoginPath,
                requestContext,
                cancellationToken);
            if (decision.IsBlocked)
            {
                throw new DomainException(ApiCodes.TooManyRequests, InvalidCredentialsMessage);
            }

            throw new DomainException(ApiCodes.Unauthorized, InvalidCredentialsMessage);
        }

        var now = _clock.UtcNow;

        if (user.MustChangePassword)
        {
            await RecordPasswordChangeRequiredAsync(username, user.Id, requestContext, now, cancellationToken);
            await _auditWriter.RecordAsync(
                user.Id,
                username,
                LoginAuditAction,
                AuditResultBlocked,
                "Login blocked because password rotation is required.",
                LoginPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.BusinessError, "Password change required.");
        }

        if (await _twoFactorChallengeService.RequiresTwoFactorAsync(user.Id, cancellationToken))
        {
            return await _twoFactorChallengeService.CreateChallengeAsync(user, requestContext, now, cancellationToken);
        }

        return await _sessionIssuer.IssueAsync(
            user,
            requestContext,
            new AuthSessionAudit(LoginAuditAction, "Login succeeded and refresh token issued.", LoginPath),
            now,
            cancellationToken);
    }

    public async Task<AuthSessionResult> VerifyTwoFactorAsync(
        TwoFactorVerifyRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return await _twoFactorChallengeService.VerifyCodeAsync(request, requestContext, cancellationToken);
    }

    public async Task<AuthSessionResult> VerifyTwoFactorRecoveryCodeAsync(
        TwoFactorRecoveryCodeRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return await _twoFactorChallengeService.VerifyRecoveryCodeAsync(request, requestContext, cancellationToken);
    }

    public Task<AuthSessionResult> RefreshAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _refreshTokenRotationService.RefreshAsync(refreshToken, requestContext, cancellationToken);
    }

    public Task LogoutAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _logoutTokenRevoker.LogoutAsync(refreshToken, requestContext, cancellationToken);
    }

    public async Task<AuthMeResponse> MeAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _repository.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        var accessProfile = await _accessProfileService.GetAsync(user.Id, cancellationToken);
        var menus = AuthAccessProfileMapper.ToAuthMenuTree(accessProfile.Menus);

        return new AuthMeResponse(ToDto(user), accessProfile.PermissionVersion, accessProfile.Roles, accessProfile.Permissions, menus);
    }

    private async Task<LoginFailureDecision> RecordFailedLoginAsync(
        string username,
        long? userId,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _repository.RecordFailedLoginAsync(
            new FailedLoginRecord(username, requestContext.Ip, requestContext.UserAgent, "invalid_credentials", now),
            cancellationToken);
        await _securityEventWriter.RecordAsync(
            "auth.login_failed",
            userId,
            username,
            requestContext,
            "warning",
            InvalidCredentialsMessage,
            now,
            cancellationToken);
        return await _loginFailureLimiter.RecordFailureAsync(
            new LoginFailureContext(username, userId, requestContext.Ip, requestContext.UserAgent, now, requestContext.TraceId),
            cancellationToken);
    }

    private async Task RecordPasswordChangeRequiredAsync(
        string username,
        long userId,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _securityEventWriter.RecordAsync(
            PasswordChangeRequiredEvent,
            userId,
            username,
            requestContext,
            "warning",
            "Password change required before authentication is allowed.",
            now,
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

    private static AuthUserDto ToDto(AuthUserRecord user)
    {
        return new AuthUserDto(user.Id, user.Username, user.DisplayName);
    }
}
