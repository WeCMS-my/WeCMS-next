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
        var service = CreateService(repository);

        var profile = await service.GetAsync(99, CancellationToken.None);

        Assert.Equal(12, profile.PermissionVersion);
        Assert.Equal(["admin", "operator"], profile.Roles);
        Assert.Equal(["identity:user:button:disable", "identity:user:list", "identity:user:button:create"], profile.Permissions);
        Assert.Equal(["identity:user:button:create", "identity:user:button:disable"], profile.Buttons);

        var root = Assert.Single(profile.Menus);
        Assert.Equal("sys.system", root.Code);
        Assert.Equal("sys.users", Assert.Single(root.Children).Code);
        Assert.Equal(99, repository.CapturedUserId);
    }

    [Fact]
    public async Task GetAsync_CachesProfileByUserAndPermissionVersion()
    {
        var repository = new FakeAccessProfileRepository
        {
            PermissionVersion = 12,
            Roles = ["admin"],
            Permissions = ["identity:user:list"],
            Menus = []
        };
        var cache = new FakeAccessProfileCache();
        var service = CreateService(repository, cache);

        var first = await service.GetAsync(99, CancellationToken.None);
        var second = await service.GetAsync(99, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(2, repository.PermissionVersionCalls);
        Assert.Equal(1, repository.RoleCalls);
        Assert.Equal(1, repository.PermissionCalls);
        Assert.Equal(1, repository.MenuCalls);
        Assert.Equal(1, cache.SetCalls);
    }

    [Fact]
    public async Task GetAsync_ChangesCacheKeyWhenPermissionVersionChanges()
    {
        var repository = new FakeAccessProfileRepository
        {
            PermissionVersion = 12,
            Roles = ["admin"],
            Permissions = ["identity:user:list"],
            Menus = []
        };
        var cache = new FakeAccessProfileCache();
        var service = CreateService(repository, cache);

        _ = await service.GetAsync(99, CancellationToken.None);
        repository.PermissionVersion = 13;
        repository.Roles = ["operator"];
        var changed = await service.GetAsync(99, CancellationToken.None);

        Assert.Equal(["operator"], changed.Roles);
        Assert.Equal(2, repository.RoleCalls);
        Assert.Equal(2, cache.SetCalls);
    }

    [Fact]
    public async Task AccessProfileService_UsesVersionedCacheAbstraction()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl", "AccessProfiles", "AccessProfileService.cs"), TestContext.Current.CancellationToken);
        var cacheSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "AccessControl", "CachingAccessProfileCache.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("IAccessProfileCache", source, StringComparison.Ordinal);
        Assert.Contains("_cache.GetAsync(userId, permissionVersion", source, StringComparison.Ordinal);
        Assert.Contains("_cache.SetAsync(userId, profile", source, StringComparison.Ordinal);
        Assert.Contains("ICache", cacheSource, StringComparison.Ordinal);
        Assert.Contains("ICacheTenantAccessor", cacheSource, StringComparison.Ordinal);
        Assert.Contains("_tenantAccessor", cacheSource, StringComparison.Ordinal);
        Assert.Contains("permissionVersion", cacheSource, StringComparison.Ordinal);
    }

    private static AccessProfileService CreateService(
        IAccessProfileRepository repository,
        IAccessProfileCache? cache = null)
    {
        return new AccessProfileService(repository, cache ?? new FakeAccessProfileCache());
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class FakeAccessProfileRepository : IAccessProfileRepository
    {
        public long PermissionVersion { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = [];

        public IReadOnlyList<string> Permissions { get; set; } = [];

        public IReadOnlyList<MenuSummaryDto> Menus { get; set; } = [];

        public long CapturedUserId { get; private set; }

        public int PermissionVersionCalls { get; private set; }

        public int RoleCalls { get; private set; }

        public int PermissionCalls { get; private set; }

        public int MenuCalls { get; private set; }

        public Task<long> GetPermissionVersionAsync(long userId, CancellationToken cancellationToken)
        {
            PermissionVersionCalls++;
            CapturedUserId = userId;
            return Task.FromResult(PermissionVersion);
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            RoleCalls++;
            return Task.FromResult(Roles);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            PermissionCalls++;
            return Task.FromResult(Permissions);
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, CancellationToken cancellationToken)
        {
            MenuCalls++;
            return Task.FromResult(Menus);
        }
    }

    private sealed class FakeAccessProfileCache : IAccessProfileCache
    {
        private readonly Dictionary<string, AccessProfileDto> _values = new(StringComparer.Ordinal);

        public int SetCalls { get; private set; }

        public ValueTask<AccessProfileDto?> GetAsync(
            long userId,
            long permissionVersion,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_values.TryGetValue(Key(userId, permissionVersion), out var profile) ? profile : null);
        }

        public ValueTask SetAsync(
            long userId,
            AccessProfileDto profile,
            CancellationToken cancellationToken)
        {
            SetCalls++;
            _values[Key(userId, profile.PermissionVersion)] = profile;
            return ValueTask.CompletedTask;
        }

        private static string Key(long userId, long permissionVersion)
        {
            return $"{userId}:{permissionVersion}";
        }
    }
}
