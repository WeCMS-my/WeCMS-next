namespace WeCms.Tests.Architecture;

public sealed class S8AuditMigrationTests
{
    [Fact]
    public async Task AuditMigration_DoesNotUseOldSystemOrPersistenceLogOwnership()
    {
        var oldSystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule, "Logs");
        var oldPersistenceRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.Persistence, "Modules", "System", "Logs");
        var forbiddenTokens = new[]
        {
            "LoginLog",
            "AuditLog",
            "sys_login_log",
            "sys_audit_log",
            "MapLoginLogEndpoints",
            "MapAuditLogEndpoints"
        };

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(oldSystemRoot, "*.cs", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(oldPersistenceRoot, "*.cs", SearchOption.AllDirectories)))
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
    public async Task AuditModule_DoesNotContainSqlOrPersistenceReferences()
    {
        var moduleRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Audit");
        var forbiddenTokens = new[]
        {
            "SqlSugar",
            "MySqlConnector",
            LegacyBoundaryNames.Persistence,
            "WeCms.Modules.Audit.SqlSugar",
            "SELECT ",
            "INSERT INTO",
            "UPDATE sys_",
            "DELETE FROM"
        };

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(moduleRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task AuditSqlSugar_DoesNotContainSecurityEventPersistence()
    {
        var moduleRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Audit.SqlSugar");
        var forbiddenTokens = new[]
        {
            "SecurityEvent",
            "sys_security_event"
        };

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(moduleRoot, "*.cs", SearchOption.AllDirectories))
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
    public async Task AuditProjects_UseOnlyAllowedProjectReferences()
    {
        var auditRefs = await ProjectReferencesAsync("WeCms.Modules.Audit");
        var auditSqlSugarRefs = await ProjectReferencesAsync("WeCms.Modules.Audit.SqlSugar");

        Assert.Equal(["WeCms.Shared"], auditRefs);
        Assert.Equal(["WeCms.Data.SqlSugar", "WeCms.Modules.Audit", "WeCms.Shared"], auditSqlSugarRefs);
    }

    private static async Task<string[]> ProjectReferencesAsync(string projectName)
    {
        var projectPath = Path.Combine(TestPaths.SourceRoot, projectName, projectName + ".csproj");
        var document = System.Xml.Linq.XDocument.Parse(await File.ReadAllTextAsync(projectPath, TestContext.Current.CancellationToken));
        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => include is not null)
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', Path.DirectorySeparatorChar)))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
