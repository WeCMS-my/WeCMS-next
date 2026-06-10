using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Shared;

public sealed class PermissionMetadataTests
{
    [Fact]
    public void Constructor_ShouldSetCode()
    {
        var metadata = new PermissionMetadata("sys:test:action");
        Assert.Equal("sys:test:action", metadata.Code);
    }

    [Fact]
    public void Code_ShouldNotBeEmpty_WhenValid()
    {
        var metadata = new PermissionMetadata("sys:system:secure-ping");
        Assert.NotEmpty(metadata.Code);
    }

    [Fact]
    public void TwoInstances_WithSameCode_ShouldBeEqual()
    {
        var a = new PermissionMetadata("sys:test");
        var b = new PermissionMetadata("sys:test");
        Assert.Equal(a, b);
    }
}

public sealed class PermissionCheckResultTests
{
    [Fact]
    public void Active_WithPermission_ShouldSetBothTrue()
    {
        var result = new PermissionCheckResult(true, true);
        Assert.True(result.IsActive);
        Assert.True(result.HasPermission);
    }

    [Fact]
    public void Inactive_ShouldSetHasPermissionFalse()
    {
        var result = new PermissionCheckResult(false, false);
        Assert.False(result.IsActive);
        Assert.False(result.HasPermission);
    }

    [Fact]
    public void Active_WithoutPermission_ShouldSetCorrectly()
    {
        var result = new PermissionCheckResult(true, false);
        Assert.True(result.IsActive);
        Assert.False(result.HasPermission);
    }
}
