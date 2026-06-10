using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Shared;

public sealed class SystemPermissionsTests
{
    [Fact]
    public void SystemSecurePing_ShouldFollowNamingConvention()
    {
        Assert.Equal("sys:system:secure-ping", SystemPermissions.SystemSecurePing);
    }

    [Fact]
    public void AllPermissions_ShouldBeNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SystemPermissions.SystemSecurePing));
    }
}
