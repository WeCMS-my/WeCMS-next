using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Modules.System.Menus;

namespace WeCms.Modules.System.Auth;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string EnabledStatus = "enabled";
    private const int MaxUsernameLength = 64;
    private const int MaxPasswordLength = 256;
    private const int MaxRefreshTokenLength = 128;
    private const string LogoutSuccessEvent = "auth.logout";
    private const string LogoutUnknownTokenEvent = "auth.logout_unknown_token";
    private const string LogoutRevokedTokenEvent = "auth.logout_replay_attempt";
    private const string PasswordChangeRequiredEvent = "auth.password_change_required";
    private const string LoginPath = "/api/v1/auth/login";
    private const string RefreshPath = "/api/v1/auth/refresh";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string AuthAuditModule = "auth";
    private const string AuthAuditResource = "auth";
    private const string LoginAuditAction = "login";
    private const string RefreshAuditAction = "refresh";
    private const string LogoutAuditAction = "logout";
    private const string AuditResultSuccess = "success";
    private const string AuditResultFailed = "failed";
    private const string AuditResultBlocked = "blocked";
    private static readonly TimeSpan RefreshTokenConcurrentReuseWindow = TimeSpan.FromSeconds(2);

    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoginFailureLimiter _loginFailureLimiter;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IAuthTwoFactorChallengeService _twoFactorChallengeService;

    public AuthService(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthClock clock,
        IUnitOfWork unitOfWork,
        ILoginFailureLimiter loginFailureLimiter,
        IAuthSessionIssuer sessionIssuer,
        IAuthTwoFactorChallengeService twoFactorChallengeService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _loginFailureLimiter = loginFailureLimiter;
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
        if (user is null
            || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal)
            || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            var decision = await RecordFailedLoginAsync(username, user?.Id, requestContext, cancellationToken);
            await RecordAuditLogAsync(
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
            await RecordAuditLogAsync(
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

    public async Task<AuthSessionResult> RefreshAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        var refreshTokenValue = NormalizeRequired(refreshToken, nameof(refreshToken), MaxRefreshTokenLength);
        var refreshTokenHash = _refreshTokenService.Hash(refreshTokenValue);
        var existingToken = await _repository.FindRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);
        if (existingToken is null)
        {
            await RecordAuditLogAsync(
                null,
                null,
                RefreshAuditAction,
                AuditResultFailed,
                "Refresh rejected because refresh token is invalid.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        var now = _clock.UtcNow;
        if (existingToken.RevokedAt is not null)
        {
            await RecordRefreshReuseOrConcurrentAsync(existingToken, requestContext, now, cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                RefreshAuditAction,
                AuditResultFailed,
                "Refresh rejected because token is already revoked.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        if (existingToken.ExpiresAt <= now)
        {
            await RecordRefreshSecurityEventAsync(existingToken, requestContext, "auth.refresh_expired", "Expired refresh token rejected.", now, cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                RefreshAuditAction,
                AuditResultFailed,
                "Refresh rejected because token is expired.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        if (!string.Equals(existingToken.UserStatus, EnabledStatus, StringComparison.Ordinal))
        {
            await RecordRefreshSecurityEventAsync(existingToken, requestContext, "auth.refresh_user_disabled", "Disabled user refresh rejected.", now, cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                RefreshAuditAction,
                AuditResultBlocked,
                "Refresh rejected because user is disabled.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        if (existingToken.MustChangePassword)
        {
            await RecordPasswordChangeRequiredAsync(existingToken.Username, existingToken.UserId, requestContext, now, cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                RefreshAuditAction,
                AuditResultBlocked,
                "Refresh blocked because password rotation is required.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.BusinessError, "Password change required.");
        }

        var user = new AuthUserRecord(
            existingToken.UserId,
            existingToken.Username,
            existingToken.DisplayName,
            PasswordHash: string.Empty,
            existingToken.UserStatus,
            existingToken.IsSuperAdmin,
            SecurityStamp: existingToken.SecurityStamp);
        var accessToken = _accessTokenService.Issue(user, now);
        var newRefreshToken = _refreshTokenService.Issue(now) with { FamilyId = existingToken.FamilyId };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.CompleteRefreshRotationAsync(
                new RefreshRotationRecord(
                    existingToken.Id,
                    existingToken.UserId,
                    newRefreshToken.Hash,
                    existingToken.FamilyId,
                    newRefreshToken.ExpiresAt,
                    now),
                cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                RefreshAuditAction,
                AuditResultSuccess,
                "Refresh succeeded and token family rotated.",
                RefreshPath,
                requestContext,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (RefreshTokenAlreadyRevokedException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var latestToken = await _repository.FindRefreshTokenByHashAsync(refreshTokenHash, cancellationToken)
                ?? existingToken;
            await RecordRefreshReuseOrConcurrentAsync(latestToken, requestContext, now, cancellationToken);
            await RecordAuditLogAsync(
                latestToken.UserId,
                latestToken.Username,
                RefreshAuditAction,
                AuditResultFailed,
                "Refresh rejected due to token reuse detection.",
                RefreshPath,
                requestContext,
                cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var roles = await _repository.ListRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(user.Id, cancellationToken);
        var menus = await ListVisibleMenusAsync(user.Id, user.IsSuperAdmin, cancellationToken);

        return new AuthSessionResult(
            new LoginResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                ToDto(user),
                roles,
                permissions,
                menus),
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt,
            newRefreshToken.ExpiresAt - now);
    }

    public async Task LogoutAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        var refreshTokenValue = NormalizeRequired(refreshToken, nameof(refreshToken), MaxRefreshTokenLength);
        var refreshTokenHash = _refreshTokenService.Hash(refreshTokenValue);
        var existingToken = await _repository.FindRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);
        var now = _clock.UtcNow;

        if (existingToken is null)
        {
            await RecordLogoutSecurityEventAsync(
                null,
                null,
                requestContext,
                LogoutUnknownTokenEvent,
                "Unknown refresh token in logout request.",
                "warning",
                now,
                cancellationToken);
            await RecordAuditLogAsync(
                null,
                null,
                LogoutAuditAction,
                AuditResultFailed,
                "Logout rejected because refresh token does not exist.",
                LogoutPath,
                requestContext,
                cancellationToken);
            return;
        }

        if (existingToken.RevokedAt is not null)
        {
            await RecordLogoutSecurityEventAsync(
                existingToken.UserId,
                existingToken.Username,
                requestContext,
                LogoutRevokedTokenEvent,
                "Revoked refresh token received during logout.",
                "warning",
                now,
                cancellationToken);
            await RecordAuditLogAsync(
                existingToken.UserId,
                existingToken.Username,
                LogoutAuditAction,
                AuditResultFailed,
                "Logout rejected because refresh token was already revoked.",
                LogoutPath,
                requestContext,
                cancellationToken);
            return;
        }

        await _repository.RevokeRefreshTokenFamilyAsync(existingToken.FamilyId, now, cancellationToken);
        await RecordAuditLogAsync(
            existingToken.UserId,
            existingToken.Username,
            LogoutAuditAction,
            AuditResultSuccess,
            "Logout succeeded and token family revoked.",
                LogoutPath,
            requestContext,
            cancellationToken);
        await RecordLogoutSecurityEventAsync(
            existingToken.UserId,
            existingToken.Username,
            requestContext,
            LogoutSuccessEvent,
            "User logged out.",
            "info",
            now,
            cancellationToken);
    }

    public async Task<AuthMeResponse> MeAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _repository.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        var roles = await _repository.ListRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(user.Id, cancellationToken);
        var menus = await ListVisibleMenusAsync(user.Id, user.IsSuperAdmin, cancellationToken);

        return new AuthMeResponse(ToDto(user), roles, permissions, menus);
    }

    private async Task<IReadOnlyList<MenuTreeDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
    {
        var menus = await _repository.ListVisibleMenusAsync(userId, isSuperAdmin, cancellationToken);
        return MenuTreeBuilder.Build(menus);
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
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                "auth.login_failed",
                userId,
                username,
                requestContext.Ip,
                "warning",
                InvalidCredentialsMessage,
                now),
            cancellationToken);
        return await _loginFailureLimiter.RecordFailureAsync(
            new LoginFailureContext(username, userId, requestContext.Ip, requestContext.UserAgent, now),
            cancellationToken);
    }

    private async Task RecordRefreshSecurityEventAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        string eventType,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                eventType,
                token.UserId,
                token.Username,
                requestContext.Ip,
                "warning",
                message,
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

    private async Task RecordLogoutSecurityEventAsync(
        long? userId,
        string? username,
        AuthRequestContext requestContext,
        string eventType,
        string message,
        string severity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                eventType,
                userId,
                username,
                requestContext.Ip,
                severity,
                message,
                now),
            cancellationToken);
    }

    private async Task RecordPasswordChangeRequiredAsync(
        string username,
        long userId,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                PasswordChangeRequiredEvent,
                userId,
                username,
                requestContext.Ip,
                "warning",
                "Password change required before authentication is allowed.",
                now),
            cancellationToken);
    }

    private async Task RecordRefreshReuseOrConcurrentAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (ShouldTreatAsConcurrentReplay(token, now))
        {
            await RecordRefreshSecurityEventAsync(token, requestContext, "auth.refresh_reuse", "Refresh token concurrent rotation replay detected.", now, cancellationToken);
            return;
        }

        await RecordRefreshReuseAsync(token, requestContext, now, cancellationToken);
    }

    private static bool ShouldTreatAsConcurrentReplay(RefreshTokenRecord token, DateTimeOffset now)
    {
        if (token.ReplacedByTokenHash is null)
        {
            return false;
        }

        if (token.RevokedAt is null)
        {
            return false;
        }

        return (now - token.RevokedAt.Value).Duration() <= RefreshTokenConcurrentReuseWindow;
    }

    private async Task RecordRefreshReuseAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.RevokeRefreshTokenFamilyAsync(token.FamilyId, now, cancellationToken);
            await RecordRefreshSecurityEventAsync(token, requestContext, "auth.refresh_reuse", "Refresh token reuse detected.", now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
        return new AuthUserDto(user.Id, user.Username, user.DisplayName, user.IsSuperAdmin);
    }
}
