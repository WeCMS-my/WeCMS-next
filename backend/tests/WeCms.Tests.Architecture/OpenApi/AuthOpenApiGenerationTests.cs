using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;

namespace WeCms.Tests.Architecture.OpenApi;

public sealed class AuthOpenApiGenerationTests
{
    [Fact]
    public async Task GeneratedDocument_LoginRefreshAndLogoutRequestBodies_ShouldMatchAuthDtos()
    {
        await using var app = BuildMinimalAuthApp();
        await app.StartAsync(CancellationToken.None);

        var client = app.GetTestClient();
        var document = await JsonDocument.ParseAsync(await client.GetStreamAsync("/openapi/v1.json"));

        AssertRequestBodyMatches<LoginRequest>(document, "/api/v1/auth/login");
        AssertRequestBodyMatches<RefreshRequest>(document, "/api/v1/auth/refresh");
        AssertRequestBodyMatches<LogoutRequest>(document, "/api/v1/auth/logout");
    }

    private static WebApplication BuildMinimalAuthApp()
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.MapAuthEndpoints();
        app.MapOpenApi();

        return app;
    }

    private static void AssertRequestBodyMatches<TRequest>(JsonDocument document, string route)
    {
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("paths", out _), root.GetRawText());
        var requestBody = root.GetProperty("paths")
            .GetProperty(route)
            .GetProperty("post")
            .GetProperty("requestBody");

        Assert.True(requestBody.GetProperty("required").GetBoolean());

        var schema = ResolveSchema(root, requestBody.GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema"));

        Assert.Equal("object", schema.GetProperty("type").GetString());

        var actualProperties = schema.GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expectedProperties = GetExpectedPropertyNames<TRequest>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedProperties, actualProperties);

        var actualRequired = schema.TryGetProperty("required", out var requiredNode)
            ? requiredNode.EnumerateArray().Select(item => item.GetString()!).OrderBy(name => name, StringComparer.Ordinal).ToArray()
            : [];

        Assert.Equal(expectedProperties, actualRequired);
    }

    private static JsonElement ResolveSchema(JsonElement root, JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var refNode))
        {
            var schemaName = refNode.GetString()!.Split('/').Last();
            return root.GetProperty("components").GetProperty("schemas").GetProperty(schemaName);
        }

        return schema;
    }

    private static string[] GetExpectedPropertyNames<TRequest>()
    {
        var nullability = new NullabilityInfoContext();

        return typeof(TRequest)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod is not null && property.GetIndexParameters().Length == 0)
            .Where(property => property.PropertyType.IsValueType || nullability.Create(property).ReadState == NullabilityState.NotNull)
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .ToArray();
    }
}
