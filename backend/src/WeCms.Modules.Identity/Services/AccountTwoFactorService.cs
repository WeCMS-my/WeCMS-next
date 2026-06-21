using WeCms.Shared;

namespace WeCms.Modules.Identity.Services;

public sealed class AccountTwoFactorService : IAccountTwoFactorService
{
    private const string AuthAuditModule = "auth";
    private const string AccountTwoFactorResource = "account-2fa";
    private const string StatusEnabled = "enabled";
    private const string AuditResultSuccess = "success";
    private const string AuditResultFailed = "failed";
    private const string AuditResultBlocked = "blocked";
    private const string SetupPath = "/api/v1/account/2fa/setup";
    private const string ConfirmPath = "/api/v1/account/2fa/confirm";
    private const string DisablePath = "/api/v1/account/2fa/disable";
    private const string RegeneratePath = "/api/v1/account/2fa/recovery-codes/regenerate";

    private readonly IAuthRepository _authRepository;
    private readonly IUserTwoFactorRepository _twoFactorRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthClock _clock;

    public AccountTwoFactorService(
        IAuthRepository authRepository,
        IUserTwoFactorRepository twoFactorRepository,
        ITwoFactorService twoFactorService,
        IPasswordHasher passwordHasher,
        IAuthClock clock)
    {
        _authRepository = authRepository;
        _twoFactorRepository = twoFactorRepository;
        _twoFactorService = twoFactorService;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<AccountTwoFactorStatusResponse> StatusAsync(long userId, CancellationToken cancellationToken)
    {
        var record = await _twoFactorRepository.GetByUserIdAsync(userId, cancellationToken);
        return ToStatus(record);
    }

    public async Task<AccountTwoFactorSetupResponse> BeginSetupAsync(
        long userId,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var now = _clock.UtcNow;
        var setup = await _twoFactorService.BeginSetupAsync(user.Id, user.Username, now, cancellationToken);
        await RecordAuditAsync(user, "account-2fa-setup", AuditResultSuccess, "Account two-factor setup started.", SetupPath, requestContext, cancellationToken);
        await RecordSecurityEventAsync(user, "auth.account_2fa_setup_started", "Account two-factor setup started.", "info", requestContext, now, cancellationToken);

        return new AccountTwoFactorSetupResponse(setup.Secret, setup.OtpAuthUri, setup.RecoveryCodes);
    }

    public async Task<AccountTwoFactorStatusResponse> ConfirmAsync(
        long userId,
        AccountTwoFactorConfirmRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await RequireUserAsync(userId, cancellationToken);
        var now = _clock.UtcNow;
        await _twoFactorService.ConfirmSetupAsync(user.Id, NormalizeRequired(request.Code, nameof(request.Code), 16), now, cancellationToken);
        await RecordAuditAsync(user, "account-2fa-confirm", AuditResultSuccess, "Account two-factor setup confirmed.", ConfirmPath, requestContext, cancellationToken);
        await RecordSecurityEventAsync(user, "auth.account_2fa_enabled", "Account two-factor authentication enabled.", "info", requestContext, now, cancellationToken);

        return await StatusAsync(user.Id, cancellationToken);
    }

    public async Task DisableAsync(
        long userId,
        AccountTwoFactorDisableRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await RequireUserAsync(userId, cancellationToken);
        var now = _clock.UtcNow;
        if (!await VerifySensitiveOperationAsync(user, request.CurrentPassword, request.Code, now, cancellationToken))
        {
            await RecordSensitiveFailureAsync(user, "account-2fa-disable", DisablePath, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Current password or two-factor code is invalid.");
        }

        await _twoFactorService.ClearAsync(user.Id, now, cancellationToken);
        await RecordAuditAsync(user, "account-2fa-disable", AuditResultSuccess, "Account two-factor authentication disabled.", DisablePath, requestContext, cancellationToken);
        await RecordSecurityEventAsync(user, "auth.account_2fa_disabled", "Account two-factor authentication disabled.", "warning", requestContext, now, cancellationToken);
    }

    public async Task<AccountTwoFactorRecoveryCodesResponse> RegenerateRecoveryCodesAsync(
        long userId,
        AccountTwoFactorRegenerateRecoveryCodesRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await RequireUserAsync(userId, cancellationToken);
        var now = _clock.UtcNow;
        if (!await VerifySensitiveOperationAsync(user, request.CurrentPassword, request.Code, now, cancellationToken))
        {
            await RecordSensitiveFailureAsync(user, "account-2fa-recovery-codes-regenerate", RegeneratePath, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Current password or two-factor code is invalid.");
        }

        var result = await _twoFactorService.RegenerateRecoveryCodesAsync(user.Id, now, cancellationToken);
        await RecordAuditAsync(user, "account-2fa-recovery-codes-regenerate", AuditResultSuccess, "Account two-factor recovery codes regenerated.", RegeneratePath, requestContext, cancellationToken);
        await RecordSecurityEventAsync(user, "auth.account_2fa_recovery_codes_regenerated", "Account two-factor recovery codes regenerated.", "warning", requestContext, now, cancellationToken);

        return new AccountTwoFactorRecoveryCodesResponse(result.RecoveryCodes);
    }

    private async Task<bool> VerifySensitiveOperationAsync(
        AuthUserRecord user,
        string? currentPassword,
        string? code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(currentPassword) && _passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var verified = await _twoFactorService.VerifyCodeAsync(user.Id, NormalizeRequired(code, nameof(code), 16), now, cancellationToken);
        return verified.Verified;
    }

    private async Task<AuthUserRecord> RequireUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _authRepository.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, StatusEnabled, StringComparison.Ordinal))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return user;
    }

    private async Task RecordSensitiveFailureAsync(
        AuthUserRecord user,
        string action,
        string path,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RecordAuditAsync(user, action, AuditResultBlocked, "Sensitive two-factor account operation rejected.", path, requestContext, cancellationToken);
        await RecordSecurityEventAsync(user, "auth.account_2fa_sensitive_operation_rejected", "Sensitive two-factor account operation rejected.", "warning", requestContext, now, cancellationToken);
    }

    private async Task RecordAuditAsync(
        AuthUserRecord user,
        string action,
        string result,
        string detail,
        string path,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        await _authRepository.RecordAuditLogAsync(
            new AuditLogRecord(
                user.Id,
                user.Username,
                AuthAuditModule,
                AccountTwoFactorResource,
                action,
                user.Username,
                "POST",
                path,
                requestContext.Ip,
                requestContext.UserAgent,
                requestContext.TraceId,
                result,
                detail,
                _clock.UtcNow),
            cancellationToken);
    }

    private async Task RecordSecurityEventAsync(
        AuthUserRecord user,
        string eventType,
        string message,
        string severity,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _authRepository.RecordSecurityEventAsync(
            new SecurityEventRecord(eventType, user.Id, user.Username, requestContext.Ip, severity, message, now, requestContext.TraceId),
            cancellationToken);
    }

    private static AccountTwoFactorStatusResponse ToStatus(UserTwoFactorRecord? record)
    {
        return new AccountTwoFactorStatusResponse(
            record is { Enabled: true },
            record?.ConfirmedAt,
            record?.RecoveryCodeHashes.Count ?? 0,
            record?.ResetRequired ?? false);
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
