namespace WeCms.Shared.Security;

public interface ICaptchaService
{
    Task<CaptchaChallenge> CreateChallengeAsync(CancellationToken cancellationToken);

    Task<bool> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken);
}

public sealed record CaptchaChallenge(
    string ChallengeId,
    string ImageData,
    int ExpiresIn);
