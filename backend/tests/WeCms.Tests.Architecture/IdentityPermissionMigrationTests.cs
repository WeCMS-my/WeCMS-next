namespace WeCms.Tests.Architecture;

public sealed class IdentityPermissionMigrationTests
{
    private static readonly string IdentityRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    private static readonly string[] IdentityUserPermissionCodes =
    [
        "sys:user:page",
        "sys:user:list",
        "sys:user:detail",
        "sys:user:create",
        "sys:user:update",
        "sys:user:delete",
        "sys:user:enable",
        "sys:user:disable",
        "sys:user:reset-password",
        "sys:user:reset-2fa",
        "sys:user:assign-role",
        "sys:user:assign-position"
    ];

    [Fact]
    public async Task IdentityUserPermissions_DefinesEveryUserPermissionCode()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(IdentityRoot, "Permissions", "IdentityUserPermissions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("public const string Page = \"sys:user:page\";", source, StringComparison.Ordinal);
        foreach (var code in IdentityUserPermissionCodes)
        {
            Assert.Contains(code, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemUserPermissionDefinition_IsRemoved()
    {
        Assert.False(File.Exists(Path.Combine(SystemRoot, "Users", "UserPermissions.cs")));
    }

    [Fact]
    public async Task OpenApiUserEndpointMetadata_UsesIdentityPermissionDefinitions()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Extensions", "OpenApiExtensions.Endpoints.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("using WeCms.Modules.Identity.Permissions;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Users") + ";", source, StringComparison.Ordinal);
        Assert.Contains("IdentityUserPermissions.List", source, StringComparison.Ordinal);
        Assert.Contains("IdentityUserPermissions.AssignPosition", source, StringComparison.Ordinal);
        Assert.DoesNotContain(" UserPermissions.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("(UserPermissions.", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedFiles_AssignUserPermissionsToIdentityModule()
    {
        var systemSeed = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.RepoRoot, "database", "seeds", "000002_seed_system_permissions.sql"),
            TestContext.Current.CancellationToken);
        var identitySeed = systemSeed;

        Assert.Contains("CASE WHEN v.code LIKE 'sys:user:%' THEN 'identity' ELSE 'system' END", systemSeed, StringComparison.Ordinal);
        Assert.Contains("UPDATE sys_permission", identitySeed, StringComparison.Ordinal);
        Assert.Contains("SET module = 'identity'", identitySeed, StringComparison.Ordinal);
        Assert.Contains("WHERE code LIKE 'sys:user:%'", identitySeed, StringComparison.Ordinal);
    }
}
