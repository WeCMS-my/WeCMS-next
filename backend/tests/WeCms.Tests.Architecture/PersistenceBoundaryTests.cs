using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class PersistenceBoundaryTests
{
    private static readonly string RepoRoot = GetRepositoryRoot();
    private static readonly string SrcRoot = Path.Combine(RepoRoot, "backend", "src");
    private static readonly string[] Modules = [
        "WeCms.Modules.System",
        "WeCms.Modules.Cms"
    ];

    private static readonly Regex DapperApiPattern = new(
        @"\b(QueryAsync|ExecuteAsync|CommandDefinition)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DbPrimitiveTypePattern = new(
        @"\b(DbConnection|DbTransaction)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PersistenceTypeReferencePattern = new(
        @"\bWeCms\.Persistence\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SqlKeywordPattern = new(
        "[\"@\\$]*\"[^\"]*\\b(SELECT|INSERT|UPDATE|DELETE|FROM|JOIN|WHERE|INNER|LEFT|RIGHT|OUTER|FULL|CREATE|DROP|ALTER|TRUNCATE|VALUES|LIMIT|ORDER\\s+BY)\\b[^\"]*\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void Modules_ShouldNotReferenceDapperOrMySqlPackages()
    {
        var violators = new List<string>();

        foreach (var project in GetModuleProjectPaths())
        {
            var packageReferences = GetPackageReferences(project);
            if (packageReferences.Any(r => IsBannedDataPackage(r)))
            {
                var names = string.Join(", ", packageReferences.Where(IsBannedDataPackage));
                violators.Add($"{project}: {names}");
            }
        }

        Assert.Empty(violators);
    }

    [Fact]
    public void Modules_ShouldNotReferencePersistenceImplementation()
    {
        var violators = new List<string>();

        foreach (var project in GetModuleProjectPaths())
        {
            var projectReferences = GetProjectReferences(project);
            if (projectReferences.Any(r => r.Contains("WeCms.Persistence", StringComparison.Ordinal)))
            {
                violators.Add(project);
            }
        }

        Assert.Empty(violators);
    }

    [Fact]
    public void Modules_ShouldNotDirectlyReferencePersistenceTypes()
    {
        var matches = new List<string>();

        foreach (var source in GetModuleSourceFiles())
        {
            var lines = File.ReadAllLines(source);
            for (var i = 0; i < lines.Length; i++)
            {
                if (PersistenceTypeReferencePattern.IsMatch(lines[i]))
                {
                    matches.Add($"{source}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void Modules_ShouldNotCallDapperAsyncApisOrCommandDefinition()
    {
        var matches = new List<string>();

        foreach (var source in GetModuleSourceFiles())
        {
            var lines = File.ReadAllLines(source);
            for (var i = 0; i < lines.Length; i++)
            {
                if (DapperApiPattern.IsMatch(lines[i]) && !lines[i].Contains(".ExecuteAsync(context)", StringComparison.Ordinal))
                {
                    matches.Add($"{source}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void Modules_ShouldNotUseDbConnectionOrDbTransactionDirectly()
    {
        var matches = new List<string>();

        foreach (var source in GetModuleSourceFiles())
        {
            var lines = File.ReadAllLines(source);
            for (var i = 0; i < lines.Length; i++)
            {
                if (DbPrimitiveTypePattern.IsMatch(lines[i]))
                {
                    matches.Add($"{source}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void Modules_ShouldNotContainSqlKeywords()
    {
        var matches = new List<string>();

        foreach (var source in GetModuleSourceFiles())
        {
            var lines = File.ReadAllLines(source);
            for (var i = 0; i < lines.Length; i++)
            {
                if (SqlKeywordPattern.IsMatch(lines[i]))
                {
                    matches.Add($"{source}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void OnlyPersistence_ShouldReferenceDapperAndMySqlPackages()
    {
        var violators = new List<string>();
        foreach (var project in EnumerateProjectFiles("backend/src"))
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            if (projectName.Equals("WeCms.Persistence", StringComparison.Ordinal))
            {
                continue;
            }

            var packageReferences = GetPackageReferences(project);
            var banned = packageReferences
                .Where(IsBannedDataPackage)
                .ToArray();

            if (banned.Length > 0)
            {
                violators.Add($"{project}: {string.Join(", ", banned)}");
            }
        }

        Assert.Empty(violators);
    }

    private static bool IsBannedDataPackage(string packageReference) =>
        packageReference.Equals("Dapper", StringComparison.OrdinalIgnoreCase) ||
        packageReference.Equals("Dapper.AOT", StringComparison.OrdinalIgnoreCase) ||
        packageReference.Equals("MySqlConnector", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateProjectFiles(string relativePath)
    {
        return Directory.EnumerateFiles(Path.Combine(RepoRoot, relativePath), "*.csproj", SearchOption.AllDirectories);
    }

    private static IEnumerable<string> GetModuleProjectPaths()
    {
        foreach (var module in Modules)
        {
            yield return Path.Combine(SrcRoot, module, $"{module}.csproj");
        }
    }

    private static IEnumerable<string> GetModuleSourceFiles()
    {
        foreach (var module in Modules)
        {
            var moduleSourceDir = Path.Combine(SrcRoot, module);
            foreach (var source in Directory.EnumerateFiles(moduleSourceDir, "*.cs", SearchOption.AllDirectories))
            {
                yield return source;
            }
        }
    }

    private static IReadOnlyList<string> GetPackageReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc
            .Descendants("PackageReference")
            .Select(x => (string?)x.Attribute("Include"))
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
    }

    private static IReadOnlyList<string> GetProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include"))
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
    }

    private static string GetRepositoryRoot()
    {
        var directoryCandidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in directoryCandidates)
        {
            var current = new DirectoryInfo(candidate);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "backend", "WeCms.sln")) ||
                    File.Exists(Path.Combine(current.FullName, "backend", "WeCms.slnx")))
                {
                    return current.FullName;
                }

                if (current.Name.Equals("backend", StringComparison.OrdinalIgnoreCase) &&
                    (File.Exists(Path.Combine(current.FullName, "WeCms.sln")) ||
                     File.Exists(Path.Combine(current.FullName, "WeCms.slnx"))))
                {
                    return current.Parent?.FullName ?? current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing backend/WeCms.sln");
    }
}
