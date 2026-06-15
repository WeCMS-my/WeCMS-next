using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class PermissionsTests
{
    [Fact]
    public void SystemUserList_ShouldUsePermissionCodeFormat_WhenReferenced()
    {
        Assert.Equal("sys:user:list", Permissions.SystemUserList);
    }
}
