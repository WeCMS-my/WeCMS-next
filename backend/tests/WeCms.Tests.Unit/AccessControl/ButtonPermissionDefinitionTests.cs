using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Permissions;

namespace WeCms.Tests.Unit.AccessControl;

public sealed class ButtonPermissionDefinitionTests
{
    [Fact]
    public void ButtonPermissionDefinition_AcceptsValidButtonPermission()
    {
        var definition = new ButtonPermissionDefinition(
            "create",
            "sys.users",
            "identity:user:button:create",
            "Create user");

        Assert.Equal("create", definition.ButtonKey);
        Assert.Equal("sys.users", definition.MenuCode);
        Assert.Equal("identity:user:button:create", definition.PermissionCode);
        Assert.Equal("identity", definition.Module);
        Assert.Equal("user", definition.Resource);
        Assert.Equal("Create user", definition.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Create")]
    [InlineData("assign_permission")]
    [InlineData("assign-permission!")]
    public void ButtonPermissionDefinition_RejectsInvalidButtonKey(string buttonKey)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ButtonPermissionDefinition(
            buttonKey,
            "sys.users",
            "identity:user:button:create",
            "Create user"));

        Assert.Contains("Button key is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sys")]
    [InlineData("Sys.Users")]
    [InlineData("sys_users")]
    public void ButtonPermissionDefinition_RejectsInvalidMenuCode(string menuCode)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ButtonPermissionDefinition(
            "create",
            menuCode,
            "identity:user:button:create",
            "Create user"));

        Assert.Contains("Menu code is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("identity:user:create")]
    [InlineData("identity:user:button")]
    [InlineData("identity:user:button:create:extra")]
    [InlineData("Identity:user:button:create")]
    public void ButtonPermissionDefinition_RejectsInvalidPermissionCode(string permissionCode)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ButtonPermissionDefinition(
            "create",
            "sys.users",
            permissionCode,
            "Create user"));

        Assert.Contains("Button permission code must use domain:resource:button:action format.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonPermissionDefinition_RejectsPermissionActionThatDoesNotMatchButtonKey()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ButtonPermissionDefinition(
            "create",
            "sys.users",
            "identity:user:button:disable",
            "Create user"));

        Assert.Contains("Button permission action must match the button key.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessProfileDto_ReturnsButtonPermissions()
    {
        var profile = new AccessProfileDto(
            PermissionVersion: 7,
            Roles: ["admin"],
            Permissions: ["identity:user:list"],
            Buttons: ["identity:user:button:create"],
            Menus:
            [
                new MenuTreeDto(
                    1,
                    null,
                    "menu",
                    "sys.users",
                    "/system/users",
                    "system/users/index",
                    "Users",
                    "route.system.users",
                    null,
                    100,
                    false,
                    false,
                    null,
                    "sys:user:page",
                    "enabled",
                    true,
                    [])
            ]);

        Assert.Equal(7, profile.PermissionVersion);
        Assert.Equal(["admin"], profile.Roles);
        Assert.Equal(["identity:user:list"], profile.Permissions);
        Assert.Equal(["identity:user:button:create"], profile.Buttons);
        Assert.Equal("sys.users", Assert.Single(profile.Menus).Code);
    }

    [Fact]
    public void AccessProfileDto_IsCoveredByJsonSerializerContext()
    {
        var profile = new AccessProfileDto(
            PermissionVersion: 7,
            Roles: ["admin"],
            Permissions: ["identity:user:list"],
            Buttons: ["identity:user:button:create"],
            Menus: []);

        var json = JsonSerializer.Serialize(profile, WeCmsJsonSerializerContext.Default.AccessProfileDto);

        Assert.Contains("\"buttons\":[\"identity:user:button:create\"]", json, StringComparison.Ordinal);
    }
}
