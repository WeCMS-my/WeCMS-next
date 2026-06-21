using WeCms.EventBus;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Events;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.Repositories;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Modules.Identity.Events;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.Organization;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Id;

namespace WeCms.Tests.Unit.EventBus;

public sealed class SystemFoundationEventPublishingTests
{
    [Fact]
    public async Task UserCreateAsync_WritesUserCreatedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = new UserService(
            new FakeUserRepository(operations),
            new FakePasswordHasher(),
            new FakeUnitOfWork(operations),
            new FakeTwoFactorService(),
            new FakeIdentityPermissionVersionService(),
            new FakeOrganizationLookupService(),
            outbox,
            new FixedIdGenerator());

        await service.CreateAsync(
            new CreateUserRequest("operator", "Operator", "Password@123", null, null, null, [3], []),
            UserContext(),
            CancellationToken.None);

        var evt = Assert.IsType<UserCreatedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(UserCreatedEvent.EventType, evt.Type);
        Assert.Equal(2, evt.UserId);
        Assert.Equal("trace", evt.TraceId);
        AssertWriteWasInsideTransaction(operations, UserCreatedEvent.EventType);
    }

    [Fact]
    public async Task UserDisableAsync_WritesUserDisabledEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = new UserService(
            new FakeUserRepository(operations),
            new FakePasswordHasher(),
            new FakeUnitOfWork(operations),
            new FakeTwoFactorService(),
            new FakeIdentityPermissionVersionService(),
            new FakeOrganizationLookupService(),
            outbox,
            new FixedIdGenerator());

        await service.DisableAsync(2, UserContext(), CancellationToken.None);

        var evt = Assert.IsType<UserDisabledEvent>(Assert.Single(outbox.Events));
        Assert.Equal(UserDisabledEvent.EventType, evt.Type);
        Assert.Equal(2, evt.UserId);
        AssertWriteWasInsideTransaction(operations, UserDisabledEvent.EventType);
    }

    [Fact]
    public async Task RoleAssignPermissionsAsync_WritesRolePermissionsChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = new RoleService(
            new FakeRoleRepository(operations),
            new FakeUnitOfWork(operations),
            new FakeAccessControlPermissionVersionService(),
            outbox,
            new FixedIdGenerator());

        await service.AssignPermissionsAsync(7, new AssignRolePermissionsRequest([10, 11]), RoleContext(), CancellationToken.None);

        var evt = Assert.IsType<RolePermissionsChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(RolePermissionsChangedEvent.EventType, evt.Type);
        Assert.Equal(7, evt.RoleId);
        Assert.Equal([10, 11], evt.PermissionIds);
        AssertWriteWasInsideTransaction(operations, RolePermissionsChangedEvent.EventType);
    }

    [Fact]
    public async Task RolePermissionsChangedEvent_EvictsAccessProfileCache()
    {
        var permissionVersionService = new FakeAccessControlPermissionVersionService();
        var handler = new RolePermissionsChangedCacheHandler(permissionVersionService);

        await handler.HandleAsync(
            new RolePermissionsChangedEvent(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Now(),
                "trace",
                null,
                7,
                [10, 11]),
            CancellationToken.None);

        Assert.True(permissionVersionService.BumpUsersByRoleCalled);
    }

    [Fact]
    public async Task MenuSortAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = new MenuService(
            new FakeMenuRepository(operations),
            new FakeUnitOfWork(operations),
            new FakeAccessControlPermissionVersionService(),
            outbox,
            new FixedIdGenerator());

        await service.SortAsync(
            new SortMenusRequest([new SortMenuItemRequest(8, null, 10), new SortMenuItemRequest(9, 8, 20)]),
            MenuContext(),
            CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(MenuChangedEvent.EventType, evt.Type);
        Assert.Equal([8, 9], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    [Fact]
    public async Task MenuCreateAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = CreateMenuService(operations, outbox);

        await service.CreateAsync(
            new CreateMenuRequest(null, "menu", "sys.users", "/system/users", "system/users/index", "Users", null, null, 10, false, false, null, null, "enabled"),
            MenuContext(),
            CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal([8], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    [Fact]
    public async Task MenuUpdateAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = CreateMenuService(operations, outbox);

        await service.UpdateAsync(
            8,
            new UpdateMenuRequest(null, "menu", "/system/users", "system/users/index", "Users", null, null, 10, false, false, null, null, "enabled"),
            MenuContext(),
            CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal([8], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    [Fact]
    public async Task MenuDeleteAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = CreateMenuService(operations, outbox);

        await service.DeleteAsync(8, MenuContext(), CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal([8], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    [Fact]
    public async Task MenuEnableAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = CreateMenuService(operations, outbox);

        await service.EnableAsync(8, MenuContext(), CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal([8], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    [Fact]
    public async Task MenuDisableAsync_WritesMenuChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new RecordingOutboxWriter(operations);
        var service = CreateMenuService(operations, outbox);

        await service.DisableAsync(8, MenuContext(), CancellationToken.None);

        var evt = Assert.IsType<MenuChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal([8], evt.MenuIds);
        AssertWriteWasInsideTransaction(operations, MenuChangedEvent.EventType);
    }

    private static void AssertWriteWasInsideTransaction(IReadOnlyList<string> operations, string eventType)
    {
        var orderedOperations = operations.ToList();
        var begin = orderedOperations.IndexOf("begin");
        var outbox = orderedOperations.IndexOf($"outbox:{eventType}");
        var commit = orderedOperations.IndexOf("commit");

        Assert.True(begin >= 0, string.Join(", ", operations));
        Assert.True(outbox > begin, string.Join(", ", operations));
        Assert.True(commit > outbox, string.Join(", ", operations));
    }

    private static UserRequestContext UserContext()
    {
        return new UserRequestContext(1, "admin", "192.168.101.199", "unit-test", "trace", Now());
    }

    private static RoleRequestContext RoleContext()
    {
        return new RoleRequestContext(1, "admin", "192.168.101.199", "unit-test", "trace", Now());
    }

    private static MenuRequestContext MenuContext()
    {
        return new MenuRequestContext(1, "admin", "192.168.101.199", "unit-test", "trace", Now());
    }

    private static MenuService CreateMenuService(List<string> operations, RecordingOutboxWriter outbox)
    {
        return new MenuService(
            new FakeMenuRepository(operations),
            new FakeUnitOfWork(operations),
            new FakeAccessControlPermissionVersionService(),
            outbox,
            new FixedIdGenerator());
    }

    private static DateTimeOffset Now()
    {
        return new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class RecordingOutboxWriter(List<string> operations) : IOutboxWriter
    {
        public List<IIntegrationEvent> Events { get; } = [];

        public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
            where TEvent : IIntegrationEvent
        {
            Events.Add(integrationEvent);
            operations.Add($"outbox:{integrationEvent.Type}");
            return Task.CompletedTask;
        }
    }

    private sealed class FixedIdGenerator : IIdGenerator
    {
        private int _next = 1;

        public string NewId()
        {
            return $"0000000000000000000000000000000{_next++}";
        }
    }

    private sealed class FakeUnitOfWork(List<string> operations) : IUnitOfWork
    {
        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            operations.Add("begin");
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(operations));
        }
    }

    private sealed class FakeTransactionContext(List<string> operations) : ITransactionContext
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            operations.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            operations.Add("rollback");
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeTwoFactorService : ITwoFactorService
    {
        public Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeOrganizationLookupService : IOrganizationLookupService
    {
        public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlySet<long>> ExistingPositionIdsAsync(IReadOnlyList<long> positionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(positionIds.ToHashSet());
    }

    private sealed class FakeIdentityPermissionVersionService : IIdentityPermissionVersionService
    {
        public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAccessControlPermissionVersionService : IAccessControlPermissionVersionService
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

    private sealed class FakeUserRepository(List<string> operations) : IUserRepository
    {
        public Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<UserSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<UserDetailDto?>(new UserDetailDto(id, "operator", "Operator", null, null, null, "enabled", false, 0, null, [3], [], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(roleIds.ToHashSet());
        public Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
        {
            operations.Add($"set-status:{status}");
            return Task.CompletedTask;
        }

        public Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RevokeUserRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplacePositionsAsync(long id, IReadOnlyList<long> positionIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<long>> ListLockedRoleIdsByUserAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([]);
        public Task<IReadOnlySet<long>> ExistingLockedRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(new HashSet<long>());
        public Task<int> CountEnabledUsersByRoleForUpdateAsync(long roleId, CancellationToken cancellationToken) => Task.FromResult(2);
        public Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordSecurityEventAsync(UserSecurityEventRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRoleRepository(List<string> operations) : IRoleRepository
    {
        public Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<RoleSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<RoleDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<RoleDetailDto?>(new RoleDetailDto(id, "editor", "Editor", "enabled", false, false, [10], [8], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptRoleId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlySet<long>> ExistingPermissionIdsAsync(IReadOnlyList<long> permissionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(permissionIds.ToHashSet());
        public Task<IReadOnlySet<long>> ExistingMenuIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(menuIds.ToHashSet());
        public Task<long> CreateAsync(RoleCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(7L);
        public Task UpdateAsync(RoleUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplacePermissionsAsync(long id, IReadOnlyList<long> permissionIds, DateTimeOffset now, CancellationToken cancellationToken)
        {
            operations.Add("replace-permissions");
            return Task.CompletedTask;
        }

        public Task ReplaceMenusAsync(long id, IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(RoleAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMenuRepository(List<string> operations) : IMenuRepository
    {
        public Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MenuSummaryDto>>([]);
        public Task<MenuDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<MenuDetailDto?>(new MenuDetailDto(id, null, "catalog", "sys.system", "/system", "layout.base", "System", null, null, 1, false, false, null, null, "enabled", false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptMenuId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<long> CreateAsync(MenuCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(8L);
        public Task UpdateAsync(MenuUpdateRecord record, CancellationToken cancellationToken)
        {
            operations.Add("update-menu");
            return Task.CompletedTask;
        }

        public Task SortAsync(IReadOnlyList<MenuSortRecord> records, CancellationToken cancellationToken)
        {
            operations.Add("sort-menus");
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
        {
            operations.Add("delete-menu");
            return Task.CompletedTask;
        }

        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
        {
            operations.Add($"set-status:{status}");
            return Task.CompletedTask;
        }

        public Task RecordAuditAsync(MenuAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
