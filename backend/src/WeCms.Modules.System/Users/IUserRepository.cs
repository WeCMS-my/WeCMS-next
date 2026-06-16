using WeCms.Shared;

namespace WeCms.Modules.System.Users;

public interface IUserRepository
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken);

    Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken);

    Task<bool> DeptExistsAsync(long deptId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingPostIdsAsync(IReadOnlyList<long> postIds, CancellationToken cancellationToken);

    Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken);

    Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken);

    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);

    Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task ReplacePostsAsync(long id, IReadOnlyList<long> postIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> CountActiveSuperAdminsExceptAsync(long? exceptUserId, CancellationToken cancellationToken);

    Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken);
}
