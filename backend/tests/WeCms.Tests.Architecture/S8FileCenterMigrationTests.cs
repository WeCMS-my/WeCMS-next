namespace WeCms.Tests.Architecture;

public sealed class S8FileCenterMigrationTests
{
    private static readonly string SystemFilesRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Files");
    private static readonly string PersistenceFilesRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "Files");
    private static readonly string FileCenterRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.FileCenter");
    private static readonly string FileCenterSqlSugarRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.FileCenter.SqlSugar");

    [Fact]
    public async Task SystemModule_DoesNotContainFileCenterOwnership()
    {
        var text = Directory.Exists(SystemFilesRoot) ? await ReadAllSourceAsync(SystemFilesRoot) : string.Empty;

        foreach (var forbidden in new[]
        {
            "FileService",
            "FileEndpoints",
            "FilePermissions",
            "IFileRepository",
            "FileUploadPolicy",
            "IFileObjectKeyGenerator",
            "CreateFileRequest"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Persistence_DoesNotContainFileRepositories()
    {
        var text = Directory.Exists(PersistenceFilesRoot) ? await ReadAllSourceAsync(PersistenceFilesRoot) : string.Empty;

        foreach (var forbidden in new[]
        {
            "FileRepository",
            "IFileRepository",
            "sys_file"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task FileCenterModule_DoesNotContainDatabaseInfrastructure()
    {
        var text = await ReadAllSourceAsync(FileCenterRoot);

        foreach (var forbidden in new[]
        {
            "SqlSugar",
            "MySqlConnector",
            LegacyBoundaryNames.Persistence,
            "WeCms.Modules.FileCenter.SqlSugar",
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
    public async Task FileCenterSqlSugar_ContainsOnlyFilePersistence()
    {
        var text = await ReadAllSourceAsync(FileCenterSqlSugarRoot);

        Assert.Contains("sys_file", text, StringComparison.Ordinal);
        Assert.DoesNotContain("sys_security_ban", text, StringComparison.Ordinal);
        Assert.DoesNotContain("sys_login_log", text, StringComparison.Ordinal);
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
}
