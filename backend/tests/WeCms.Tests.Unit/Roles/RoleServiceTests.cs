using WeCms.Modules.System.Roles;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Roles;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsSuperAdmin()
    {
        var service = new RoleService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DisableAsync_RejectsSuperAdmin()
    {
        var service = new RoleService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new RoleService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new RoleListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private static RoleRequestContext Context()
    {
        return new RoleRequestContext(
            1,
            "admin",
            "127.0.0.1",
            "unit-test",
            "trace",
            new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        public Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<RoleSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<RoleDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<RoleDetailDto?>(new RoleDetailDto(
                id,
                "super_admin",
                "Super Admin",
                "enabled",
                true,
                [1],
                [1],
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch));
        }

        public Task<bool> CodeExistsAsync(string code, long? exceptRoleId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlySet<long>> ExistingPermissionIdsAsync(IReadOnlyList<long> permissionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(permissionIds.ToHashSet());
        public Task<IReadOnlySet<long>> ExistingMenuIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(menuIds.ToHashSet());
        public Task<long> CreateAsync(RoleCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(RoleUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplacePermissionsAsync(long id, IReadOnlyList<long> permissionIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceMenusAsync(long id, IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(RoleAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
