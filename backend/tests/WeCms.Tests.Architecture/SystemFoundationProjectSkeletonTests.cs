using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class SystemFoundationProjectSkeletonTests
{
    private static readonly string[] PlatformProjects =
    [
        "WeCms.Data.SqlSugar",
        "WeCms.Caching",
        "WeCms.EventBus",
        "WeCms.EventBus.SqlSugar",
        "WeCms.Aop"
    ];

    private static readonly string[] ModuleProjects =
    [
        "WeCms.Modules.Identity",
        "WeCms.Modules.AccessControl",
        "WeCms.Modules.Organization",
        "WeCms.Modules.Configuration",
        "WeCms.Modules.Audit",
        "WeCms.Modules.Security",
        "WeCms.Modules.FileCenter",
        "WeCms.Modules.Platform"
    ];

    private static readonly string[] ModuleSqlSugarProjects =
    [
        "WeCms.Modules.Identity.SqlSugar",
        "WeCms.Modules.AccessControl.SqlSugar",
        "WeCms.Modules.Organization.SqlSugar",
        "WeCms.Modules.Configuration.SqlSugar",
        "WeCms.Modules.Audit.SqlSugar",
        "WeCms.Modules.Security.SqlSugar",
        "WeCms.Modules.FileCenter.SqlSugar"
    ];

    [Fact]
    public void SystemFoundationProjects_ExistAndAreIncludedInSolution()
    {
        var solution = File.ReadAllText(Path.Combine(TestPaths.BackendRoot, "WeCms.slnx"));

        foreach (var projectName in PlatformProjects.Concat(ModuleProjects).Concat(ModuleSqlSugarProjects))
        {
            var projectPath = ProjectPath(projectName);
            Assert.True(File.Exists(projectPath), $"Missing project: {projectPath}");
            Assert.Contains($"src/{projectName}/{projectName}.csproj", solution, StringComparison.Ordinal);
            AssertAssemblyMarker(projectName);
        }
    }

    [Fact]
    public void PlatformProjects_HaveExpectedReferences()
    {
        AssertProjectReferences("WeCms.Data.SqlSugar", "WeCms.Shared");
        AssertProjectReferences("WeCms.Caching", "WeCms.Shared");
        AssertProjectReferences("WeCms.EventBus", "WeCms.Shared");
        AssertProjectReferences("WeCms.EventBus.SqlSugar", "WeCms.Data.SqlSugar", "WeCms.EventBus", "WeCms.Shared");
        AssertProjectReferences("WeCms.Aop", "WeCms.Caching", "WeCms.EventBus", "WeCms.Shared");
    }

    [Fact]
    public void BusinessModules_OnlyReferenceSharedAndExposeSkeletonFolders()
    {
        foreach (var projectName in ModuleProjects)
        {
            if (projectName == "WeCms.Modules.Identity")
            {
                AssertProjectReferences(projectName, "WeCms.EventBus", "WeCms.Modules.AccessControl", "WeCms.Modules.Organization", "WeCms.Shared");
            }
            else if (projectName is "WeCms.Modules.AccessControl" or "WeCms.Modules.Configuration" or "WeCms.Modules.Security")
            {
                AssertProjectReferences(projectName, "WeCms.EventBus", "WeCms.Shared");
            }
            else
            {
                AssertProjectReferences(projectName, "WeCms.Shared");
            }

            foreach (var folder in new[] { "Endpoints", "Services", "Contracts", "Permissions", "Repositories", "Records" })
            {
                Assert.True(Directory.Exists(Path.Combine(TestPaths.SourceRoot, projectName, folder)), $"{projectName} missing {folder}");
            }
        }
    }

    [Fact]
    public void ModuleSqlSugarAdapters_ReferenceOnlyCorrespondingModuleDataSqlSugarAndShared()
    {
        foreach (var projectName in ModuleSqlSugarProjects)
        {
            var moduleProject = projectName[..^".SqlSugar".Length];
            AssertProjectReferences(projectName, moduleProject, "WeCms.Data.SqlSugar", "WeCms.Shared");

            foreach (var folder in new[] { "Entities", "Repositories", "CodeFirst" })
            {
                Assert.True(Directory.Exists(Path.Combine(TestPaths.SourceRoot, projectName, folder)), $"{projectName} missing {folder}");
            }
        }
    }

    private static string ProjectPath(string projectName)
    {
        return Path.Combine(TestPaths.SourceRoot, projectName, $"{projectName}.csproj");
    }

    private static void AssertAssemblyMarker(string projectName)
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.SourceRoot, projectName, "AssemblyMarker.cs"));
        Assert.Contains($"namespace {projectName};", source, StringComparison.Ordinal);
        Assert.Contains("public static class AssemblyMarker", source, StringComparison.Ordinal);
    }

    private static void AssertProjectReferences(string projectName, params string[] expectedReferences)
    {
        var actualReferences = ProjectReferences(ProjectPath(projectName));
        var expected = expectedReferences.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actualReferences);
    }

    private static string[] ProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path has no directory: {projectPath}");
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Replace('\\', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
            .Select(Path.GetFileNameWithoutExtension)
            .Select(projectName => projectName ?? throw new InvalidOperationException($"Could not resolve project reference in {projectPath}."))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
