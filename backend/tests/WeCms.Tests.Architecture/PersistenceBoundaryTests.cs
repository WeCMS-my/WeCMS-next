using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed partial class PersistenceBoundaryTests
{
    private const string FinalDataPlatformFlag = "WECMS_ARCHITECTURE_FINAL_SQLSUGAR_PLATFORM";

    private static readonly string[] ForbiddenDatabaseTokens =
    [
        "SqlSugarCore",
        "SqlSugarClient",
        "SqlSugarScope",
        "ISqlSugarClient",
        "MySqlConnection",
        "MySqlConnector",
        "DbConnection",
        "DbTransaction"
    ];

    [Fact]
    public void OnlyPersistenceProject_CanReferenceSqlSugarOrMySql()
    {
        var violations = ProductionFiles()
            .Where(file => !IsAllowedDatabaseProject(file))
            .SelectMany(file => ForbiddenDatabaseTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{RelativePath(file)} contains {token}"))
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Modules_DoNotContainSqlText()
    {
        var violations = ProductionFiles()
            .Where(IsUnderBusinessModuleProject)
            .Where(file => SqlKeywordPattern().IsMatch(File.ReadAllText(file)))
            .Select(RelativePath)
            .ToArray();

        Assert.True(violations.Length == 0, $"Module files contain SQL keywords:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void UserRepository_LocksEnabledLockedRoleHoldersWhenCounting()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Users",
            "UserRepository.cs"));

        Assert.Contains("CountEnabledUsersByRoleForUpdateAsync", source, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UserRepository_CreateOperationChecksAffectedRows()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Users",
            "UserRepository.cs"));

        var createMethodStart = source.IndexOf("public async Task<long> CreateAsync", StringComparison.Ordinal);
        Assert.True(createMethodStart >= 0, "CreateAsync method not found.");
        var createMethodEnd = source.IndexOf("public async Task UpdateAsync", createMethodStart, StringComparison.Ordinal);
        Assert.True(createMethodEnd > createMethodStart, "CreateAsync method boundary not found.");
        var createMethodBody = source[createMethodStart..createMethodEnd];
        Assert.Contains("await ExpectOneAsync(", createMethodBody, StringComparison.Ordinal);

        Assert.DoesNotContain("permission_version = permission_version + 1", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionVersionRepository_OwnsPermissionVersionSql()
    {
        var permissionVersionRepository = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Permissions",
            "PermissionVersionRepository.cs"));
        var userRepository = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Users",
            "UserRepository.cs"));
        var roleRepository = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Roles",
            "RoleRepository.cs"));

        Assert.Contains("permission_version = permission_version + 1", permissionVersionRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("permission_version = permission_version + 1", userRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("permission_version = permission_version + 1", roleRepository, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionFiles()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static bool IsUnderProject(string file, string projectName)
    {
        var projectRoot = Path.Combine(TestPaths.SourceRoot, projectName);

        return file.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static bool IsAllowedDatabaseProject(string file)
    {
        if (IsFinalDataPlatformMode())
        {
            return IsUnderProject(file, "WeCms.Data.SqlSugar") || IsUnderModuleSqlSugarProject(file);
        }

        return IsUnderProject(file, "WeCms.Persistence")
            || IsUnderProject(file, "WeCms.Data.SqlSugar")
            || IsUnderModuleSqlSugarProject(file);
    }

    private static bool IsFinalDataPlatformMode()
    {
        return string.Equals(Environment.GetEnvironmentVariable(FinalDataPlatformFlag), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderBusinessModuleProject(string file)
    {
        var relative = Path.GetRelativePath(TestPaths.SourceRoot, file);
        var projectName = relative.Split(Path.DirectorySeparatorChar)[0];

        return projectName.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
            && !projectName.EndsWith(".SqlSugar", StringComparison.Ordinal);
    }

    private static bool IsUnderModuleSqlSugarProject(string file)
    {
        var relative = Path.GetRelativePath(TestPaths.SourceRoot, file);
        var projectName = relative.Split(Path.DirectorySeparatorChar)[0];

        return projectName.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
            && projectName.EndsWith(".SqlSugar", StringComparison.Ordinal);
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(TestPaths.RepoRoot, file);
    }

    [GeneratedRegex("""\b(SELECT\s+.+\s+FROM|INSERT\s+INTO|UPDATE\s+[A-Za-z_][A-Za-z0-9_]*|DELETE\s+FROM)\b""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlKeywordPattern();
}
