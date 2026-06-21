namespace WeCms.Tests.Architecture;

public sealed class IdentityEndpointMigrationTests
{
    private static readonly string IdentityRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    private static readonly string[] IdentityEndpointFiles =
    [
        Path.Combine("Endpoints", "AuthEndpointDefinition.cs"),
        Path.Combine("Endpoints", "AccountProfileEndpointDefinition.cs"),
        Path.Combine("Endpoints", "AccountTwoFactorEndpointDefinition.cs"),
        Path.Combine("Endpoints", "UserEndpointDefinition.cs")
    ];

    private static readonly string[] LegacySystemIdentityEndpointFiles =
    [
        Path.Combine("Auth", "AuthEndpoints.cs"),
        Path.Combine("Auth", "AccountProfileEndpoints.cs"),
        Path.Combine("Auth", "AccountTwoFactorEndpoints.cs"),
        Path.Combine("Users", "UserEndpoints.cs")
    ];

    [Fact]
    public void IdentityEndpointDefinitions_LiveInIdentityModule()
    {
        foreach (var relativePath in IdentityEndpointFiles)
        {
            var path = Path.Combine(IdentityRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing Identity endpoint definition file: {relativePath}");
        }
    }

    [Fact]
    public async Task IdentityEndpointDefinitions_UseExplicitEndpointDefinitionPattern()
    {
        foreach (var relativePath in IdentityEndpointFiles)
        {
            var source = await File.ReadAllTextAsync(Path.Combine(IdentityRoot, relativePath), TestContext.Current.CancellationToken);

            Assert.Contains("namespace WeCms.Modules.Identity.Endpoints;", source, StringComparison.Ordinal);
            Assert.Contains("IEndpointDefinition", source, StringComparison.Ordinal);
            Assert.Contains("void MapEndpoints(IEndpointRouteBuilder endpoints)", source, StringComparison.Ordinal);
            Assert.DoesNotContain(LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemIdentityEndpointFiles_AreRemoved()
    {
        var remaining = LegacySystemIdentityEndpointFiles
            .Select(relativePath => Path.Combine(SystemRoot, relativePath))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(SystemRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy System identity endpoint files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task ApiProgram_RegistersIdentityEndpointDefinitions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        foreach (var definition in new[]
        {
            "AuthEndpointDefinition",
            "AccountProfileEndpointDefinition",
            "AccountTwoFactorEndpointDefinition",
            "UserEndpointDefinition"
        })
        {
            Assert.Contains($"registry.Add(new {definition}());", source, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("app.MapAuthEndpoints();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("app.MapAccountProfileEndpoints();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("app.MapAccountTwoFactorEndpoints();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("app.MapUserEndpoints();", source, StringComparison.Ordinal);
    }
}
