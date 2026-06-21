namespace WeCms.Tests.Architecture;

public sealed class S8SecurityMigrationTests
{
    private static readonly string SystemSecurityRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Security");
    private static readonly string SystemLogsRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Logs");
    private static readonly string PersistenceSystemRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System");
    private static readonly string SecurityRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Security");
    private static readonly string SecuritySqlSugarRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Security.SqlSugar");

    [Fact]
    public async Task SystemModule_DoesNotContainSecurityOwnership()
    {
        var systemSecurityText = Directory.Exists(SystemSecurityRoot)
            ? await ReadAllSourceAsync(SystemSecurityRoot)
            : string.Empty;
        var systemLogsText = Directory.Exists(SystemLogsRoot)
            ? await ReadAllSourceAsync(SystemLogsRoot)
            : string.Empty;

        foreach (var forbidden in new[]
        {
            "SecurityBanService",
            "SecurityEndpoints",
            "SecurityAlertService",
            "RateLimitSecurityEventService",
            "SecurityEventEndpoints",
            "SecurityEventListQuery",
            "sys:security-event:"
        })
        {
            Assert.DoesNotContain(forbidden, systemSecurityText, StringComparison.Ordinal);
            Assert.DoesNotContain(forbidden, systemLogsText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Persistence_DoesNotContainSecurityRepositories()
    {
        var text = string.Concat(
            Directory.Exists(Path.Combine(PersistenceSystemRoot, "Logs")) ? await ReadAllSourceAsync(Path.Combine(PersistenceSystemRoot, "Logs")) : string.Empty,
            Directory.Exists(Path.Combine(PersistenceSystemRoot, "Security")) ? await ReadAllSourceAsync(Path.Combine(PersistenceSystemRoot, "Security")) : string.Empty);

        foreach (var forbidden in new[]
        {
            "SecurityBanRepository",
            "RateLimitSecurityEventRepository",
            "sys_security_ban",
            "sys_security_event"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SecurityModule_DoesNotContainDatabaseInfrastructure()
    {
        var text = await ReadAllSourceAsync(SecurityRoot);

        foreach (var forbidden in new[]
        {
            "SqlSugar",
            "MySqlConnector",
            LegacyBoundaryNames.Persistence,
            "WeCms.Modules.Security.SqlSugar",
            "SELECT ",
            "INSERT INTO",
            "UPDATE sys_",
            "DELETE FROM"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SecuritySqlSugar_ContainsOnlySecurityPersistence()
    {
        var text = await ReadAllSourceAsync(SecuritySqlSugarRoot);

        Assert.Contains("sys_security_event", text, StringComparison.Ordinal);
        Assert.Contains("sys_security_ban", text, StringComparison.Ordinal);
        Assert.DoesNotContain("sys_login_log", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecurityProjects_UseOnlyAllowedProjectReferences()
    {
        var securityRefs = await ReadProjectReferencesAsync(Path.Combine(SecurityRoot, "WeCms.Modules.Security.csproj"));
        var securitySqlSugarRefs = await ReadProjectReferencesAsync(Path.Combine(SecuritySqlSugarRoot, "WeCms.Modules.Security.SqlSugar.csproj"));

        Assert.Equal(new[] { "WeCms.EventBus", "WeCms.Shared" }, securityRefs);
        Assert.Equal(new[] { "WeCms.Data.SqlSugar", "WeCms.Modules.Security", "WeCms.Shared" }, securitySqlSugarRefs);
    }

    private static async Task<string> ReadAllSourceAsync(string root)
    {
        var files = Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(filePath => !filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(filePath => filePath, StringComparer.Ordinal);

        var chunks = new List<string>();
        foreach (var file in files)
        {
            chunks.Add(await File.ReadAllTextAsync(file));
        }

        return string.Join('\n', chunks);
    }

    private static async Task<string[]> ReadProjectReferencesAsync(string projectPath)
    {
        var xml = await File.ReadAllTextAsync(projectPath);
        return System.Text.RegularExpressions.Regex
            .Matches(xml, @"ProjectReference Include=""\.\.\\(?<name>[^\\]+)\\")
            .Select(match => match.Groups["name"].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
