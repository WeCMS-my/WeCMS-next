namespace WeCms.Tests.Architecture;

public sealed class S6OrganizationMigrationTests
{
    private static readonly string[] SearchRoots =
    [
        Path.Combine(TestPaths.RepoRoot, "backend", "src"),
        Path.Combine(TestPaths.RepoRoot, "backend", "tests"),
        Path.Combine(TestPaths.RepoRoot, "database"),
        Path.Combine(TestPaths.RepoRoot, "scripts", "checks")
    ];

    [Fact]
    public async Task SystemPositionNaming_DoesNotUseLegacyPostTokens()
    {
        var forbiddenTokens = new[]
        {
            string.Concat("sys_", "post"),
            string.Concat("sys_user_", "post"),
            string.Concat("User", "Post"),
            string.Concat("Post", "Service"),
            string.Concat("I", "Post", "Repository"),
            string.Concat("Post", "Repository"),
            string.Concat("Post", "Permissions"),
            string.Concat("Create", "Post", "Request"),
            string.Concat("Update", "Post", "Request"),
            string.Concat("/api/v1/system/", "posts"),
            string.Concat("/system/", "posts"),
            string.Concat("sys:", "post", ":"),
            string.Concat("sys:user:assign-", "post"),
            string.Concat("assign-", "post")
        };

        var violations = new List<string>();
        foreach (var file in SourceFiles())
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task DepartmentMigration_DoesNotUseOldSystemOrPersistenceBoundary()
    {
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.SystemNamespace("Departments"),
            LegacyBoundaryNames.PersistenceSystemNamespace("Departments"),
            "AddWeCmsSystemDepartments",
            "SystemDepartmentsServiceCollectionExtensions",
            LegacyBoundaryNames.SystemSourcePath("Departments"),
            LegacyBoundaryNames.PersistenceSystemSourcePath("Departments")
        };

        var oldSystemDepartmentFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Departments");
        var oldPersistenceDepartmentFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "Departments");
        var violations = new List<string>();
        if (Directory.Exists(oldSystemDepartmentFiles) && Directory.EnumerateFiles(oldSystemDepartmentFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldSystemDepartmentFiles)} still contains Department source files");
        }

        if (Directory.Exists(oldPersistenceDepartmentFiles) && Directory.EnumerateFiles(oldPersistenceDepartmentFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldPersistenceDepartmentFiles)} still contains Department repository files");
        }

        foreach (var file in SourceFiles())
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task PositionMigration_DoesNotUseOldSystemOrPersistenceBoundary()
    {
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.SystemNamespace("Positions"),
            LegacyBoundaryNames.PersistenceSystemNamespace("Positions"),
            "AddWeCmsSystemPositions",
            "SystemPositionsServiceCollectionExtensions",
            LegacyBoundaryNames.SystemSourcePath("Positions"),
            LegacyBoundaryNames.PersistenceSystemSourcePath("Positions")
        };

        var oldSystemPositionFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Positions");
        var oldPersistencePositionFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "Positions");
        var violations = new List<string>();
        if (Directory.Exists(oldSystemPositionFiles) && Directory.EnumerateFiles(oldSystemPositionFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldSystemPositionFiles)} still contains Position source files");
        }

        if (Directory.Exists(oldPersistencePositionFiles) && Directory.EnumerateFiles(oldPersistencePositionFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldPersistencePositionFiles)} still contains Position repository files");
        }

        foreach (var file in SourceFiles())
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task IdentityUserService_UsesOrganizationLookupBoundary()
    {
        var userService = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity", "Services", "UserService.cs");
        var userRepositoryInterface = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity", "Repositories", "IUserRepository.cs");
        var userRepository = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar", "Repositories", "UserRepository.cs");
        var userRepositoryHelpers = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar", "Repositories", "UserRepository.Helpers.cs");

        var userServiceSource = await File.ReadAllTextAsync(userService, TestContext.Current.CancellationToken);
        Assert.Contains("IOrganizationLookupService", userServiceSource, StringComparison.Ordinal);
        Assert.Contains("_organizationLookupService.DepartmentExistsAsync", userServiceSource, StringComparison.Ordinal);
        Assert.Contains("_organizationLookupService.ExistingPositionIdsAsync", userServiceSource, StringComparison.Ordinal);

        var forbiddenTokens = new[]
        {
            "DeptExistsAsync",
            "ExistingPositionIdsAsync",
            "WeCms.Modules.Organization.SqlSugar",
            "IDepartmentRepository",
            "IPositionRepository",
            "DepartmentRepository",
            "PositionRepository",
            "FROM sys_dept",
            "FROM sys_position",
            "UPDATE sys_dept",
            "UPDATE sys_position",
            "INSERT INTO sys_dept",
            "INSERT INTO sys_position"
        };

        var violations = new List<string>();
        foreach (var file in new[] { userRepositoryInterface, userRepository, userRepositoryHelpers })
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> SourceFiles()
    {
        return SearchRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(path => !path.EndsWith(nameof(S6OrganizationMigrationTests) + ".cs", StringComparison.Ordinal))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".sql", StringComparison.Ordinal)
                || path.EndsWith(".sh", StringComparison.Ordinal));
    }
}
