using System.Text.Json;
using System.Text.Json.Nodes;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task<bool> ExportOpenApiAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var outputPath = OutputPath(args);
        if (outputPath is null)
        {
            return false;
        }

        outputPath = ResolveOutputPath(outputPath);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = CreateDocument();
        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);

        return true;
    }

    private static string? OutputPath(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], "--export-openapi", StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                throw new InvalidOperationException("--export-openapi requires an output path.");
            }

            return args[index + 1];
        }

        return null;
    }

    private static string ResolveOutputPath(string outputPath)
    {
        return Path.IsPathRooted(outputPath)
            ? outputPath
            : Path.Combine(FindRepoRoot(), outputPath);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for OpenAPI export.");
    }

    private static JsonObject CreateDocument()
    {
        var endpoints = DiscoverEndpoints();
        var endpointMetadata = DiscoverEndpointMetadata();
        return new JsonObject
        {
            ["openapi"] = "3.1.0",
            ["info"] = new JsonObject
            {
                ["title"] = "WeCMS API",
                ["version"] = "v1"
            },
            ["paths"] = Paths(endpoints, endpointMetadata),
            ["components"] = Components()
        };
    }

    private static List<OpenApiEndpointDescriptor> DiscoverEndpoints()
    {
        return RegisteredDiscoveryEndpoints
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
            .ToList();
    }

    private static JsonObject Paths(
        IEnumerable<OpenApiEndpointDescriptor> endpoints,
        IReadOnlyDictionary<OpenApiOperationKey, OpenApiRuntimeEndpointMetadata> endpointMetadata)
    {
        var paths = new JsonObject();
        foreach (var endpoint in endpoints)
        {
            if (paths[endpoint.Path] is not JsonObject pathObject)
            {
                pathObject = new JsonObject();
                paths[endpoint.Path] = pathObject;
            }

            endpointMetadata.TryGetValue(new OpenApiOperationKey(endpoint.Path, endpoint.Method), out var metadata);
            pathObject[endpoint.Method] = Operation(
                path: endpoint.Path,
                method: endpoint.Method,
                tag: TagForPath(endpoint.Path),
                summary: SummaryForPath(endpoint.Path, endpoint.Method),
                responseRef: endpoint.ResponseType,
                requestRef: endpoint.RequestBodyType,
                security: endpoint.Security,
                metadata: metadata);
        }

        return paths;
    }


    private static string TagForPath(string path)
    {
        if (path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
        {
            return "Auth";
        }

        if (path.StartsWith("/api/v1/system/menus", StringComparison.OrdinalIgnoreCase))
        {
            return "Menus";
        }

        if (path.StartsWith("/api/v1/system/depts", StringComparison.OrdinalIgnoreCase))
        {
            return "Departments";
        }

        if (path.StartsWith("/api/v1/system/positions", StringComparison.OrdinalIgnoreCase))
        {
            return "Positions";
        }

        if (path.StartsWith("/api/v1/system/dict", StringComparison.OrdinalIgnoreCase))
        {
            return "Dicts";
        }

        if (path.StartsWith("/api/v1/system/settings", StringComparison.OrdinalIgnoreCase))
        {
            return "Settings";
        }

        if (path.StartsWith("/api/v1/system/i18n", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/i18n", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/account/i18n", StringComparison.OrdinalIgnoreCase))
        {
            return "I18n";
        }

        if (path.StartsWith("/api/v1/system/login-logs", StringComparison.OrdinalIgnoreCase))
        {
            return "LoginLogs";
        }

        if (path.StartsWith("/api/v1/system/audit-logs", StringComparison.OrdinalIgnoreCase))
        {
            return "AuditLogs";
        }

        if (path.StartsWith("/api/v1/system/security-events", StringComparison.OrdinalIgnoreCase))
        {
            return "SecurityEvents";
        }

        if (path.StartsWith("/api/v1/system/security", StringComparison.OrdinalIgnoreCase))
        {
            return "Security";
        }

        if (path.StartsWith("/api/v1/system/files", StringComparison.OrdinalIgnoreCase))
        {
            return "Files";
        }

        if (path.StartsWith("/api/v1/system/permissions", StringComparison.OrdinalIgnoreCase))
        {
            return "Permissions";
        }

        if (path.StartsWith("/api/v1/system/users", StringComparison.OrdinalIgnoreCase))
        {
            return "Users";
        }

        return path.StartsWith("/api/v1/system/roles", StringComparison.OrdinalIgnoreCase)
            ? "Roles"
            : "System";
    }

    private static string SummaryForPath(string path, string method)
    {
        return $"{method.ToUpperInvariant()} {path}";
    }

    private static JsonObject Operation(
        string path,
        string method,
        string tag,
        string summary,
        string responseRef,
        string? requestRef = null,
        string? failureStatus = null,
        bool security = false,
        OpenApiRuntimeEndpointMetadata? metadata = null)
    {
        var operation = new JsonObject
        {
            ["tags"] = new JsonArray(tag),
            ["summary"] = summary,
            ["responses"] = Responses(responseRef, failureStatus)
        };

        if (requestRef is not null)
        {
            var mediaType = (path == "/api/v1/system/files" || path == "/api/v1/account/avatar") && string.Equals(method, "post", StringComparison.OrdinalIgnoreCase)
                ? "multipart/form-data"
                : "application/json";

            operation["requestBody"] = new JsonObject
            {
                ["required"] = true,
                ["content"] = JsonContent(SchemaRef(requestRef), mediaType)
            };
        }

        if (security)
        {
            operation["security"] = new JsonArray
            {
                new JsonObject
                {
                    ["bearerAuth"] = new JsonArray()
                }
            };
        }

        if (metadata?.Module is not null)
        {
            operation[EndpointOpenApiExtensionNames.Module] = metadata.Module;
        }

        if (metadata?.Permission is not null)
        {
            operation[EndpointOpenApiExtensionNames.Permission] = metadata.Permission;
        }

        if (metadata?.Audit is not null)
        {
            operation[EndpointOpenApiExtensionNames.Audit] = new JsonObject
            {
                ["module"] = metadata.Audit.Module,
                ["resource"] = metadata.Audit.Resource,
                ["action"] = metadata.Audit.Action
            };
        }

        if (metadata?.RateLimitPolicy is not null)
        {
            operation[EndpointOpenApiExtensionNames.RateLimit] = metadata.RateLimitPolicy;
        }

        var queryParameters = QueryParameters(path, method);
        if (queryParameters is not null)
        {
            operation["parameters"] = queryParameters;
        }

        return operation;
    }

    private static JsonArray? QueryParameters(string path, string method)
    {
        if (!string.Equals(method, "get", StringComparison.Ordinal))
        {
            return null;
        }

        return path switch
        {
            "/api/v1/system/users" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/roles" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/positions" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/dict-types" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/settings" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("groupCode", "string")),
            "/api/v1/system/i18n/messages" => Parameters(("page", "integer"), ("pageSize", "integer"), ("locale", "string"), ("module", "string"), ("keyword", "string"), ("status", "string")),
            "/api/v1/i18n/messages" => Parameters(("locale", "string")),
            "/api/v1/system/login-logs" => Parameters(("page", "integer"), ("pageSize", "integer"), ("username", "string"), ("ip", "string"), ("result", "string"), ("from", "date-time"), ("to", "date-time")),
            "/api/v1/system/audit-logs" => Parameters(("page", "integer"), ("pageSize", "integer"), ("user", "string"), ("module", "string"), ("resource", "string"), ("action", "string"), ("result", "string"), ("from", "date-time"), ("to", "date-time")),
            "/api/v1/system/security-events" => Parameters(("page", "integer"), ("pageSize", "integer"), ("eventType", "string"), ("severity", "string"), ("user", "string"), ("ip", "string"), ("from", "date-time"), ("to", "date-time")),
            "/api/v1/system/security/bans" => Parameters(("page", "integer"), ("pageSize", "integer"), ("banType", "string"), ("target", "string"), ("severity", "string"), ("source", "string"), ("activeOnly", "boolean")),
            "/api/v1/system/files" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("mimeType", "string"), ("status", "string")),
            _ => null
        };
    }

    private static JsonArray Parameters(params (string Name, string Type)[] parameters)
    {
        var array = new JsonArray();
        foreach (var parameter in parameters)
        {
            array.Add(new JsonObject
            {
                ["name"] = parameter.Name,
                ["in"] = "query",
                ["required"] = false,
                ["schema"] = parameter.Type switch
                {
                    "integer" => new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    "date-time" => DateTimeSchema(),
                    "boolean" => BooleanSchema(),
                    _ => StringSchema()
                }
            });
        }

        return array;
    }

    private static JsonObject Responses(string responseRef, string? failureStatus)
    {
        var responses = new JsonObject
        {
            ["200"] = new JsonObject
            {
                ["description"] = "Success",
                ["content"] = JsonContent(ApiResultRef(responseRef))
            }
        };

        if (failureStatus is not null)
        {
            responses[failureStatus] = new JsonObject
            {
                ["description"] = "Error",
                ["content"] = JsonContent(ApiResultRef("Object"))
            };
        }

        return responses;
    }

}
