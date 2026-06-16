using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["WeCms.Api"] =
            [
                "WeCms.Infrastructure",
                "WeCms.Modules.Cms",
                "WeCms.Modules.System",
                "WeCms.Persistence",
                "WeCms.Shared"
            ],
            ["WeCms.Infrastructure"] = ["WeCms.Shared"],
            ["WeCms.Modules.Cms"] = ["WeCms.Shared"],
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
