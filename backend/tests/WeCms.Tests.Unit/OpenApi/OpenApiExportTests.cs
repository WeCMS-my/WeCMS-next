using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;
using WeCms.Shared;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiExportTests
{
    [Fact]
    public async Task ExportOpenApiAsync_WritesExpectedContract()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            var paths = root.GetProperty("paths");
            var schemas = root.GetProperty("components").GetProperty("schemas");

            Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var loginPath));
            Assert.True(loginPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/refresh", out var refreshPath));
            Assert.True(refreshPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/logout", out var logoutPath));
            Assert.True(logoutPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(schemas.TryGetProperty("ApiResult", out _));
            Assert.True(schemas.TryGetProperty("LoginResponse", out _));

            Assert.True(paths.TryGetProperty("/health/live", out _));
            Assert.True(paths.TryGetProperty("/health/ready", out _));
            Assert.True(paths.TryGetProperty("/api/v1/system/db-check", out _));

            var securePing = paths.GetProperty("/api/v1/system/secure-ping").GetProperty("get");
            Assert.Equal("sys:system:secure-ping", securePing.GetProperty("x-wecms-permission").GetString());
            Assert.True(securePing.TryGetProperty("security", out _));
            AssertAllRefsResolve(root);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_CoverageMatchesRegisteredEndpoints()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-coverage-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;

            var contractOperations = CollectRegisteredEndpointMetadata();
            var openApiOperations = CollectOpenApiOperations(root.GetProperty("paths"));

            Assert.Equal(contractOperations.Count, openApiOperations.Count);
            AssertOpenApiOperationsMatch(contractOperations, openApiOperations);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_RequestBodyRefsMatchOpenApiMetadata()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-requestbody-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var paths = document.RootElement.GetProperty("paths");
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

            foreach (var registered in CollectRegisteredEndpointMetadata().Where(operation => operation.Method is "post" or "put"))
            {
                var operation = paths.GetProperty(registered.Path).GetProperty(registered.Method);
                var requestBody = operation.GetProperty("requestBody");
                var schemaRef = requestBody
                    .GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetProperty("$ref")
                    .GetString();

                Assert.NotNull(registered.RequestBody);
                Assert.Equal($"#/components/schemas/{registered.RequestBody}", schemaRef);
                Assert.True(schemas.TryGetProperty(registered.RequestBody, out _));
            }
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private static void AssertAllRefsResolve(JsonElement root)
    {
        var schemas = root.GetProperty("components").GetProperty("schemas");
        foreach (var element in Walk(root))
        {
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty("$ref", out var refElement))
            {
                continue;
            }

            var reference = refElement.GetString();
            Assert.NotNull(reference);
            const string prefix = "#/components/schemas/";
            Assert.StartsWith(prefix, reference, StringComparison.Ordinal);
            Assert.True(schemas.TryGetProperty(reference[prefix.Length..], out _), $"Dangling $ref: {reference}");
        }
    }

    private static IEnumerable<JsonElement> Walk(JsonElement element)
    {
        yield return element;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in Walk(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in Walk(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static HashSet<(string Path, string Method)> CollectOpenApiOperations(JsonElement paths)
    {
        var operations = new HashSet<(string Path, string Method)>();

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                operations.Add((path.Name, method.Name.ToLowerInvariant()));
            }
        }

        return operations;
    }

    private static HashSet<RegisteredEndpoint> CollectRegisteredEndpointMetadata()
    {
        using var app = CreateDiscoveryApp();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is not null)
            .SelectMany(
                endpoint => endpoint.Metadata.OfType<HttpMethodMetadata>()
                    .SelectMany(metadata => metadata.HttpMethods)
                    .Where(method => !string.IsNullOrWhiteSpace(method))
            .Select(method => new RegisteredEndpoint(
                        endpoint.RoutePattern!.RawText!,
                        Method: method.ToLowerInvariant(),
                        Permission: endpoint.Metadata.OfType<PermissionMetadata>().Select(metadata => metadata.Code).FirstOrDefault(),
                        RequestBody: endpoint.Metadata.OfType<OpenApiRequestBodyMetadata>().Select(metadata => metadata.RequestType.Name).FirstOrDefault())))
            .ToHashSet();
    }

    private static WebApplication CreateDiscoveryApp()
    {
        var builder = WebApplication.CreateSlimBuilder(Array.Empty<string>());
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>( "ConnectionStrings:Default", "Server=127.0.0.1;Database=wecms_openapi;Uid=dummy;Pwd=dummy;"),
            new KeyValuePair<string, string?>( "Auth:AccessTokenSecret", "openapi-secret-openapi-secret-openapi-secret-openapi-secret")
        ]);
        builder.Services.AddWeCmsPersistence(builder.Configuration);
        builder.Services.AddWeCmsSystemAuth(builder.Configuration);
        builder.Services.AddWeCmsSystemPermissions();

        var app = builder.Build();
        app.MapSystemEndpoints();
        app.MapAuthEndpoints();
        app.MapSystemPermissionEndpoints();

        return app;
    }

    private sealed record RegisteredEndpoint(string Path, string Method, string? Permission, string? RequestBody);
    
    private static void AssertOpenApiOperationsMatch(
        IEnumerable<RegisteredEndpoint> registered,
        HashSet<(string Path, string Method)> openApiOperations)
    {
        var registeredSet = registered.Select(endpoint => (endpoint.Path, endpoint.Method)).ToHashSet();

        Assert.All(registeredSet, operation =>
        {
            Assert.True(openApiOperations.Contains(operation), $"OpenAPI missing {operation.Method.ToUpperInvariant()} {operation.Path}");
        });

        Assert.All(openApiOperations, operation =>
        {
            Assert.True(
                registeredSet.Contains(operation),
                $"OpenAPI contains unregistered endpoint {operation.Method.ToUpperInvariant()} {operation.Path}");
        });
    }

    [Fact]
    public async Task ExportOpenApiAsync_ReturnsFalseWhenArgumentIsMissing()
    {
        var handled = await OpenApiExtensions.ExportOpenApiAsync([]);

        Assert.False(handled);
    }
}
