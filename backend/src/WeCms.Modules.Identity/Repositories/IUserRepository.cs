using WeCms.Shared;

namespace WeCms.Modules.Identity.Repositories;

public interface IUserRepository
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken);

    Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken);

    Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken);

    Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken);

    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);

    Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken);

    Task RevokeUserRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplacePositionsAsync(long id, IReadOnlyList<long> positionIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<long>> ListLockedRoleIdsByUserAsync(long userId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingLockedRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken);

    Task<int> CountEnabledUsersByRoleForUpdateAsync(long roleId, CancellationToken cancellationToken);

    Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(UserSecurityEventRecord record, CancellationToken cancellationToken);
}
