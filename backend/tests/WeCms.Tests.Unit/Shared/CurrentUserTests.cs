using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Shared;

public sealed class CurrentUserTests
{
    [Fact]
    public void Anonymous_ShouldHaveZeroId()
    {
        Assert.Equal(0L, CurrentUser.Anonymous.Id);
        Assert.True(string.IsNullOrEmpty(CurrentUser.Anonymous.Username));
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var user = new CurrentUser(1L, "admin", "超级管理员", 5, "stamp-v1");

        Assert.Equal(1L, user.Id);
        Assert.Equal("admin", user.Username);
        Assert.Equal("超级管理员", user.DisplayName);
        Assert.Equal(5, user.PermissionVersion);
        Assert.Equal("stamp-v1", user.SecurityStamp);
    }

    [Fact]
    public void IsAuthenticated_ShouldBeFalse_WhenAnonymous()
    {
        Assert.False(CurrentUser.Anonymous.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ShouldBeTrue_WhenHasId()
    {
        var user = new CurrentUser(1L, "admin", "Admin", 1, "stamp");
        Assert.True(user.IsAuthenticated);
    }
}
