namespace WeCms.Shared.Security;

public interface ITwoFactorLoginService
{
    Task<TwoFactorLoginChallenge> CreateChallengeAsync(
        long userId,
        string username,
        CancellationToken cancellationToken);

    Task<TwoFactorLoginVerification> VerifyChallengeAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken);
}

public sealed record TwoFactorLoginChallenge(
    string ChallengeId,
    string Method,
    int ExpiresIn);

public sealed record TwoFactorLoginVerification(
    bool IsValid,
    long UserId);
