using WeCms.Modules.Identity.Records;

namespace WeCms.Modules.Identity.Services;

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

public interface ITotpSecretEntropy
{
    byte[] GetBytes(int count);
}

public interface IRecoveryCodeEntropy
{
    byte[] GetBytes(int count);
}
