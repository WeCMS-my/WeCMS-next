using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["WeCms.Api"] =
            [
                "WeCms.Aop",
                "WeCms.Caching",
                "WeCms.Data.SqlSugar",
                "WeCms.EventBus",
                "WeCms.Infrastructure",
                "WeCms.Modules.AccessControl",
                "WeCms.Modules.AccessControl.SqlSugar",
                "WeCms.Modules.Audit",
                "WeCms.Modules.Audit.SqlSugar",
                "WeCms.Modules.Cms",
                "WeCms.Modules.Configuration",
                "WeCms.Modules.Configuration.SqlSugar",
                "WeCms.Modules.FileCenter",
                "WeCms.Modules.FileCenter.SqlSugar",
                "WeCms.Modules.Identity",
                "WeCms.Modules.Identity.SqlSugar",
                "WeCms.Modules.Organization",
                "WeCms.Modules.Organization.SqlSugar",
                "WeCms.Modules.Platform",
                "WeCms.Modules.Security",
                "WeCms.Modules.Security.SqlSugar",
                "WeCms.Modules.System",
                "WeCms.Persistence",
                "WeCms.Shared"
            ],
            ["WeCms.Aop"] = ["WeCms.Caching", "WeCms.EventBus", "WeCms.Shared"],
            ["WeCms.Caching"] = ["WeCms.Shared"],
            ["WeCms.Data.SqlSugar"] = ["WeCms.Shared"],
            ["WeCms.EventBus"] = ["WeCms.Shared"],
            ["WeCms.Infrastructure"] = ["WeCms.Shared"],
            ["WeCms.Modules.AccessControl"] = ["WeCms.Shared"],
            ["WeCms.Modules.AccessControl.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.AccessControl", "WeCms.Shared"],
            ["WeCms.Modules.Audit"] = ["WeCms.Shared"],
            ["WeCms.Modules.Audit.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.Audit", "WeCms.Shared"],
            ["WeCms.Modules.Cms"] = ["WeCms.Shared"],
            ["WeCms.Modules.Configuration"] = ["WeCms.Shared"],
            ["WeCms.Modules.Configuration.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.Configuration", "WeCms.Shared"],
            ["WeCms.Modules.FileCenter"] = ["WeCms.Shared"],
            ["WeCms.Modules.FileCenter.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.FileCenter", "WeCms.Shared"],
            ["WeCms.Modules.Identity"] = ["WeCms.Shared"],
            ["WeCms.Modules.Identity.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.Identity", "WeCms.Shared"],
            ["WeCms.Modules.Organization"] = ["WeCms.Shared"],
            ["WeCms.Modules.Organization.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.Organization", "WeCms.Shared"],
            ["WeCms.Modules.Platform"] = ["WeCms.Shared"],
            ["WeCms.Modules.Security"] = ["WeCms.Shared"],
            ["WeCms.Modules.Security.SqlSugar"] = ["WeCms.Data.SqlSugar", "WeCms.Modules.Security", "WeCms.Shared"],
            ["WeCms.Modules.System"] = ["WeCms.Shared"],
            ["WeCms.Persistence"] = ["WeCms.Modules.Cms", "WeCms.Modules.System", "WeCms.Shared"],
            ["WeCms.Shared"] = []
        };

    [Fact]
    public void ProductionProjects_UseOnlyAllowedProjectReferences()
    {
        foreach (var project in ProductionProjects())
        {
            var actualReferences = ProjectReferences(project.Path);
            var allowedReferences = AllowedProjectReferences[project.Name];
            var unexpectedReferences = actualReferences.Except(allowedReferences, StringComparer.Ordinal).ToArray();

            Assert.True(
                unexpectedReferences.Length == 0,
                $"{project.Name} has forbidden project references: {string.Join(", ", unexpectedReferences)}");
        }
    }

    [Fact]
    public void SharedProject_HasNoProductionProjectReferences()
    {
        var sharedProject = ProductionProjects().Single(project => project.Name == "WeCms.Shared");

        Assert.Empty(ProjectReferences(sharedProject.Path));
    }

    [Fact]
    public void Modules_DoNotReferencePersistence()
    {
        var moduleProjects = ProductionProjects()
            .Where(project => project.Name.StartsWith("WeCms.Modules.", StringComparison.Ordinal));

        foreach (var project in moduleProjects)
        {
            Assert.DoesNotContain("WeCms.Persistence", ProjectReferences(project.Path));
        }
    }

    [Fact]
    public void BusinessModules_DoNotReferenceSqlSugarAdapterProjects()
    {
        var businessModuleProjects = ProductionProjects()
            .Where(project => project.Name.StartsWith("WeCms.Modules.", StringComparison.Ordinal))
            .Where(project => !project.Name.EndsWith(".SqlSugar", StringComparison.Ordinal));

        foreach (var project in businessModuleProjects)
        {
            var adapterReferences = ProjectReferences(project.Path)
                .Where(reference => reference.EndsWith(".SqlSugar", StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                adapterReferences.Length == 0,
                $"{project.Name} references SqlSugar adapter projects: {string.Join(", ", adapterReferences)}");
        }
    }

    private static IEnumerable<(string Name, string Path)> ProductionProjects()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: path))
            .OrderBy(project => project.Name, StringComparer.Ordinal);
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
