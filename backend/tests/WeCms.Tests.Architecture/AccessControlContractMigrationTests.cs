namespace WeCms.Tests.Architecture;

public sealed class AccessControlContractMigrationTests
{
    private static readonly string AccessControlRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.AccessControl");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    private static readonly string AccessControlSqlSugarRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.AccessControl.SqlSugar");

    private static readonly string PersistenceRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.Persistence);

    private static readonly string[] AccessControlContractFiles =
    [
        Path.Combine("Contracts", "RoleDtos.cs"),
        Path.Combine("Contracts", "PermissionManagementDtos.cs"),
        Path.Combine("Contracts", "MenuDtos.cs"),
        Path.Combine("Records", "RoleRecords.cs"),
        Path.Combine("Records", "PermissionManagementRecords.cs"),
        Path.Combine("Records", "PermissionRecords.cs"),
        Path.Combine("Records", "MenuRecords.cs")
    ];

    private static readonly string[] LegacySystemContractFiles =
    [
        Path.Combine("Roles", "RoleDtos.cs"),
        Path.Combine("Roles", "RoleRecords.cs"),
        Path.Combine("Permissions", "PermissionManagementDtos.cs"),
        Path.Combine("Permissions", "PermissionManagementRecords.cs"),
        Path.Combine("Permissions", "PermissionRecords.cs"),
        Path.Combine("Menus", "MenuDtos.cs"),
        Path.Combine("Menus", "MenuRecords.cs")
    ];

    [Fact]
    public async Task RolePermissionMenuContractsAndRecords_LiveInAccessControl()
    {
        foreach (var relativePath in AccessControlContractFiles)
        {
            var path = Path.Combine(AccessControlRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing AccessControl contract or record file: {relativePath}");

            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            var expectedNamespace = relativePath.StartsWith("Contracts", StringComparison.Ordinal)
                ? "namespace WeCms.Modules.AccessControl.Contracts;"
                : "namespace WeCms.Modules.AccessControl.Records;";
            Assert.Contains(expectedNamespace, source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemContractAndRecordFiles_AreRemoved()
    {
        var remaining = LegacySystemContractFiles
            .Select(relativePath => Path.Combine(SystemRoot, relativePath))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(SystemRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy System AccessControl contract files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task JsonSerializerContext_UsesAccessControlContractNamespaces()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Json", "WeCmsJsonSerializerContext.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("using WeCms.Modules.AccessControl.Contracts;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Roles") + ";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Permissions") + ";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Menus") + ";", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RoleServiceAndRepository_LiveInAccessControlBoundaries()
    {
        var accessControlFiles = new[]
        {
            Path.Combine(AccessControlRoot, "Roles", "IRoleService.cs"),
            Path.Combine(AccessControlRoot, "Roles", "RoleService.cs"),
            Path.Combine(AccessControlRoot, "Repositories", "IRoleRepository.cs"),
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "RoleRepository.cs")
        };

        foreach (var path in accessControlFiles)
        {
            Assert.True(File.Exists(path), $"Missing migrated Role file: {Path.GetFileName(path)}");
        }

        var roleService = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "Roles", "RoleService.cs"),
            TestContext.Current.CancellationToken);
        var roleRepository = await File.ReadAllTextAsync(
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "RoleRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("namespace WeCms.Modules.AccessControl.Roles;", roleService, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;", roleRepository, StringComparison.Ordinal);
        Assert.Contains("IAccessControlPermissionVersionService", roleService, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlSugar", roleService, StringComparison.Ordinal);
        Assert.DoesNotContain("MySqlConnector", roleService, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacySystemRoleServiceAndPersistenceRepositoryFiles_AreRemoved()
    {
        var legacyFiles = new[]
        {
            Path.Combine(SystemRoot, "Roles", "IRoleService.cs"),
            Path.Combine(SystemRoot, "Roles", "RoleService.cs"),
            Path.Combine(SystemRoot, "Roles", "IRoleRepository.cs"),
            Path.Combine(PersistenceRoot, "Modules", "System", "Roles", "RoleRepository.cs")
        };
        var remaining = legacyFiles
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(TestPaths.SourceRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy Role service/repository files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task PermissionServicesCheckerFilterAndRepositories_LiveInAccessControlBoundaries()
    {
        var accessControlFiles = new[]
        {
            Path.Combine(AccessControlRoot, "Permissions", "IPermissionChecker.cs"),
            Path.Combine(AccessControlRoot, "Permissions", "PermissionChecker.cs"),
            Path.Combine(AccessControlRoot, "Permissions", "PermissionEndpointFilter.cs"),
            Path.Combine(AccessControlRoot, "Permissions", "IPermissionManagementService.cs"),
            Path.Combine(AccessControlRoot, "Permissions", "PermissionManagementService.cs"),
            Path.Combine(AccessControlRoot, "Repositories", "IPermissionRepository.cs"),
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "PermissionRepository.cs"),
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "PermissionSecurityEventRepository.cs")
        };

        foreach (var path in accessControlFiles)
        {
            Assert.True(File.Exists(path), $"Missing migrated Permission file: {Path.GetFileName(path)}");
        }

        var permissionService = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "Permissions", "PermissionManagementService.cs"),
            TestContext.Current.CancellationToken);
        var permissionFilter = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "Permissions", "PermissionEndpointFilter.cs"),
            TestContext.Current.CancellationToken);
        var permissionRepository = await File.ReadAllTextAsync(
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "PermissionRepository.cs"),
            TestContext.Current.CancellationToken);
        var securityEventRepository = await File.ReadAllTextAsync(
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "PermissionSecurityEventRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("namespace WeCms.Modules.AccessControl.Permissions;", permissionService, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.Permissions;", permissionFilter, StringComparison.Ordinal);
        Assert.Contains("IAccessControlPermissionVersionService", permissionService, StringComparison.Ordinal);
        Assert.Contains("permission_denied", permissionFilter, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;", permissionRepository, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;", securityEventRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlSugar", permissionService, StringComparison.Ordinal);
        Assert.DoesNotContain("MySqlConnector", permissionService, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemNamespace("Permissions") + ";", permissionService, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndpointPermissionExtensions_BindToAccessControlPermissionFilter()
    {
        var apiExtension = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Endpoints", "EndpointPermissionExtensions.cs"),
            TestContext.Current.CancellationToken);
        var sharedExtension = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Shared", "Endpoints", "EndpointPermissionExtensions.cs"),
            TestContext.Current.CancellationToken);
        var registration = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "AccessControlServiceCollectionExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("using WeCms.Modules.AccessControl.Permissions;", apiExtension, StringComparison.Ordinal);
        Assert.Contains(".AddEndpointFilter<PermissionEndpointFilter>()", apiExtension, StringComparison.Ordinal);
        Assert.DoesNotContain("EndpointPermissionRuntimeExtensions", apiExtension, StringComparison.Ordinal);
        Assert.DoesNotContain("EndpointPermissionFilter", apiExtension, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IEndpointPermissionFilter>()", sharedExtension, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IEndpointPermissionFilter, PermissionEndpointFilter>()", registration, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacySystemPermissionServiceAndPersistenceRepositoryFiles_AreRemoved()
    {
        var legacyFiles = new[]
        {
            Path.Combine(SystemRoot, "Permissions", "IPermissionChecker.cs"),
            Path.Combine(SystemRoot, "Permissions", "PermissionChecker.cs"),
            Path.Combine(SystemRoot, "Permissions", "PermissionEndpointFilter.cs"),
            Path.Combine(SystemRoot, "Permissions", "PermissionMetadata.cs"),
            Path.Combine(SystemRoot, "Permissions", "EndpointPermissionDeniedRecorder.cs"),
            Path.Combine(SystemRoot, "Permissions", "IPermissionManagementService.cs"),
            Path.Combine(SystemRoot, "Permissions", "PermissionManagementService.cs"),
            Path.Combine(SystemRoot, "Permissions", "IPermissionRepository.cs"),
            Path.Combine(PersistenceRoot, "Modules", "System", "Permissions", "PermissionRepository.cs"),
            Path.Combine(PersistenceRoot, "Modules", "System", "Permissions", "PermissionSecurityEventRepository.cs")
        };
        var remaining = legacyFiles
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(TestPaths.SourceRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy Permission service/repository files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task MenuServiceTreeBuilderAndRepository_LiveInAccessControlBoundaries()
    {
        var accessControlFiles = new[]
        {
            Path.Combine(AccessControlRoot, "Menus", "IMenuService.cs"),
            Path.Combine(AccessControlRoot, "Menus", "MenuService.cs"),
            Path.Combine(AccessControlRoot, "Menus", "MenuTreeBuilder.cs"),
            Path.Combine(AccessControlRoot, "Repositories", "IMenuRepository.cs"),
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "MenuRepository.cs")
        };

        foreach (var path in accessControlFiles)
        {
            Assert.True(File.Exists(path), $"Missing migrated Menu file: {Path.GetFileName(path)}");
        }

        var menuService = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "Menus", "MenuService.cs"),
            TestContext.Current.CancellationToken);
        var menuTreeBuilder = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "Menus", "MenuTreeBuilder.cs"),
            TestContext.Current.CancellationToken);
        var menuRepository = await File.ReadAllTextAsync(
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "MenuRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("namespace WeCms.Modules.AccessControl.Menus;", menuService, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.Menus;", menuTreeBuilder, StringComparison.Ordinal);
        Assert.Contains("IAccessControlPermissionVersionService", menuService, StringComparison.Ordinal);
        Assert.Contains("MenuTreeBuilder.Build", menuService, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;", menuRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlSugar", menuService, StringComparison.Ordinal);
        Assert.DoesNotContain("MySqlConnector", menuService, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemNamespace("Menus") + ";", menuService, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacySystemMenuServiceAndPersistenceRepositoryFiles_AreRemoved()
    {
        var legacyFiles = new[]
        {
            Path.Combine(SystemRoot, "Menus", "IMenuService.cs"),
            Path.Combine(SystemRoot, "Menus", "MenuService.cs"),
            Path.Combine(SystemRoot, "Menus", "MenuTreeBuilder.cs"),
            Path.Combine(SystemRoot, "Menus", "IMenuRepository.cs"),
            Path.Combine(PersistenceRoot, "Modules", "System", "Menus", "MenuRepository.cs")
        };
        var remaining = legacyFiles
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(TestPaths.SourceRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy Menu service/repository files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task AccessProfileServiceAndRepository_LiveInAccessControlBoundaries()
    {
        var accessControlFiles = new[]
        {
            Path.Combine(AccessControlRoot, "AccessProfiles", "IAccessProfileService.cs"),
            Path.Combine(AccessControlRoot, "AccessProfiles", "AccessProfileService.cs"),
            Path.Combine(AccessControlRoot, "Repositories", "IAccessProfileRepository.cs"),
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "AccessProfileRepository.cs")
        };

        foreach (var path in accessControlFiles)
        {
            Assert.True(File.Exists(path), $"Missing migrated AccessProfile file: {Path.GetFileName(path)}");
        }

        var service = await File.ReadAllTextAsync(
            Path.Combine(AccessControlRoot, "AccessProfiles", "AccessProfileService.cs"),
            TestContext.Current.CancellationToken);
        var repository = await File.ReadAllTextAsync(
            Path.Combine(AccessControlSqlSugarRoot, "Repositories", "AccessProfileRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("namespace WeCms.Modules.AccessControl.AccessProfiles;", service, StringComparison.Ordinal);
        Assert.Contains("namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;", repository, StringComparison.Ordinal);
        Assert.Contains("AccessProfileDto", service, StringComparison.Ordinal);
        Assert.Contains("MenuTreeBuilder.Build", service, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlSugar", service, StringComparison.Ordinal);
        Assert.DoesNotContain("MySqlConnector", service, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityConsumesAccessProfileServiceWithoutIdentityAccessProfileReader()
    {
        var identityFiles = new[]
        {
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity", "Services", "AuthService.cs"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity", "Services", "AuthSessionIssuer.cs"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity", "Services", "RefreshTokenRotationService.cs")
        };

        foreach (var path in identityFiles)
        {
            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("IAccessProfileService", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IIdentityAccessProfileReader", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IdentityAccessProfile", source, StringComparison.Ordinal);
        }

        var identitySqlSugarDi = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar", "IdentitySqlSugarServiceCollectionExtensions.cs"),
            TestContext.Current.CancellationToken);
        var authRepository = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar", "Repositories", "AuthRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("IIdentityAccessProfileReader", identitySqlSugarDi, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdentityAccessProfileReader", authRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("ListVisibleMenusAsync", authRepository, StringComparison.Ordinal);
    }
}
