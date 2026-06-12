using Microsoft.Extensions.Configuration;

namespace WeCms.Persistence.Migration;

public sealed class FileDbMigrationScriptProvider : IDbMigrationScriptProvider
{
    private readonly string _databaseRoot;

    public FileDbMigrationScriptProvider(IConfiguration configuration)
    {
        var configuredRoot = configuration["Database:ScriptsRoot"];
        _databaseRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? ResolveDefaultDatabaseRoot()
            : Path.GetFullPath(configuredRoot, Directory.GetCurrentDirectory());
    }

    public IReadOnlyList<DbMigrationScript> GetSchemaMigrations()
    {
        return LoadScripts(Path.Combine(_databaseRoot, "migrations"));
    }

    public IReadOnlyList<DbMigrationScript> GetSeeds()
    {
        return LoadScripts(Path.Combine(_databaseRoot, "seeds"));
    }

    private static IReadOnlyList<DbMigrationScript> LoadScripts(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Database script directory not found: {directory}");
        }

        var scripts = Directory
            .EnumerateFiles(directory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(ReadScript)
            .ToArray();

        var duplicateVersion = scripts
            .GroupBy(static script => script.Version, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateVersion is not null)
        {
            throw new InvalidOperationException($"Duplicate database script version: {duplicateVersion.Key}");
        }

        return scripts;
    }

    private static DbMigrationScript ReadScript(string path)
    {
        var fileName = Path.GetFileName(path);
        var separatorIndex = fileName.IndexOf('_', StringComparison.Ordinal);
        if (separatorIndex <= 0 || !fileName.EndsWith(".sql", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid database script file name: {fileName}");
        }

        var version = fileName[..separatorIndex];
        var name = fileName[(separatorIndex + 1)..^4];
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Invalid database script name: {fileName}");
        }

        return DbMigrationScript.Create(version, name, File.ReadAllText(path));
    }

    private static string ResolveDefaultDatabaseRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "database");
                if (Directory.Exists(Path.Combine(candidate, "migrations")) &&
                    Directory.Exists(Path.Combine(candidate, "seeds")))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate database script root. Set Database:ScriptsRoot or deploy the database directory with the application.");
    }
}
