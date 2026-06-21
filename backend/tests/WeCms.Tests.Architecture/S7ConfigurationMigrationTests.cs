namespace WeCms.Tests.Architecture;

public sealed class S7ConfigurationMigrationTests
{
    [Fact]
    public async Task SettingsMigration_DoesNotUseOldSystemOrPersistenceBoundary()
    {
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.SystemNamespace("Settings"),
            LegacyBoundaryNames.PersistenceSystemNamespace("Settings"),
            "AddWeCmsSystemSettings",
            "SystemSettingsServiceCollectionExtensions",
            "ISettingCache"
        };

        var oldSystemSettingsFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Settings");
        var oldPersistenceSettingsFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "Settings");
        var violations = new List<string>();
        if (Directory.Exists(oldSystemSettingsFiles) && Directory.EnumerateFiles(oldSystemSettingsFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldSystemSettingsFiles)} still contains Settings source files");
        }

        if (Directory.Exists(oldPersistenceSettingsFiles) && Directory.EnumerateFiles(oldPersistenceSettingsFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldPersistenceSettingsFiles)} still contains Settings repository files");
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
    public async Task DictMigration_DoesNotUseOldSystemOrPersistenceBoundary()
    {
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.SystemNamespace("Dicts"),
            LegacyBoundaryNames.PersistenceSystemNamespace("Dicts"),
            "AddWeCmsSystemDicts",
            "SystemDictsServiceCollectionExtensions"
        };

        var oldSystemDictFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "Dicts");
        var oldPersistenceDictFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "Dicts");
        var violations = new List<string>();
        if (Directory.Exists(oldSystemDictFiles) && Directory.EnumerateFiles(oldSystemDictFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldSystemDictFiles)} still contains Dict source files");
        }

        if (Directory.Exists(oldPersistenceDictFiles) && Directory.EnumerateFiles(oldPersistenceDictFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldPersistenceDictFiles)} still contains Dict repository files");
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
    public async Task I18nMigration_DoesNotUseOldSystemOrPersistenceBoundary()
    {
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.SystemNamespace("I18n"),
            LegacyBoundaryNames.PersistenceSystemNamespace("I18n"),
            "AddWeCmsSystemI18n",
            "SystemI18nServiceCollectionExtensions"
        };

        var oldSystemI18nFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "I18n");
        var oldPersistenceI18nFiles = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.Persistence, "Modules", "System", "I18n");
        var violations = new List<string>();
        if (Directory.Exists(oldSystemI18nFiles) && Directory.EnumerateFiles(oldSystemI18nFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldSystemI18nFiles)} still contains I18n source files");
        }

        if (Directory.Exists(oldPersistenceI18nFiles) && Directory.EnumerateFiles(oldPersistenceI18nFiles, "*.cs", SearchOption.AllDirectories).Any())
        {
            violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, oldPersistenceI18nFiles)} still contains I18n repository files");
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
    public async Task ConfigurationModule_DoesNotContainSqlOrPersistenceReferences()
    {
        var moduleRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration");
        var forbiddenTokens = new[]
        {
            "SqlSugar",
            "MySqlConnector",
            LegacyBoundaryNames.Persistence,
            "WeCms.Modules.Configuration.SqlSugar",
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
    public async Task SettingWrite_CallsCacheInvalidator()
    {
        var source = await ReadSourceAsync("WeCms.Modules.Configuration", "Settings", "SettingService.cs");

        AssertMethodContains(source, "UpdateAsync", "InvalidateSettingsAsync");
        AssertMethodContains(source, "ReloadCacheAsync", "InvalidateSettingsAsync");
        Assert.Contains("_cacheInvalidator.InvalidateSettingsAsync(cancellationToken)", source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, "InvalidateSettingsAsync(cancellationToken)"));
    }

    [Fact]
    public async Task DictWrite_CallsCacheInvalidator()
    {
        var source = await ReadSourceAsync("WeCms.Modules.Configuration", "Dicts", "DictService.cs");
        var directWriteMethods = new[]
        {
            "CreateTypeAsync",
            "UpdateTypeAsync",
            "DeleteTypeAsync",
            "CreateValueAsync",
            "UpdateValueAsync",
            "DeleteValueAsync"
        };

        foreach (var methodName in directWriteMethods)
        {
            AssertMethodContains(source, methodName, "InvalidateDictsAsync");
        }

        AssertMethodContains(source, "SetTypeStatusAsync", "InvalidateDictsAsync");
        AssertMethodContains(source, "SetValueStatusAsync", "InvalidateDictsAsync");
        Assert.Contains("_cacheInvalidator.InvalidateDictsAsync(cancellationToken)", source, StringComparison.Ordinal);
        Assert.Equal(8, CountOccurrences(source, "await InvalidateDictsAsync(cancellationToken);"));
    }

    [Fact]
    public async Task I18nWrite_CallsCacheInvalidator()
    {
        var source = await ReadSourceAsync("WeCms.Modules.Configuration", "I18n", "I18nMessageService.cs");

        AssertMethodContains(source, "CreateAsync", "InvalidateI18nAsync");
        AssertMethodContains(source, "UpdateAsync", "InvalidateI18nAsync");
        AssertMethodContains(source, "DeleteAsync", "InvalidateI18nAsync");
        Assert.Contains("_cacheInvalidator.InvalidateI18nAsync(cancellationToken)", source, StringComparison.Ordinal);
        AssertMethodDoesNotContain(source, "GetPublicMessagesAsync", "InvalidateI18nAsync");
        AssertMethodDoesNotContain(source, "SwitchLocaleAsync", "InvalidateI18nAsync");
        Assert.Equal(3, CountOccurrences(source, "await InvalidateI18nAsync(cancellationToken);"));
    }

    [Fact]
    public void S7Scope_DoesNotMoveUnownedModulesIntoConfiguration()
    {
        var moduleRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration");
        var allowedTopLevelFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssemblyMarker.cs",
            "ConfigurationCacheInvalidator.cs",
            "ConfigurationClock.cs",
            "ConfigurationEndpointRouteBuilderExtensions.cs",
            "ConfigurationServiceCollectionExtensions.cs",
            "WeCms.Modules.Configuration.csproj"
        };
        var allowedTopLevelDirectories = new HashSet<string>(StringComparer.Ordinal)
        {
            "Dicts",
            "Events",
            "I18n",
            "Settings"
        };
        var ignoredTopLevelDirectories = new HashSet<string>(StringComparer.Ordinal)
        {
            "bin",
            "obj"
        };

        var violations = new List<string>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(moduleRoot))
        {
            var name = Path.GetFileName(entry);
            if (File.Exists(entry))
            {
                if (!allowedTopLevelFiles.Contains(name))
                {
                    violations.Add($"Unexpected Configuration top-level file: {name}");
                }

                continue;
            }

            if (!Directory.Exists(entry) || allowedTopLevelDirectories.Contains(name) || ignoredTopLevelDirectories.Contains(name))
            {
                continue;
            }

            var containsSourceFiles = Directory.EnumerateFiles(entry, "*.cs", SearchOption.AllDirectories).Any();
            if (containsSourceFiles)
            {
                violations.Add($"Unexpected Configuration top-level directory: {name}");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ConfigurationSqlSugarScope_DoesNotMoveUnownedModulesIntoConfigurationSqlSugar()
    {
        var moduleRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration.SqlSugar");
        var allowedTopLevelFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssemblyMarker.cs",
            "ConfigurationSqlSugarServiceCollectionExtensions.cs",
            "WeCms.Modules.Configuration.SqlSugar.csproj"
        };
        var allowedTopLevelDirectories = new HashSet<string>(StringComparer.Ordinal)
        {
            "CodeFirst",
            "Entities",
            "Repositories"
        };
        var ignoredTopLevelDirectories = new HashSet<string>(StringComparer.Ordinal)
        {
            "bin",
            "obj"
        };

        var violations = new List<string>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(moduleRoot))
        {
            var name = Path.GetFileName(entry);
            if (File.Exists(entry))
            {
                if (!allowedTopLevelFiles.Contains(name))
                {
                    violations.Add($"Unexpected Configuration.SqlSugar top-level file: {name}");
                }

                continue;
            }

            if (Directory.Exists(entry)
                && !allowedTopLevelDirectories.Contains(name)
                && !ignoredTopLevelDirectories.Contains(name))
            {
                violations.Add($"Unexpected Configuration.SqlSugar top-level directory: {name}");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task ConfigurationProjects_UseOnlyAllowedProjectReferences()
    {
        var configurationRefs = await ProjectReferencesAsync("WeCms.Modules.Configuration");
        var configurationSqlSugarRefs = await ProjectReferencesAsync("WeCms.Modules.Configuration.SqlSugar");

        Assert.Equal(["WeCms.EventBus", "WeCms.Shared"], configurationRefs);
        Assert.Equal(
            ["WeCms.Data.SqlSugar", "WeCms.Modules.Configuration", "WeCms.Shared"],
            configurationSqlSugarRefs);
    }

    [Fact]
    public async Task ConfigurationSources_DoNotReferenceUnownedModulesOrInfrastructure()
    {
        var roots = new[]
        {
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration.SqlSugar")
        };
        var forbiddenTokens = new[]
        {
            "WeCms.Modules.Audit",
            "WeCms.Modules.Security",
            "WeCms.Modules.FileCenter",
            "WeCms.Modules.Platform",
            "WeCms.Modules.Cms",
            "WeCms.Caching",
            "WeCms.Aop"
        };

        var violations = new List<string>();
        foreach (var file in roots.SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)))
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

    private static Task<string> ReadSourceAsync(params string[] relativeSegments)
    {
        var path = Path.Combine(new[] { TestPaths.SourceRoot }.Concat(relativeSegments).ToArray());
        return File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
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

    private static void AssertMethodContains(string source, string methodName, string expected)
    {
        var body = ExtractMethodBody(source, methodName);
        Assert.Contains(expected, body, StringComparison.Ordinal);
    }

    private static void AssertMethodDoesNotContain(string source, string methodName, string expected)
    {
        var body = ExtractMethodBody(source, methodName);
        Assert.DoesNotContain(expected, body, StringComparison.Ordinal);
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var methodStart = FindMethodDeclarationStart(source, methodName);
        Assert.True(methodStart >= 0, $"Method {methodName} was not found.");

        var braceStart = source.IndexOf('{', methodStart);
        Assert.True(braceStart >= 0, $"Method {methodName} does not have a block body.");

        var depth = 0;
        for (var index = braceStart; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[braceStart..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Method {methodName} body could not be parsed.");
    }

    private static int FindMethodDeclarationStart(string source, string methodName)
    {
        var index = 0;
        var needle = methodName + "(";
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            var lineStart = source.LastIndexOf('\n', index);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var linePrefix = source[lineStart..index];
            if (linePrefix.Contains("public ", StringComparison.Ordinal)
                || linePrefix.Contains("private ", StringComparison.Ordinal)
                || linePrefix.Contains("internal ", StringComparison.Ordinal))
            {
                return index;
            }

            index += needle.Length;
        }

        return -1;
    }

    private static int CountOccurrences(string source, string expected)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(expected, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += expected.Length;
        }

        return count;
    }

    private static IEnumerable<string> SourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "backend", "src"),
            Path.Combine(TestPaths.RepoRoot, "backend", "tests"),
            Path.Combine(TestPaths.RepoRoot, "scripts", "checks")
        };

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(root, "*.sh", SearchOption.AllDirectories)))
            .Where(path => !path.EndsWith(nameof(S7ConfigurationMigrationTests) + ".cs", StringComparison.Ordinal));
    }
}
