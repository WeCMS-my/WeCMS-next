using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Repositories;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Roles;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsSuperAdmin()
    {
        var service = CreateService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DisableAsync_RejectsSuperAdmin()
    {
        var service = CreateService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = CreateService(new FakeRoleRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new RoleListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(2, new UpdateRoleRequest("Locked"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role cannot be updated.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(2, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role cannot be deleted.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_BumpsPermissionVersion_ForDeletedRole()
    {
        var permissionVersionService = new FakePermissionVersionService();
        var service = CreateService(new FakeRoleRepository(UnlockedRole()), permissionVersionService: permissionVersionService);

        await service.DeleteAsync(3, Context(), CancellationToken.None);

        Assert.True(permissionVersionService.BumpUsersByRoleCalled);
    }

    [Fact]
    public async Task DisableAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(2, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role cannot be disabled.", exception.Message);
    }

    [Fact]
    public async Task EnableAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.EnableAsync(2, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role cannot be enabled.", exception.Message);
    }

    [Fact]
    public async Task AssignPermissionsAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.AssignPermissionsAsync(2, new AssignRolePermissionsRequest([1]), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role permissions cannot be modified.", exception.Message);
    }

    [Fact]
    public async Task AssignMenusAsync_RejectsLockedRole()
    {
        var service = CreateService(new FakeRoleRepository(LockedRole()));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.AssignMenusAsync(2, new AssignRoleMenusRequest([1]), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role menus cannot be modified.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_AllowsUnlockedRole()
    {
        var service = CreateService(new FakeRoleRepository(UnlockedRole()));

        var response = await service.UpdateAsync(3, new UpdateRoleRequest("Editor Updated"), Context(), CancellationToken.None);

        Assert.Equal(3, response.Id);
    }

    private static RoleRequestContext Context()
    {
        return new RoleRequestContext(
            1,
            "admin",
            "192.168.101.199",
            "unit-test",
            "trace",
            new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private static RoleService CreateService(
        FakeRoleRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        FakePermissionVersionService? permissionVersionService = null)
    {
        return new RoleService(
            repository,
            unitOfWork ?? new FakeUnitOfWork(),
            permissionVersionService ?? new FakePermissionVersionService(),
            new WeCms.Tests.Unit.NullOutboxWriter(),
            new WeCms.Tests.Unit.FixedTestIdGenerator());
    }

    private static RoleDetailDto LockedRole()
    {
        return new RoleDetailDto(
            2,
            "security_admin",
            "Security Admin",
            "enabled",
            true,
            true,
            [1],
            [1],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);
    }

    private static RoleDetailDto UnlockedRole()
    {
        return new RoleDetailDto(
            3,
            "editor",
            "Editor",
            "enabled",
            false,
            false,
            [1],
            [1],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly RoleDetailDto _role;

        public FakeRoleRepository()
            : this(new RoleDetailDto(
                1,
                "super_admin",
                "Super Admin",
                "enabled",
                true,
                false,
                [1],
                [1],
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch))
        {
        }

        public FakeRoleRepository(RoleDetailDto role)
        {
            _role = role;
        }

        public Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<RoleSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<RoleDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<RoleDetailDto?>(_role with { Id = id });
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

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext());
        }
    }

    private sealed class FakeTransactionContext : ITransactionContext
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePermissionVersionService : IAccessControlPermissionVersionService
    {
        public bool BumpUsersByRoleCalled { get; private set; }

        public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            BumpUsersByRoleCalled = true;
            return Task.CompletedTask;
        }

        public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
