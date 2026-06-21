namespace WeCms.Modules.Identity.Repositories;

public interface IUserTwoFactorRepository
{
    Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken);

    Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken);

    Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken);

    Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken);

    Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
}
