using WeCms.Modules.AccessControl.Permissions;

namespace WeCms.Tests.Unit.AccessControl;

public sealed class PermissionDefinitionRegistryTests
{
    [Fact]
    public void FromProviders_ReturnsDefinitionsWhenInputIsValid()
    {
        var registry = PermissionRegistry.FromProviders(
        [
            new StaticPermissionDefinitionProvider(
            [
                new PermissionGroupDefinition(
                    "system",
                    [
                        new PermissionDefinition(
                            "sys:role:list",
                            "system",
                            PermissionKind.Url,
                            PermissionAction.List,
                            "Role list")
                    ])
            ])
        ]);

        var definition = registry.GetRequired("sys:role:list");

        Assert.Equal("sys:role:list", definition.Code);
        Assert.Equal("system", definition.Module);
        Assert.Equal(PermissionKind.Url, definition.Kind);
        Assert.Equal(PermissionAction.List, definition.Action);
    }

    [Fact]
    public void FromProviders_RejectsDuplicatePermissionCodes()
    {
        var providers = new PermissionDefinitionProvider[]
        {
            new StaticPermissionDefinitionProvider(
            [
                new PermissionGroupDefinition(
                    "system",
                    [
                        new PermissionDefinition(
                            "sys:role:list",
                            "system",
                            PermissionKind.Url,
                            PermissionAction.List,
                            "Role list")
                    ]),
                new PermissionGroupDefinition(
                    "identity",
                    [
                        new PermissionDefinition(
                            "sys:role:list",
                            "identity",
                            PermissionKind.Url,
                            PermissionAction.List,
                            "Duplicate role list")
                    ])
            ])
        };

        var exception = Assert.Throws<InvalidOperationException>(() => PermissionRegistry.FromProviders(providers));

        Assert.Contains("Duplicate permission code: sys:role:list", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sys")]
    [InlineData("sys:role")]
    [InlineData("Sys:role:list")]
    [InlineData("sys:role:list:extra")]
    [InlineData("sys:role:list!")]
    public void PermissionDefinition_RejectsInvalidCodeFormat(string code)
    {
        var exception = Assert.Throws<ArgumentException>(() => new PermissionDefinition(
            code,
            "system",
            PermissionKind.Url,
            PermissionAction.List,
            "Role list"));

        Assert.Contains("Permission code must use domain:resource:action format.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("System")]
    [InlineData("system.permission")]
    [InlineData("system_permission")]
    public void PermissionDefinition_RejectsInvalidModule(string module)
    {
        var exception = Assert.Throws<ArgumentException>(() => new PermissionDefinition(
            "sys:role:list",
            module,
            PermissionKind.Url,
            PermissionAction.List,
            "Role list"));

        Assert.Contains("Permission module is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionDefinition_RejectsUndefinedKind()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PermissionDefinition(
            "sys:role:list",
            "system",
            (PermissionKind)99,
            PermissionAction.List,
            "Role list"));

        Assert.Equal("kind", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("List")]
    [InlineData("assign_permission")]
    [InlineData("assign-permission!")]
    public void PermissionAction_From_RejectsInvalidAction(string action)
    {
        var exception = Assert.Throws<ArgumentException>(() => PermissionAction.From(action));

        Assert.Contains("Permission action is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionDefinition_RejectsActionThatDoesNotMatchCode()
    {
        var exception = Assert.Throws<ArgumentException>(() => new PermissionDefinition(
            "sys:role:list",
            "system",
            PermissionKind.Url,
            PermissionAction.Create,
            "Role list"));

        Assert.Contains("Permission action must match the permission code action segment.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionGroupDefinition_RejectsDefinitionsFromDifferentModule()
    {
        var exception = Assert.Throws<ArgumentException>(() => new PermissionGroupDefinition(
            "system",
            [
                new PermissionDefinition(
                    "sys:user:list",
                    "identity",
                    PermissionKind.Url,
                    PermissionAction.List,
                    "User list")
            ]));

        Assert.Contains("Permission definition module must match the group module.", exception.Message, StringComparison.Ordinal);
    }

    private sealed class StaticPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

        public StaticPermissionDefinitionProvider(IReadOnlyList<PermissionGroupDefinition> groups)
        {
            _groups = groups;
        }

        public override IReadOnlyList<PermissionGroupDefinition> GetGroups()
        {
            return _groups;
        }
    }
}
