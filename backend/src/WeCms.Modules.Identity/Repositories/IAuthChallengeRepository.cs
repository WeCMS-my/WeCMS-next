namespace WeCms.Modules.Identity.Repositories;

public interface IAuthChallengeRepository
{
    Task CreateAsync(CreateAuthChallengeRecord record, CancellationToken cancellationToken);

    Task<AuthChallengeRecord?> FindByChallengeIdAsync(string challengeId, CancellationToken cancellationToken);

    Task<int> IncrementFailedAttemptsAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task MarkFailedAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> ConsumeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
}
