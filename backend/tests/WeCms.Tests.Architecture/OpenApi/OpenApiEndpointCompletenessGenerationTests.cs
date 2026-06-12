using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Shared.Security;

namespace WeCms.Tests.Architecture.OpenApi;

public sealed class OpenApiEndpointCompletenessGenerationTests
{
    private static readonly string[] ExpectedPaths =
    [
        "/health/live",
        "/health/ready",
        "/api/v1/system/ping",
        "/api/v1/system/version",
        "/api/v1/system/db-check",
        "/api/v1/system/secure-ping",
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/api/v1/auth/logout",
        "/api/v1/auth/me",
    ];

    [Fact]
    public async Task GeneratedDocument_ShouldContainAllMappedPaths()
    {
        await using var app = BuildApp();
        await app.StartAsync(CancellationToken.None);

        var client = app.GetTestClient();
        await using var stream = await client.GetStreamAsync("/openapi/v1.json");
        using var document = await JsonDocument.ParseAsync(stream);

        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingPaths = ExpectedPaths.Where(path => !paths.Contains(path)).ToArray();

        Assert.True(missingPaths.Length == 0, $"Generated OpenAPI document is missing paths: {string.Join(", ", missingPaths)}");
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<AuthEndpointHandlers>();
        builder.Services.AddScoped<SystemEndpointHandlers>();
        builder.Services.AddScoped<PermissionEndpointFilter>();
        builder.Services.AddSingleton<IPermissionChecker, AllowAllPermissionChecker>();

        var app = builder.Build();
        SystemEndpoints.Map(app);
        app.MapAuthEndpoints();
        app.MapOpenApi();

        return app;
    }

    private sealed class AllowAllPermissionChecker : IPermissionChecker
    {
        public Task<PermissionCheckResult> CheckAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new PermissionCheckResult(true, true));
    }
}
