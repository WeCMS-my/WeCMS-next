namespace WeCms.Tests.Unit.Auth;

public sealed class PermissionVersionSourceTests
{
    [Fact]
    public async Task AuthDtos_ExposePermissionVersionInLoginAndMeResponses()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Contracts", "AuthDtos.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("long PermissionVersion", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record LoginResponse(", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record AuthMeResponse(", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PermissionVersionService_IsRegisteredAndUsedByPermissionChangingServices()
    {
        var program = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var accessControlSqlSugarDi = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl.SqlSugar", "AccessControlSqlSugarServiceCollectionExtensions.cs"), TestContext.Current.CancellationToken);
        var userService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Services", "UserService.cs"), TestContext.Current.CancellationToken);
        var roleService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl", "Roles", "RoleService.cs"), TestContext.Current.CancellationToken);
        var permissionService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl", "Permissions", "PermissionManagementService.cs"), TestContext.Current.CancellationToken);
        var menuService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl", "Menus", "MenuService.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("IAccessControlPermissionVersionService", program, StringComparison.Ordinal);
        Assert.Contains("IIdentityPermissionVersionService", program, StringComparison.Ordinal);
        Assert.Contains("IPermissionVersionRepository", accessControlSqlSugarDi, StringComparison.Ordinal);
        Assert.Contains("BumpUserAsync", userService, StringComparison.Ordinal);
        Assert.Contains("BumpUsersByRoleAsync", roleService, StringComparison.Ordinal);
        Assert.Contains("IAccessControlPermissionVersionService", permissionService, StringComparison.Ordinal);
        Assert.Contains("BumpUsersByPermissionAsync", permissionService, StringComparison.Ordinal);
        Assert.Contains("IAccessControlPermissionVersionService", menuService, StringComparison.Ordinal);
        Assert.Contains("BumpUsersByMenuAsync", menuService, StringComparison.Ordinal);
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
}
