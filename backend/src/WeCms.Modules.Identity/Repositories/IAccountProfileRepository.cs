namespace WeCms.Modules.Identity.Repositories;

public interface IAccountProfileRepository
{
    Task<AccountProfileRecord?> GetAsync(long userId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, long exceptUserId, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, long exceptUserId, CancellationToken cancellationToken);

    Task UpdateProfileAsync(AccountProfileUpdateRecord record, CancellationToken cancellationToken);

    Task UpdatePasswordAsync(AccountPasswordUpdateRecord record, CancellationToken cancellationToken);

    Task RevokeRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task UpdateAvatarAsync(AccountAvatarUpdateRecord record, CancellationToken cancellationToken);

    Task RecordAuditAsync(AccountAuditRecord record, CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(AccountSecurityEventRecord record, CancellationToken cancellationToken);
}
