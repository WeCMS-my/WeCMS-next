namespace WeCms.Modules.System.TwoFactor;

public sealed record TwoFactorOptions(
    string SecretProtectionKey,
    string Issuer,
    int PeriodSeconds,
    int CodeDigits,
    int AllowedWindowSteps,
    int RecoveryCodeCount);

public sealed record TotpVerificationResult(bool IsValid, bool IsReplay, long? UsedStep);

public sealed record RecoveryCodeBundle(IReadOnlyList<string> Codes, IReadOnlyList<string> Hashes);

public sealed record RecoveryCodeConsumptionResult(bool Consumed, IReadOnlyList<string> RemainingHashes);

public sealed record TwoFactorSetupResult(string Secret, string OtpAuthUri, IReadOnlyList<string> RecoveryCodes);

public sealed record TwoFactorConfirmResult(bool Enabled);

public sealed record TwoFactorRecoveryCodeUseResult(bool Consumed);

public sealed record TwoFactorRecoveryCodeRegenerationResult(IReadOnlyList<string> RecoveryCodes);

public sealed record TwoFactorVerificationResult(bool Verified);

public sealed record UserTwoFactorRecord(
    long Id,
    long UserId,
    bool Enabled,
    string SecretCipher,
    DateTimeOffset? ConfirmedAt,
    long? LastTotpStep,
    IReadOnlyList<string> RecoveryCodeHashes,
    int RecoveryCodesUsedCount,
    bool ResetRequired,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UserTwoFactorSetupRecord(
    long UserId,
    string SecretCipher,
    IReadOnlyList<string> RecoveryCodeHashes,
    DateTimeOffset Now);

public sealed record UserTwoFactorEnableRecord(long UserId, long LastTotpStep, DateTimeOffset Now);

public sealed record UserTwoFactorRecoveryCodeUpdateRecord(
    long UserId,
    IReadOnlyList<string> RecoveryCodeHashes,
    int RecoveryCodesUsedCount,
    DateTimeOffset Now);

public sealed record UserTwoFactorTotpStepUpdateRecord(long UserId, long LastTotpStep, DateTimeOffset Now);

public interface ITotpService
{
    string GenerateSecret();

    string GenerateCode(string secret, DateTimeOffset now);

    TotpVerificationResult Verify(string secret, string code, DateTimeOffset now, long? lastTotpStep);

    long GetStep(DateTimeOffset now);

    string BuildOtpAuthUri(string secret, string accountName);
}

public interface ISecretProtector
{
    string Protect(string secret);

    string Unprotect(string cipher);
}

public interface IRecoveryCodeService
{
    RecoveryCodeBundle GenerateCodes(int count);

    RecoveryCodeConsumptionResult TryConsume(string code, IReadOnlyList<string> hashes);
}

public interface ITwoFactorService
{
    Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken);

    Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken);

    Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken);

    Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken);

    Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IUserTwoFactorRepository
{
    Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken);

    Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken);

    Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken);

    Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken);

    Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface ITotpSecretEntropy
{
    byte[] GetBytes(int count);
}

public interface IRecoveryCodeEntropy
{
    byte[] GetBytes(int count);
}
