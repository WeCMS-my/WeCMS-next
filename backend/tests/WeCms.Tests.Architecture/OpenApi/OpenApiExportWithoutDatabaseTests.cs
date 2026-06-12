using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;

namespace WeCms.Tests.Architecture.OpenApi;

public sealed class OpenApiExportWithoutDatabaseTests
{
    [Fact]
    public async Task ExportOpenApiAsync_ShouldWriteDocument_WithoutDatabaseServices()
    {
        await using var app = BuildApp();
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{Guid.NewGuid():N}.json");

        await app.ExportOpenApiAsync(outputPath);

        Assert.True(File.Exists(outputPath));

        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("/api/v1/system/secure-ping", paths);
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();
        builder.Services.AddWeCmsInfrastructure();
        builder.Services.AddWeCmsPersistence();
        builder.Services.AddScoped<SystemEndpointHandlers>();
        builder.Services.AddScoped<PermissionEndpointFilter>();
        builder.Services.AddSingleton<System.Text.Json.Serialization.JsonSerializerContext>(WeCmsJsonContext.Default);

        var app = builder.Build();
        SystemEndpoints.Map(app);
        app.MapOpenApi();

        return app;
    }
}
