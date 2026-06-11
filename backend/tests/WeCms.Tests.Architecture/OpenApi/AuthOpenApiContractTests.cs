using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeCms.Modules.System.Auth;

namespace WeCms.Tests.Architecture.OpenApi;

public sealed class AuthOpenApiContractTests
{
    [Fact]
    public void LoginRequestBody_ShouldMatchLoginRequestDto()
    {
        AssertRequestBodyMatches<LoginRequest>("/api/v1/auth/login");
    }

    [Fact]
    public void RefreshRequestBody_ShouldMatchRefreshRequestDto()
    {
        AssertRequestBodyMatches<RefreshRequest>("/api/v1/auth/refresh");
    }

    [Fact]
    public void LogoutRequestBody_ShouldMatchLogoutRequestDto()
    {
        AssertRequestBodyMatches<LogoutRequest>("/api/v1/auth/logout");
    }

    private static void AssertRequestBodyMatches<TRequest>(string route)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetArtifactPath()));

        var root = document.RootElement;
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

    private static string GetArtifactPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return Path.Combine(current.FullName, "artifacts", "openapi", "wecms-api-v1.json");
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
