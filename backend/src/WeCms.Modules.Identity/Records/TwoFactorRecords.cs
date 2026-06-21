namespace WeCms.Modules.Identity.Records;

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

public sealed record TwoFactorVerificationResult(bool Verified, bool IsReplay = false);

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
