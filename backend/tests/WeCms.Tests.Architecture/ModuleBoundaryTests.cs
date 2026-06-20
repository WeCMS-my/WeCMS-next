using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] TargetBusinessModules =
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

    [Fact]
    public void TargetBusinessModules_DoNotReferenceInfrastructureOrAdapters()
    {
        foreach (var module in TargetBusinessModules)
        {
            var references = ProjectReferences(ProjectPath(module));
            var forbidden = references
                .Where(reference => reference is "WeCms.Persistence" or "WeCms.Data.SqlSugar")
                .Concat(references.Where(reference => reference.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
                    && reference.EndsWith(".SqlSugar", StringComparison.Ordinal)))
                .ToArray();

            Assert.True(
                forbidden.Length == 0,
                $"{module} references concrete infrastructure or adapters: {string.Join(", ", forbidden)}");
        }
    }

    [Fact]
    public void SqlSugarAdapterModules_ReferenceOnlyTheirBusinessModuleDataPlatformAndShared()
    {
        foreach (var adapterProject in AdapterProjects())
        {
            var moduleName = adapterProject.Name[..^".SqlSugar".Length];
            var allowed = new[] { moduleName, "WeCms.Data.SqlSugar", "WeCms.Shared" };
            var forbidden = ProjectReferences(adapterProject.Path)
                .Except(allowed, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                forbidden.Length == 0,
                $"{adapterProject.Name} has forbidden project references: {string.Join(", ", forbidden)}");
        }
    }

    [Fact]
    public void EveryDataBackedTargetBusinessModule_HasSqlSugarAdapterProject()
    {
        var expectedAdapters = TargetBusinessModules
            .Where(module => module != "WeCms.Modules.Platform")
            .Select(module => $"{module}.SqlSugar")
            .ToArray();
        var actualAdapters = AdapterProjects()
            .Select(project => project.Name)
            .ToArray();
        var missing = expectedAdapters.Except(actualAdapters, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0,
            $"Missing SqlSugar adapter projects: {string.Join(", ", missing)}");
    }

    private static string ProjectPath(string projectName)
    {
        var path = Path.Combine(TestPaths.SourceRoot, projectName, $"{projectName}.csproj");
        Assert.True(File.Exists(path), $"Missing production project: {projectName}");
        return path;
    }

    private static IEnumerable<(string Name, string Path)> AdapterProjects()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: path))
            .Where(project => project.Name.StartsWith("WeCms.Modules.", StringComparison.Ordinal))
            .Where(project => project.Name.EndsWith(".SqlSugar", StringComparison.Ordinal))
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
