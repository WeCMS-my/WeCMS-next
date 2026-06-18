namespace WeCms.Tests.Unit.Auth;

public sealed class PermissionVersionSourceTests
{
    [Fact]
    public async Task AuthDtos_ExposePermissionVersionInLoginAndMeResponses()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Auth", "AuthDtos.cs"));

        Assert.Contains("long PermissionVersion", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record LoginResponse(", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record AuthMeResponse(", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PermissionVersionService_IsRegisteredAndUsedByPermissionChangingServices()
    {
        var permissionsDi = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Permissions", "SystemPermissionsServiceCollectionExtensions.cs"));
        var persistenceDi = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Persistence", "Data", "PersistenceServiceCollectionExtensions.cs"));
        var userService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Users", "UserService.cs"));
        var roleService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Roles", "RoleService.cs"));
        var permissionService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Permissions", "PermissionManagementService.cs"));
        var menuService = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Menus", "MenuService.cs"));

        Assert.Contains("IPermissionVersionService", permissionsDi, StringComparison.Ordinal);
        Assert.Contains("IPermissionVersionRepository", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("BumpUserAsync", userService, StringComparison.Ordinal);
        Assert.Contains("BumpUsersByRoleAsync", roleService, StringComparison.Ordinal);
        Assert.Contains("BumpUsersByPermissionAsync", permissionService, StringComparison.Ordinal);
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
