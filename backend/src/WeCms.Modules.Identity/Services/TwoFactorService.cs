using WeCms.Shared;

namespace WeCms.Modules.Identity.Services;

public sealed class TwoFactorService : ITwoFactorService
{
    private readonly IUserTwoFactorRepository _repository;
    private readonly ITotpService _totpService;
    private readonly ISecretProtector _secretProtector;
    private readonly IRecoveryCodeService _recoveryCodeService;
    private readonly TwoFactorOptions _options;

    public TwoFactorService(
        IUserTwoFactorRepository repository,
        ITotpService totpService,
        ISecretProtector secretProtector,
        IRecoveryCodeService recoveryCodeService,
        TwoFactorOptions options)
    {
        _repository = repository;
        _totpService = totpService;
        _secretProtector = secretProtector;
        _recoveryCodeService = recoveryCodeService;
        _options = options;
    }

    public async Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new DomainException(ApiCodes.ValidationError, "User id is invalid.");
        }

        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new DomainException(ApiCodes.ValidationError, "Account name is required.");
        }

        var secret = _totpService.GenerateSecret();
        var recoveryCodes = _recoveryCodeService.GenerateCodes(_options.RecoveryCodeCount);

        await _repository.UpsertSetupAsync(
            new UserTwoFactorSetupRecord(
                userId,
                _secretProtector.Protect(secret),
                recoveryCodes.Hashes,
                now),
            cancellationToken);

        return new TwoFactorSetupResult(
            secret,
            _totpService.BuildOtpAuthUri(secret, accountName.Trim()),
            recoveryCodes.Codes);
    }

    public async Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await RequiredRecordAsync(userId, cancellationToken);
        var secret = _secretProtector.Unprotect(record.SecretCipher);
        var verification = _totpService.Verify(secret, code, now, record.LastTotpStep);
        if (!verification.IsValid || verification.UsedStep is null)
        {
            throw new DomainException(ApiCodes.ValidationError, "Two-factor code is invalid.");
        }

        await _repository.EnableAsync(new UserTwoFactorEnableRecord(userId, verification.UsedStep.Value, now), cancellationToken);

        return new TwoFactorConfirmResult(true);
    }

    public async Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await RequiredRecordAsync(userId, cancellationToken);
        if (!record.Enabled)
        {
            return new TwoFactorRecoveryCodeUseResult(false);
        }

        var result = _recoveryCodeService.TryConsume(code, record.RecoveryCodeHashes);
        if (!result.Consumed)
        {
            return new TwoFactorRecoveryCodeUseResult(false);
        }

        await _repository.UpdateRecoveryCodesAsync(
            new UserTwoFactorRecoveryCodeUpdateRecord(
                userId,
                result.RemainingHashes,
                record.RecoveryCodesUsedCount + 1,
                now),
            cancellationToken);

        return new TwoFactorRecoveryCodeUseResult(true);
    }

    public async Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await RequiredRecordAsync(userId, cancellationToken);
        if (!record.Enabled)
        {
            throw new DomainException(ApiCodes.BusinessError, "Two-factor authentication is not enabled.");
        }

        var recoveryCodes = _recoveryCodeService.GenerateCodes(_options.RecoveryCodeCount);
        await _repository.UpdateRecoveryCodesAsync(
            new UserTwoFactorRecoveryCodeUpdateRecord(
                userId,
                recoveryCodes.Hashes,
                0,
                now),
            cancellationToken);

        return new TwoFactorRecoveryCodeRegenerationResult(recoveryCodes.Codes);
    }

    public async Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await RequiredRecordAsync(userId, cancellationToken);
        if (!record.Enabled)
        {
            return new TwoFactorVerificationResult(false);
        }

        var secret = _secretProtector.Unprotect(record.SecretCipher);
        var verification = _totpService.Verify(secret, code, now, record.LastTotpStep);
        if (!verification.IsValid || verification.UsedStep is null)
        {
            return new TwoFactorVerificationResult(false, verification.IsReplay);
        }

        await _repository.UpdateLastTotpStepAsync(new UserTwoFactorTotpStepUpdateRecord(userId, verification.UsedStep.Value, now), cancellationToken);

        return new TwoFactorVerificationResult(true);
    }

    public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new DomainException(ApiCodes.ValidationError, "User id is invalid.");
        }

        return _repository.ClearAsync(userId, now, cancellationToken);
    }

    private async Task<UserTwoFactorRecord> RequiredRecordAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new DomainException(ApiCodes.ValidationError, "User id is invalid.");
        }

        return await _repository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Two-factor setup was not found.");
    }
}
