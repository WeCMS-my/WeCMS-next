using Microsoft.Extensions.Caching.Memory;
using WeCms.Api.AccessProfiles;
using WeCms.Caching;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Tests.Unit.AccessControl;

public sealed class AccessProfileServiceTests
{
    [Fact]
    public async Task GetAsync_ComposesAccessProfileWithButtonPermissionsAndMenuTree()
    {
        var repository = new FakeAccessProfileRepository
        {
            PermissionVersion = 12,
            Roles = ["admin", "operator"],
            Permissions =
            [
                "identity:user:button:disable",
                "identity:user:list",
                "identity:user:button:create"
            ],
            Menus =
            [
                new MenuSummaryDto(2, 1, "menu", "sys.users", "/system/users", "system/users/index", "Users", null, null, 20, false, true, null, "identity:user:list", "enabled", true),
                new MenuSummaryDto(1, null, "catalog", "sys.system", "/system", null, "System", null, null, 10, false, false, null, null, "enabled", true)
            ]
        };
        var service = new AccessProfileService(repository);

        var profile = await service.GetAsync(99, isSuperAdmin: false, CancellationToken.None);

        Assert.Equal(12, profile.PermissionVersion);
        Assert.Equal(["admin", "operator"], profile.Roles);
        Assert.Equal(["identity:user:button:disable", "identity:user:list", "identity:user:button:create"], profile.Permissions);
        Assert.Equal(["identity:user:button:create", "identity:user:button:disable"], profile.Buttons);

        var root = Assert.Single(profile.Menus);
        Assert.Equal("sys.system", root.Code);
        Assert.Equal("sys.users", Assert.Single(root.Children).Code);
        Assert.Equal(99, repository.CapturedUserId);
        Assert.False(repository.CapturedIsSuperAdmin);
    }

    [Fact]
    public async Task GetAsync_CachesProfileForSamePermissionVersion()
    {
        var repository = new FakeAccessProfileRepository
        {
            PermissionVersions = new Queue<long>([12, 12]),
            Roles = ["admin"],
            Permissions = ["identity:user:list"],
            Menus =
            [
                new MenuSummaryDto(1, null, "catalog", "sys.system", "/system", null, "System", null, null, 10, false, false, null, null, "enabled", true)
            ]
        };
        var service = CreateCachedService(repository);

        var first = await service.GetAsync(99, isSuperAdmin: false, CancellationToken.None);
        var second = await service.GetAsync(99, isSuperAdmin: false, CancellationToken.None);

        Assert.Equal(12, first.PermissionVersion);
        Assert.Equal(12, second.PermissionVersion);
        Assert.Equal(2, repository.PermissionVersionCalls);
        Assert.Equal(1, repository.RoleCodeCalls);
        Assert.Equal(1, repository.PermissionCodeCalls);
        Assert.Equal(1, repository.VisibleMenuCalls);
    }

    [Fact]
    public async Task GetAsync_ReloadsProfileWhenPermissionVersionChanges()
    {
        var repository = new FakeAccessProfileRepository
        {
            PermissionVersions = new Queue<long>([12, 13]),
            Roles = ["admin"],
            Permissions = ["identity:user:list"],
            Menus =
            [
                new MenuSummaryDto(1, null, "catalog", "sys.system", "/system", null, "System", null, null, 10, false, false, null, null, "enabled", true)
            ]
        };
        var service = CreateCachedService(repository);

        _ = await service.GetAsync(99, isSuperAdmin: false, CancellationToken.None);
        var refreshed = await service.GetAsync(99, isSuperAdmin: false, CancellationToken.None);

        Assert.Equal(13, refreshed.PermissionVersion);
        Assert.Equal(2, repository.PermissionVersionCalls);
        Assert.Equal(2, repository.RoleCodeCalls);
        Assert.Equal(2, repository.PermissionCodeCalls);
        Assert.Equal(2, repository.VisibleMenuCalls);
    }

    private static CachedAccessProfileService CreateCachedService(FakeAccessProfileRepository repository)
    {
        var cacheOptions = new CacheOptions { ApplicationName = "wecms", EnvironmentName = "unit" };

        return new CachedAccessProfileService(
            new AccessProfileService(repository),
            repository,
            new MemoryCacheProvider(
                new MemoryCache(new MemoryCacheOptions()),
                new SystemTextJsonCacheSerializer(),
                cacheOptions),
            new DefaultCacheKeyBuilder(cacheOptions));
    }

    private sealed class FakeAccessProfileRepository : IAccessProfileRepository
    {
        public long PermissionVersion { get; init; }

        public Queue<long>? PermissionVersions { get; init; }

        public IReadOnlyList<string> Roles { get; init; } = [];

        public IReadOnlyList<string> Permissions { get; init; } = [];

        public IReadOnlyList<MenuSummaryDto> Menus { get; init; } = [];

        public long CapturedUserId { get; private set; }

        public bool CapturedIsSuperAdmin { get; private set; }

        public int PermissionVersionCalls { get; private set; }

        public int RoleCodeCalls { get; private set; }

        public int PermissionCodeCalls { get; private set; }

        public int VisibleMenuCalls { get; private set; }

        public Task<long> GetPermissionVersionAsync(long userId, CancellationToken cancellationToken)
        {
            PermissionVersionCalls++;
            CapturedUserId = userId;
            return Task.FromResult(PermissionVersions is { Count: > 0 } ? PermissionVersions.Dequeue() : PermissionVersion);
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            RoleCodeCalls++;
            return Task.FromResult(Roles);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            PermissionCodeCalls++;
            return Task.FromResult(Permissions);
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            VisibleMenuCalls++;
            CapturedIsSuperAdmin = isSuperAdmin;
            return Task.FromResult(Menus);
        }
    }
}
