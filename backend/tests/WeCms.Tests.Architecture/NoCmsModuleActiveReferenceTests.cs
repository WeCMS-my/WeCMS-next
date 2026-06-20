using System.Xml.Linq;

namespace WeCms.Tests.Architecture;

public sealed class NoCmsModuleActiveReferenceTests
{
    private static readonly string[] ActiveCmsTokens =
    [
        "MapCms",
        "CmsEndpoint",
        "CmsEndpoints",
        "CmsPermissions",
        "AddWeCmsCms",
        "RequirePermission(Cms"
    ];

    [Fact]
    public void CmsModule_IsPlaceholderOnlyDuringSystemFoundationUpgrade()
    {
        var sourceFiles = Directory.EnumerateFiles(CmsProjectRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(file => Path.GetFileName(file) ?? throw new InvalidOperationException($"File has no name: {file}"))
            .ToArray();

        Assert.Equal(["AssemblyMarker.cs"], sourceFiles);
    }

    [Fact]
    public void ApiAndPersistence_DoNotReferenceCmsProject()
    {
        var activeReferences = new[]
            {
                ProjectPath("WeCms.Api"),
                ProjectPath("WeCms.Persistence")
            }
            .SelectMany(ProjectReferences)
            .Where(reference => reference == "WeCms.Modules.Cms")
            .ToArray();

        Assert.True(
            activeReferences.Length == 0,
            $"CMS module must stay inactive during system foundation upgrade: {string.Join(", ", activeReferences)}");
    }

    [Fact]
    public void ProductionCode_DoesNotRegisterCmsEndpointsOrPermissions()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => ActiveCmsTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}"))
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    private static string CmsProjectRoot()
    {
        return Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Cms");
    }

    private static string ProjectPath(string projectName)
    {
        return Path.Combine(TestPaths.SourceRoot, projectName, $"{projectName}.csproj");
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
