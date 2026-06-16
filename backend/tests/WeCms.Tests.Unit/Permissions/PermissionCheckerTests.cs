using WeCms.Modules.System.Permissions;

namespace WeCms.Tests.Unit.Permissions;

public sealed class PermissionCheckerTests
{
    [Fact]
    public async Task CheckAsync_ReturnsUserDisabledWhenUserIsMissing()
    {
        var repository = new FakePermissionRepository(null, hasPermission: true);
        var checker = new PermissionChecker(repository);

        var result = await checker.CheckAsync(42, SystemPermissions.SecurePing, CancellationToken.None);

        Assert.Equal(PermissionCheckResult.UserDisabled, result);
    }

    [Fact]
    public async Task CheckAsync_ReturnsUserDisabledWhenUserStatusIsNotEnabled()
    {
        var repository = new FakePermissionRepository(new PermissionUserRecord(42, "disabled"), hasPermission: true);
        var checker = new PermissionChecker(repository);

        var result = await checker.CheckAsync(42, SystemPermissions.SecurePing, CancellationToken.None);

        Assert.Equal(PermissionCheckResult.UserDisabled, result);
    }

    [Fact]
    public async Task CheckAsync_ReturnsForbiddenWhenEnabledUserHasNoPermission()
    {
        var repository = new FakePermissionRepository(new PermissionUserRecord(42, "enabled"), hasPermission: false);
        var checker = new PermissionChecker(repository);

        var result = await checker.CheckAsync(42, SystemPermissions.SecurePing, CancellationToken.None);

        Assert.Equal(PermissionCheckResult.Forbidden, result);
    }

    [Fact]
    public async Task CheckAsync_ReturnsAllowedWhenEnabledUserHasPermission()
    {
        var repository = new FakePermissionRepository(new PermissionUserRecord(42, "enabled"), hasPermission: true);
        var checker = new PermissionChecker(repository);

        var result = await checker.CheckAsync(42, SystemPermissions.SecurePing, CancellationToken.None);

        Assert.Equal(PermissionCheckResult.Allowed, result);
        Assert.Equal(SystemPermissions.SecurePing, repository.LastPermissionCode);
    }

    private sealed class FakePermissionRepository : IPermissionRepository
    {
        private readonly bool _hasPermission;
        private readonly PermissionUserRecord? _user;

        public FakePermissionRepository(PermissionUserRecord? user, bool hasPermission)
        {
            _user = user;
            _hasPermission = hasPermission;
        }

        public string? LastPermissionCode { get; private set; }

        public Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user);
        }

        public Task<bool> UserHasPermissionAsync(
            long userId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            LastPermissionCode = permissionCode;

            return Task.FromResult(_hasPermission);
        }
    }
}
