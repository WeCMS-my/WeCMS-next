using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Modules.Identity.Services;

public interface IRefreshTokenRotationService
{
    Task<AuthSessionResult> RefreshAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
{
    private const string EnabledStatus = "enabled";
    private const int MaxRefreshTokenLength = 128;
    private const string RefreshConcurrentReplayEvent = "auth.refresh_concurrent_replay";
    private const string RefreshReuseEvent = "auth.refresh_reuse";
    private const string RefreshPath = "/api/v1/auth/refresh";
    private const string RefreshAuditAction = "refresh";
    private const string AuditResultSuccess = "success";
    private const string AuditResultFailed = "failed";
    private const string AuditResultBlocked = "blocked";
    private const string PasswordChangeRequiredEvent = "auth.password_change_required";
    private static readonly TimeSpan RefreshTokenConcurrentReuseWindow = TimeSpan.FromSeconds(2);

    private readonly IAuthRepository _repository;
    private readonly IAccessProfileService _accessProfileService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthAuditWriter _auditWriter;
    private readonly IAuthSecurityEventWriter _securityEventWriter;

    public RefreshTokenRotationService(
        IAuthRepository repository,
        IAccessProfileService accessProfileService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthClock clock,
        IUnitOfWork unitOfWork,
        IAuthAuditWriter auditWriter,
        IAuthSecurityEventWriter securityEventWriter)
    {
        _repository = repository;
        _accessProfileService = accessProfileService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _securityEventWriter = securityEventWriter;
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
            await _auditWriter.RecordAsync(
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
            await _auditWriter.RecordAsync(
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
            await _auditWriter.RecordAsync(
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
            await _auditWriter.RecordAsync(
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
            await _securityEventWriter.RecordAsync(
                PasswordChangeRequiredEvent,
                existingToken.UserId,
                existingToken.Username,
                requestContext,
                "warning",
                "Password change required before authentication is allowed.",
                now,
                cancellationToken);
            await _auditWriter.RecordAsync(
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
            existingToken.MustChangePassword,
            existingToken.SecurityStamp,
            PermissionVersion: existingToken.PermissionVersion);
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
            await _auditWriter.RecordAsync(
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
            await _auditWriter.RecordAsync(
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

        var accessProfile = await _accessProfileService.GetAsync(user.Id, user.IsSuperAdmin, cancellationToken);
        var menus = AuthAccessProfileMapper.ToAuthMenuTree(accessProfile.Menus);

        return new AuthSessionResult(
            new LoginResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                new AuthUserDto(user.Id, user.Username, user.DisplayName, user.IsSuperAdmin),
                accessProfile.PermissionVersion,
                accessProfile.Roles,
                accessProfile.Permissions,
                menus),
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt,
            newRefreshToken.ExpiresAt - now);
    }

    private async Task RecordRefreshReuseOrConcurrentAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (ShouldTreatAsConcurrentReplay(token, now))
        {
            await RecordRefreshSecurityEventAsync(token, requestContext, RefreshConcurrentReplayEvent, "Refresh token concurrent rotation replay detected.", now, cancellationToken);
            return;
        }

        await RecordRefreshReuseAsync(token, requestContext, now, cancellationToken);
    }

    private static bool ShouldTreatAsConcurrentReplay(RefreshTokenRecord token, DateTimeOffset now)
    {
        if (token.ReplacedByTokenHash is null || token.RevokedAt is null)
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
            await RecordRefreshSecurityEventAsync(token, requestContext, RefreshReuseEvent, "Refresh token reuse detected.", now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private Task RecordRefreshSecurityEventAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        string eventType,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _securityEventWriter.RecordAsync(
            eventType,
            token.UserId,
            token.Username,
            requestContext,
            "warning",
            message,
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
}
