using System.Text.Json;
using System.Text.Json.Nodes;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;

namespace WeCms.Api.Extensions;

public static class OpenApiExtensions
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
        return new JsonObject
        {
            ["openapi"] = "3.1.0",
            ["info"] = new JsonObject
            {
                ["title"] = "WeCMS API",
                ["version"] = "v1"
            },
            ["paths"] = Paths(endpoints),
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

    private static readonly IReadOnlyList<OpenApiEndpointDescriptor> RegisteredDiscoveryEndpoints =
    [
        new OpenApiEndpointDescriptor("get", "/health/live", false, null, null, nameof(SystemLiveResponse)),
        new OpenApiEndpointDescriptor("get", "/health/ready", false, null, null, nameof(SystemReadyResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/db-check", false, null, null, nameof(SystemDbCheckResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/ping", false, null, null, nameof(SystemPingResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/version", false, null, null, nameof(SystemVersionResponse)),
        new OpenApiEndpointDescriptor(
            "get",
            "/api/v1/system/secure-ping",
            true,
            SystemPermissions.SecurePing,
            null,
            nameof(SecurePingResponse)),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/login",
            false,
            null,
            nameof(LoginRequest),
            nameof(LoginResponse)),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/refresh",
            false,
            null,
            nameof(RefreshTokenRequest),
            nameof(LoginResponse)),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/logout",
            true,
            null,
            nameof(LogoutRequest),
            "Object"),
        new OpenApiEndpointDescriptor(
            "get",
            "/api/v1/auth/me",
            true,
            null,
            null,
            nameof(AuthMeResponse)),
    ];

    private static JsonObject Paths(IEnumerable<OpenApiEndpointDescriptor> endpoints)
    {
        var paths = new JsonObject();
        foreach (var endpoint in endpoints)
        {
            if (paths[endpoint.Path] is not JsonObject pathObject)
            {
                pathObject = new JsonObject();
                paths[endpoint.Path] = pathObject;
            }

            pathObject[endpoint.Method] = Operation(
                tag: TagForPath(endpoint.Path),
                summary: SummaryForPath(endpoint.Path, endpoint.Method),
                responseRef: endpoint.ResponseType,
                requestRef: endpoint.RequestBodyType,
                security: endpoint.Security,
                permission: endpoint.Permission);
        }

        return paths;
    }


    private static string TagForPath(string path)
    {
        return path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
            ? "Auth"
            : "System";
    }

    private static string SummaryForPath(string path, string method)
    {
        return $"{method.ToUpperInvariant()} {path}";
    }

    private static JsonObject Operation(
        string tag,
        string summary,
        string responseRef,
        string? requestRef = null,
        string? failureStatus = null,
        bool security = false,
        string? permission = null)
    {
        var operation = new JsonObject
        {
            ["tags"] = new JsonArray(tag),
            ["summary"] = summary,
            ["responses"] = Responses(responseRef, failureStatus)
        };

        if (requestRef is not null)
        {
            operation["requestBody"] = new JsonObject
            {
                ["required"] = true,
                ["content"] = JsonContent(SchemaRef(requestRef))
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

        if (permission is not null)
        {
            operation["x-wecms-permission"] = permission;
        }

        return operation;
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

    private static JsonObject Components()
    {
        return new JsonObject
        {
            ["securitySchemes"] = new JsonObject
            {
                ["bearerAuth"] = new JsonObject
                {
                    ["type"] = "http",
                    ["scheme"] = "bearer"
                }
            },
            ["schemas"] = Schemas()
        };
    }

    private static JsonObject Schemas()
    {
        return new JsonObject
        {
            ["ApiResult"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("code", "msg", "data"),
                ["properties"] = new JsonObject
                {
                    ["code"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["msg"] = StringSchema(),
                    ["data"] = new JsonObject { ["nullable"] = true },
                    ["traceId"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                    ["fieldErrors"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["additionalProperties"] = ArrayOf(StringSchema())
                    }
                }
            },
            ["Object"] = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = true
            },
            ["LoginRequest"] = ObjectSchema(("username", "string"), ("password", "string")),
            ["RefreshTokenRequest"] = ObjectSchema(("refreshToken", "string")),
            ["LogoutRequest"] = ObjectSchema(("refreshToken", "string")),
            ["LoginResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("accessToken", "refreshToken", "expiresAt", "user", "roles", "permissions", "menus"),
                ["properties"] = new JsonObject
                {
                    ["accessToken"] = StringSchema(),
                    ["refreshToken"] = StringSchema(),
                    ["expiresAt"] = new JsonObject { ["type"] = "string", ["format"] = "date-time" },
                    ["user"] = SchemaRef("AuthUserDto"),
                    ["roles"] = ArrayOf(StringSchema()),
                    ["permissions"] = ArrayOf(StringSchema()),
                    ["menus"] = ArrayOf(SchemaRef("AuthMenuDto"))
                }
            },
            ["AuthMeResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("user", "roles", "permissions", "menus"),
                ["properties"] = new JsonObject
                {
                    ["user"] = SchemaRef("AuthUserDto"),
                    ["roles"] = ArrayOf(StringSchema()),
                    ["permissions"] = ArrayOf(StringSchema()),
                    ["menus"] = ArrayOf(SchemaRef("AuthMenuDto"))
                }
            },
            ["AuthUserDto"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "isSuperAdmin"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema()
                }
            },
            ["AuthMenuDto"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "type", "name", "path", "title"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["parentId"] = IntegerSchema(nullable: true),
                    ["type"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["path"] = StringSchema(),
                    ["title"] = StringSchema()
                }
            },
            ["SystemLiveResponse"] = ObjectSchema(("status", "string")),
            ["SystemReadyResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("status", "database"),
                ["properties"] = new JsonObject
                {
                    ["status"] = StringSchema(),
                    ["database"] = BooleanSchema()
                }
            },
            ["SystemPingResponse"] = ObjectSchema(("status", "string")),
            ["SystemVersionResponse"] = ObjectSchema(("version", "string")),
            ["SystemDbCheckResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("status", "database"),
                ["properties"] = new JsonObject
                {
                    ["status"] = StringSchema(),
                    ["database"] = BooleanSchema()
                }
            },
            ["SecurePingResponse"] = ObjectSchema(("status", "string"))
        };
    }

    private static JsonObject ObjectSchema(params (string Name, string Type)[] properties)
    {
        var schemaProperties = new JsonObject();
        foreach (var (name, type) in properties)
        {
            schemaProperties[name] = new JsonObject { ["type"] = type };
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required(properties.Select(property => property.Name).ToArray()),
            ["properties"] = schemaProperties
        };
    }

    private static JsonObject ApiResultRef(string dataRef)
    {
        return new JsonObject
        {
            ["allOf"] = new JsonArray
            {
                SchemaRef("ApiResult"),
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["data"] = SchemaRef(dataRef)
                    }
                }
            }
        };
    }

    private static JsonObject JsonContent(JsonObject schema)
    {
        return new JsonObject
        {
            ["application/json"] = new JsonObject
            {
                ["schema"] = schema
            }
        };
    }

    private static JsonObject SchemaRef(string name)
    {
        return new JsonObject
        {
            ["$ref"] = $"#/components/schemas/{name}"
        };
    }

    private static JsonObject StringSchema()
    {
        return new JsonObject { ["type"] = "string" };
    }

    private static JsonObject IntegerSchema(bool nullable = false)
    {
        return nullable
            ? new JsonObject { ["type"] = new JsonArray("integer", "null"), ["format"] = "int64" }
            : new JsonObject { ["type"] = "integer", ["format"] = "int64" };
    }

    private static JsonObject BooleanSchema()
    {
        return new JsonObject { ["type"] = "boolean" };
    }

    private static JsonObject ArrayOf(JsonObject itemSchema)
    {
        return new JsonObject
        {
            ["type"] = "array",
            ["items"] = itemSchema
        };
    }

    private static JsonArray Required(params string[] names)
    {
        var required = new JsonArray();
        foreach (var name in names)
        {
            required.Add(name);
        }

        return required;
    }

    private sealed record OpenApiEndpointDescriptor(
        string Method,
        string Path,
        bool Security,
        string? Permission,
        string? RequestBodyType,
        string ResponseType);
}
