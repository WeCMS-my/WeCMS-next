using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public interface ILogoutTokenRevoker
{
    Task LogoutAsync(
        string refreshToken,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public sealed class LogoutTokenRevoker : ILogoutTokenRevoker
{
    private const int MaxRefreshTokenLength = 128;
    private const string LogoutSuccessEvent = "auth.logout";
    private const string LogoutUnknownTokenEvent = "auth.logout_unknown_token";
    private const string LogoutRevokedTokenEvent = "auth.logout_replay_attempt";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string LogoutAuditAction = "logout";
    private const string AuditResultSuccess = "success";
    private const string AuditResultFailed = "failed";

    private readonly IAuthRepository _repository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthClock _clock;
    private readonly IAuthAuditWriter _auditWriter;
    private readonly IAuthSecurityEventWriter _securityEventWriter;

    public LogoutTokenRevoker(
        IAuthRepository repository,
        IRefreshTokenService refreshTokenService,
        IAuthClock clock,
        IAuthAuditWriter auditWriter,
        IAuthSecurityEventWriter securityEventWriter)
    {
        _repository = repository;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _auditWriter = auditWriter;
        _securityEventWriter = securityEventWriter;
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
            await RecordSecurityEventAsync(
                null,
                null,
                requestContext,
                LogoutUnknownTokenEvent,
                "Unknown refresh token in logout request.",
                "warning",
                now,
                cancellationToken);
            await _auditWriter.RecordAsync(
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
            await RecordSecurityEventAsync(
                existingToken.UserId,
                existingToken.Username,
                requestContext,
                LogoutRevokedTokenEvent,
                "Revoked refresh token received during logout.",
                "warning",
                now,
                cancellationToken);
            await _auditWriter.RecordAsync(
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
        await _auditWriter.RecordAsync(
            existingToken.UserId,
            existingToken.Username,
            LogoutAuditAction,
            AuditResultSuccess,
            "Logout succeeded and token family revoked.",
            LogoutPath,
            requestContext,
            cancellationToken);
        await RecordSecurityEventAsync(
            existingToken.UserId,
            existingToken.Username,
            requestContext,
            LogoutSuccessEvent,
            "User logged out.",
            "info",
            now,
            cancellationToken);
    }

    private Task RecordSecurityEventAsync(
        long? userId,
        string? username,
        AuthRequestContext requestContext,
        string eventType,
        string message,
        string severity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _securityEventWriter.RecordAsync(
            eventType,
            userId,
            username,
            requestContext,
            severity,
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
