using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Users;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new UserService(new FakeUserRepository(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new UserListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsSelfDelete()
    {
        var service = new UserService(new FakeUserRepository(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(actorUserId: 1), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DisableAsync_RejectsLastSuperAdmin()
    {
        var repository = new FakeUserRepository { ActiveSuperAdminsExceptTarget = 0 };
        var service = new UserService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    private static UserRequestContext Context(long actorUserId)
    {
        return new UserRequestContext(
            actorUserId,
            "admin",
            "127.0.0.1",
            "unit-test",
            "trace",
            new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hash:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == Hash(password);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public int ActiveSuperAdminsExceptTarget { get; init; } = 1;

        public Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<UserSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserDetailDto?>(new UserDetailDto(
                id,
                "admin",
                "Administrator",
                null,
                null,
                null,
                "enabled",
                true,
                0,
                null,
                [1],
                [],
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch));
        }

        public Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> DeptExistsAsync(long deptId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(roleIds.ToHashSet());
        public Task<IReadOnlySet<long>> ExistingPostIdsAsync(IReadOnlyList<long> postIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(postIds.ToHashSet());
        public Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplacePostsAsync(long id, IReadOnlyList<long> postIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> CountActiveSuperAdminsExceptAsync(long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(ActiveSuperAdminsExceptTarget);
        public Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
