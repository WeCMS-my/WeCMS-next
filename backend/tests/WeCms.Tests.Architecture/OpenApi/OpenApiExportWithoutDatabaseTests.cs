using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;
using WeCms.Shared.Data;

namespace WeCms.Tests.Architecture.OpenApi;

public sealed class OpenApiExportWithoutDatabaseTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public async Task ExportOpenApiAsync_ShouldWriteDocument_WithoutDatabaseServices()
    {
        await using var app = BuildApp();
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{Guid.NewGuid():N}.json");

        await app.ExportOpenApiAsync(outputPath);

        Assert.True(File.Exists(outputPath));

        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        Assert.Equal("3.1.1", document.RootElement.GetProperty("openapi").GetString());
        Assert.True(document.RootElement.TryGetProperty("paths", out _), document.RootElement.GetRawText());
        Assert.Equal(
            "http://localhost:5000/",
            document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString());
    }

    [Fact]
    public void OpenApiExportReflection_ShouldBeDocumentedAndIsolatedToExportPath()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Extensions",
            "OpenApiExtensions.cs"));
        var adrPath = Path.Combine(
            RepoRoot,
            "docs",
            "adr",
            "0008-openapi-export-reflection-isolated.md");

        Assert.True(File.Exists(adrPath), "ADR-0008 must document the CLI-only OpenAPI reflection exception.");
        Assert.Contains("ADR-0008", source, StringComparison.Ordinal);
        Assert.Contains("--export-openapi", source, StringComparison.Ordinal);
        Assert.Contains("GenerateOpenApiJsonForExportOnlyAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiAspNetCorePackageReferences_ShouldUseStableUnifiedVersions()
    {
        var project = XDocument.Load(Path.Combine(
            RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "WeCms.Api.csproj"));

        var aspNetCorePackages = project
            .Descendants("PackageReference")
            .Select(reference => new
            {
                Include = (string?)reference.Attribute("Include"),
                Version = (string?)reference.Attribute("Version")
            })
            .Where(reference => reference.Include is not null && reference.Include.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(aspNetCorePackages);
        Assert.All(aspNetCorePackages, reference => Assert.Equal("10.0.0", reference.Version));
        Assert.All(aspNetCorePackages, reference => Assert.DoesNotContain("preview", reference.Version, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExportModeParsing_ShouldFindFlag_WhenLauncherAddsArguments()
    {
        var args = new[] { "--applicationName", "WeCms.Api", "--export-openapi", "/tmp/wecms-openapi.json", "--nologo" };

        Assert.True(OpenApiExtensions.IsExportMode(args));
        Assert.Equal("/tmp/wecms-openapi.json", OpenApiExtensions.GetExportPath(args));
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateSlimBuilder([]);

        builder.Services.AddRouting();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();
        builder.Services.AddWeCmsInfrastructure();
        builder.Services.AddWeCmsPersistence();
        builder.Services.AddSingleton<IDbConnectionFactory, ThrowingDbConnectionFactory>();
        builder.Services.AddScoped<AuthEndpointHandlers>();
        builder.Services.AddSingleton<SystemEndpointHandlers>();
        builder.Services.AddScoped<PermissionEndpointFilter>();
        builder.Services.AddSingleton<System.Text.Json.Serialization.JsonSerializerContext>(WeCmsJsonContext.Default);

        var app = builder.Build();
        var systemEndpointHandlers = app.Services.GetRequiredService<SystemEndpointHandlers>();
        SystemEndpoints.Map(app, systemEndpointHandlers);
        app.MapAuthEndpoints();
        app.MapOpenApi();

        return app;
    }

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "backend", "WeCms.slnx")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Unable to locate WeCMS repository root.");
    }

    private sealed class ThrowingDbConnectionFactory : IDbConnectionFactory
    {
        public Task<System.Data.Common.DbConnection> OpenAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("DB is not needed for OpenAPI export tests.");
    }
}
