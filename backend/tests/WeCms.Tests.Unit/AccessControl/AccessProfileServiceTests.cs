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

    private sealed class FakeAccessProfileRepository : IAccessProfileRepository
    {
        public long PermissionVersion { get; init; }

        public IReadOnlyList<string> Roles { get; init; } = [];

        public IReadOnlyList<string> Permissions { get; init; } = [];

        public IReadOnlyList<MenuSummaryDto> Menus { get; init; } = [];

        public long CapturedUserId { get; private set; }

        public bool CapturedIsSuperAdmin { get; private set; }

        public Task<long> GetPermissionVersionAsync(long userId, CancellationToken cancellationToken)
        {
            CapturedUserId = userId;
            return Task.FromResult(PermissionVersion);
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Roles);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Permissions);
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            CapturedIsSuperAdmin = isSuperAdmin;
            return Task.FromResult(Menus);
        }
    }
}
