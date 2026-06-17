using System.Text.Json;
using System.Text.Json.Nodes;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.System;
using WeCms.Modules.System.Users;

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
            false,
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
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users", true, UserPermissions.List, null, "PagedUserSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users/{id:long}", true, UserPermissions.Detail, null, nameof(UserDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users", true, UserPermissions.Create, nameof(CreateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}", true, UserPermissions.Update, nameof(UpdateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/users/{id:long}", true, UserPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/enable", true, UserPermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/disable", true, UserPermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/reset-password", true, UserPermissions.ResetPassword, nameof(ResetUserPasswordRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/roles", true, UserPermissions.AssignRole, nameof(AssignUserRolesRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/posts", true, UserPermissions.AssignPost, nameof(AssignUserPostsRequest), "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/roles", true, RolePermissions.List, null, "PagedRoleSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/roles/{id:long}", true, RolePermissions.Detail, null, nameof(RoleDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles", true, RolePermissions.Create, nameof(CreateRoleRequest), nameof(RoleMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}", true, RolePermissions.Update, nameof(UpdateRoleRequest), nameof(RoleMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/roles/{id:long}", true, RolePermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles/{id:long}/enable", true, RolePermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles/{id:long}/disable", true, RolePermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}/permissions", true, RolePermissions.AssignPermission, nameof(AssignRolePermissionsRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}/menus", true, RolePermissions.AssignMenu, nameof(AssignRoleMenusRequest), "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus", true, MenuPermissions.List, null, "MenuSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus/tree", true, MenuPermissions.Tree, null, "MenuTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Detail, null, nameof(MenuDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus", true, MenuPermissions.Create, nameof(CreateMenuRequest), nameof(MenuMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Update, nameof(UpdateMenuRequest), nameof(MenuMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus/{id:long}/enable", true, MenuPermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus/{id:long}/disable", true, MenuPermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions", true, PermissionManagementPermissions.List, null, "PermissionSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions/tree", true, PermissionManagementPermissions.Tree, null, "PermissionTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Detail, null, nameof(PermissionDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions", true, PermissionManagementPermissions.Create, nameof(CreatePermissionRequest), nameof(PermissionMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Update, nameof(UpdatePermissionRequest), nameof(PermissionMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions/{id:long}/enable", true, PermissionManagementPermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions/{id:long}/disable", true, PermissionManagementPermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts", true, DepartmentPermissions.List, null, "DepartmentSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts/tree", true, DepartmentPermissions.Tree, null, "DepartmentTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Detail, null, nameof(DepartmentDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts", true, DepartmentPermissions.Create, nameof(CreateDepartmentRequest), nameof(DepartmentMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Update, nameof(UpdateDepartmentRequest), nameof(DepartmentMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts/{id:long}/enable", true, DepartmentPermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts/{id:long}/disable", true, DepartmentPermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/posts", true, PostPermissions.List, null, "PagedPostSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/posts/{id:long}", true, PostPermissions.Detail, null, nameof(PostDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts", true, PostPermissions.Create, nameof(CreatePostRequest), nameof(PostMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/posts/{id:long}", true, PostPermissions.Update, nameof(UpdatePostRequest), nameof(PostMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/posts/{id:long}", true, PostPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts/{id:long}/enable", true, PostPermissions.Enable, "Object", "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts/{id:long}/disable", true, PostPermissions.Disable, "Object", "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types", true, DictPermissions.TypeList, null, "PagedDictTypeSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeList, null, nameof(DictTypeDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types", true, DictPermissions.TypeCreate, nameof(CreateDictTypeRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeUpdate, nameof(UpdateDictTypeRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeDelete, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types/{typeCode}/values", true, DictPermissions.ValueList, null, "DictValueList"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types/{typeCode}/values", true, DictPermissions.ValueCreate, nameof(CreateDictValueRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/dict-values/{id:long}", true, DictPermissions.ValueUpdate, nameof(UpdateDictValueRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/dict-values/{id:long}", true, DictPermissions.ValueDelete, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/settings", true, SettingPermissions.List, null, "PagedSettingSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/settings/{key}", true, SettingPermissions.Detail, null, nameof(SettingDetailDto)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/settings/{key}", true, SettingPermissions.Update, nameof(UpdateSettingRequest), nameof(SettingMutationResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/login-logs", true, LogPermissions.LoginLogList, null, "PagedLoginLogSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/login-logs/{id:long}", true, LogPermissions.LoginLogDetail, null, nameof(LoginLogDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/audit-logs", true, LogPermissions.AuditLogList, null, "PagedAuditLogSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/audit-logs/{id:long}", true, LogPermissions.AuditLogDetail, null, nameof(AuditLogDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security-events", true, LogPermissions.SecurityEventList, null, "PagedSecurityEventSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security-events/{id:long}", true, LogPermissions.SecurityEventDetail, null, nameof(SecurityEventDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files", true, FilePermissions.List, null, "PagedFileSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}", true, FilePermissions.Detail, null, nameof(FileDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}/download", true, FilePermissions.Download, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}/preview", true, FilePermissions.Download, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/files", true, FilePermissions.Upload, nameof(CreateFileRequest), nameof(FileMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/files/{id:long}", true, FilePermissions.Delete, null, "Object"),
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
                path: endpoint.Path,
                method: endpoint.Method,
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

        if (path.StartsWith("/api/v1/system/posts", StringComparison.OrdinalIgnoreCase))
        {
            return "Posts";
        }

        if (path.StartsWith("/api/v1/system/dict", StringComparison.OrdinalIgnoreCase))
        {
            return "Dicts";
        }

        if (path.StartsWith("/api/v1/system/settings", StringComparison.OrdinalIgnoreCase))
        {
            return "Settings";
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
            var mediaType = path == "/api/v1/system/files" && string.Equals(method, "post", StringComparison.OrdinalIgnoreCase)
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

        if (permission is not null)
        {
            operation["x-wecms-permission"] = permission;
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
            "/api/v1/system/posts" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/dict-types" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("status", "string")),
            "/api/v1/system/settings" => Parameters(("page", "integer"), ("pageSize", "integer"), ("keyword", "string"), ("groupCode", "string")),
            "/api/v1/system/login-logs" => Parameters(("page", "integer"), ("pageSize", "integer"), ("username", "string"), ("ip", "string"), ("result", "string"), ("from", "date-time"), ("to", "date-time")),
            "/api/v1/system/audit-logs" => Parameters(("page", "integer"), ("pageSize", "integer"), ("user", "string"), ("module", "string"), ("resource", "string"), ("action", "string"), ("result", "string"), ("from", "date-time"), ("to", "date-time")),
            "/api/v1/system/security-events" => Parameters(("page", "integer"), ("pageSize", "integer"), ("eventType", "string"), ("severity", "string"), ("user", "string"), ("ip", "string"), ("from", "date-time"), ("to", "date-time")),
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
            ,
            ["PagedUserSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(UserSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(UserSummaryDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "status", "isSuperAdmin", "createdAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true),
                    ["status"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema(),
                    ["lastLoginAt"] = DateTimeSchema(nullable: true),
                    ["createdAt"] = DateTimeSchema()
                }
            },
            [nameof(UserDetailDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "status", "isSuperAdmin", "permissionVersion", "roleIds", "postIds", "createdAt", "updatedAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true),
                    ["status"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema(),
                    ["permissionVersion"] = IntegerSchema(),
                    ["lastLoginAt"] = DateTimeSchema(nullable: true),
                    ["roleIds"] = ArrayOf(IntegerSchema()),
                    ["postIds"] = ArrayOf(IntegerSchema()),
                    ["createdAt"] = DateTimeSchema(),
                    ["updatedAt"] = DateTimeSchema()
                }
            },
            [nameof(CreateUserRequest)] = ObjectSchema(("username", "string"), ("displayName", "string"), ("password", "string")),
            [nameof(UpdateUserRequest)] = ObjectSchema(("displayName", "string")),
            [nameof(ResetUserPasswordRequest)] = ObjectSchema(("password", "string")),
            [nameof(AssignUserRolesRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("roleIds"),
                ["properties"] = new JsonObject { ["roleIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(AssignUserPostsRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("postIds"),
                ["properties"] = new JsonObject { ["postIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(UserMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["PagedRoleSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(RoleSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(RoleSummaryDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "code", "name", "status", "isBuiltin", "isLocked", "createdAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["code"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["status"] = StringSchema(),
                    ["isBuiltin"] = BooleanSchema(),
                    ["isLocked"] = BooleanSchema(),
                    ["createdAt"] = DateTimeSchema()
                }
            },
            [nameof(RoleDetailDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "code", "name", "status", "isBuiltin", "isLocked", "permissionIds", "menuIds", "createdAt", "updatedAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["code"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["status"] = StringSchema(),
                    ["isBuiltin"] = BooleanSchema(),
                    ["isLocked"] = BooleanSchema(),
                    ["permissionIds"] = ArrayOf(IntegerSchema()),
                    ["menuIds"] = ArrayOf(IntegerSchema()),
                    ["createdAt"] = DateTimeSchema(),
                    ["updatedAt"] = DateTimeSchema()
                }
            },
            [nameof(CreateRoleRequest)] = ObjectSchema(("code", "string"), ("name", "string")),
            [nameof(UpdateRoleRequest)] = ObjectSchema(("name", "string")),
            [nameof(AssignRolePermissionsRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("permissionIds"),
                ["properties"] = new JsonObject { ["permissionIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(AssignRoleMenusRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("menuIds"),
                ["properties"] = new JsonObject { ["menuIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(RoleMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["MenuSummaryList"] = ArrayOf(SchemaRef(nameof(MenuSummaryDto))),
            ["MenuTreeList"] = ArrayOf(SchemaRef(nameof(MenuTreeDto))),
            [nameof(MenuSummaryDto)] = MenuSchema(includeChildren: false, includeTimestamps: false),
            [nameof(MenuTreeDto)] = MenuSchema(includeChildren: true, includeTimestamps: false),
            [nameof(MenuDetailDto)] = MenuSchema(includeChildren: false, includeTimestamps: true),
            [nameof(CreateMenuRequest)] = ObjectSchema(("type", "string"), ("code", "string"), ("path", "string"), ("title", "string"), ("status", "string")),
            [nameof(UpdateMenuRequest)] = ObjectSchema(("type", "string"), ("path", "string"), ("title", "string"), ("status", "string")),
            [nameof(MenuMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["PermissionSummaryList"] = ArrayOf(SchemaRef(nameof(PermissionSummaryDto))),
            ["PermissionTreeList"] = ArrayOf(SchemaRef(nameof(PermissionTreeDto))),
            [nameof(PermissionSummaryDto)] = PermissionSchema(includeTimestamps: false),
            [nameof(PermissionTreeDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("module", "permissions"),
                ["properties"] = new JsonObject
                {
                    ["module"] = StringSchema(),
                    ["permissions"] = ArrayOf(SchemaRef(nameof(PermissionSummaryDto)))
                }
            },
            [nameof(PermissionDetailDto)] = PermissionSchema(includeTimestamps: true),
            [nameof(CreatePermissionRequest)] = ObjectSchema(("code", "string"), ("name", "string"), ("module", "string")),
            [nameof(UpdatePermissionRequest)] = ObjectSchema(("name", "string"), ("module", "string")),
            [nameof(PermissionMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["DepartmentSummaryList"] = ArrayOf(SchemaRef(nameof(DepartmentSummaryDto))),
            ["DepartmentTreeList"] = ArrayOf(SchemaRef(nameof(DepartmentTreeDto))),
            [nameof(DepartmentSummaryDto)] = DepartmentSchema(includeChildren: false, includeTimestamps: false),
            [nameof(DepartmentTreeDto)] = DepartmentSchema(includeChildren: true, includeTimestamps: false),
            [nameof(DepartmentDetailDto)] = DepartmentSchema(includeChildren: false, includeTimestamps: true),
            [nameof(CreateDepartmentRequest)] = ObjectSchema(("code", "string"), ("name", "string"), ("status", "string")),
            [nameof(UpdateDepartmentRequest)] = ObjectSchema(("name", "string"), ("status", "string")),
            [nameof(DepartmentMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["PagedPostSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(PostSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(PostSummaryDto)] = PostSchema(includeTimestamps: false),
            [nameof(PostDetailDto)] = PostSchema(includeTimestamps: true),
            [nameof(CreatePostRequest)] = ObjectSchema(("code", "string"), ("name", "string"), ("status", "string")),
            [nameof(UpdatePostRequest)] = ObjectSchema(("name", "string"), ("status", "string")),
            [nameof(PostMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["PagedDictTypeSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(DictTypeSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(DictTypeSummaryDto)] = DictTypeSchema(includeTimestamps: false),
            [nameof(DictTypeDetailDto)] = DictTypeSchema(includeTimestamps: true),
            [nameof(CreateDictTypeRequest)] = ObjectSchema(("code", "string"), ("name", "string"), ("status", "string")),
            [nameof(UpdateDictTypeRequest)] = ObjectSchema(("name", "string"), ("status", "string")),
            ["DictValueList"] = ArrayOf(SchemaRef(nameof(DictValueDto))),
            [nameof(DictValueDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "typeId", "typeCode", "label", "value", "sortOrder", "isDefault", "status"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["typeId"] = IntegerSchema(),
                    ["typeCode"] = StringSchema(),
                    ["label"] = StringSchema(),
                    ["value"] = StringSchema(),
                    ["description"] = NullableStringSchema(),
                    ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["isDefault"] = BooleanSchema(),
                    ["status"] = StringSchema()
                }
            },
            [nameof(CreateDictValueRequest)] = ObjectSchema(("label", "string"), ("value", "string"), ("status", "string")),
            [nameof(UpdateDictValueRequest)] = ObjectSchema(("label", "string"), ("value", "string"), ("status", "string")),
            [nameof(DictMutationResponse)] = ObjectSchema(("id", "integer"))
            ,
            ["PagedSettingSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(SettingSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(SettingSummaryDto)] = SettingSchema(),
            [nameof(SettingDetailDto)] = SettingSchema(),
            [nameof(UpdateSettingRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["value"] = NullableStringSchema()
                }
            },
            [nameof(SettingMutationResponse)] = ObjectSchema(("key", "string"))
            ,
            ["PagedLoginLogSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(LoginLogSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(LoginLogSummaryDto)] = LoginLogSchema(includeUserAgent: false),
            [nameof(LoginLogDetailDto)] = LoginLogSchema(includeUserAgent: true)
            ,
            ["PagedAuditLogSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(AuditLogSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(AuditLogSummaryDto)] = AuditLogSchema(includeRequest: false),
            [nameof(AuditLogDetailDto)] = AuditLogSchema(includeRequest: true)
            ,
            ["PagedSecurityEventSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(SecurityEventSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(SecurityEventSummaryDto)] = SecurityEventSchema(),
            [nameof(SecurityEventDetailDto)] = SecurityEventSchema()
            ,
            ["PagedFileSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(FileSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(FileSummaryDto)] = FileSchema(),
            [nameof(FileDetailDto)] = FileSchema(),
            [nameof(CreateFileRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("originalName", "mimeType", "sizeBytes", "sha256", "file"),
                ["properties"] = new JsonObject
                {
                    ["originalName"] = StringSchema(),
                    ["mimeType"] = StringSchema(),
                    ["sizeBytes"] = IntegerSchema(),
                    ["sha256"] = StringSchema(),
                    ["file"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["format"] = "binary"
                    }
                }
            },
            [nameof(FileMutationResponse)] = ObjectSchema(("id", "integer"))
        };
    }

    private static JsonObject FileSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "originalName", "fileExt", "mimeType", "sizeBytes", "sha256", "status", "createdBy", "createdAt"),
            ["properties"] = new JsonObject
            {
                ["id"] = IntegerSchema(),
                ["originalName"] = StringSchema(),
                ["fileExt"] = StringSchema(),
                ["mimeType"] = StringSchema(),
                ["sizeBytes"] = IntegerSchema(),
                ["sha256"] = StringSchema(),
                ["status"] = StringSchema(),
                ["createdBy"] = IntegerSchema(),
                ["createdAt"] = DateTimeSchema()
            }
        };
    }

    private static JsonObject SecurityEventSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "eventType", "severity", "message", "createdAt"),
            ["properties"] = new JsonObject
            {
                ["id"] = IntegerSchema(),
                ["eventType"] = StringSchema(),
                ["userId"] = IntegerSchema(nullable: true),
                ["username"] = NullableStringSchema(),
                ["ip"] = NullableStringSchema(),
                ["severity"] = StringSchema(),
                ["message"] = StringSchema(),
                ["createdAt"] = DateTimeSchema()
            }
        };
    }

    private static JsonObject AuditLogSchema(bool includeRequest)
    {
        var required = includeRequest
            ? Required("id", "module", "resource", "action", "requestMethod", "requestPath", "result", "detail", "createdAt")
            : Required("id", "module", "resource", "action", "result", "createdAt");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["userId"] = IntegerSchema(nullable: true),
            ["username"] = NullableStringSchema(),
            ["module"] = StringSchema(),
            ["resource"] = StringSchema(),
            ["action"] = StringSchema(),
            ["targetId"] = NullableStringSchema(),
            ["result"] = StringSchema(),
            ["createdAt"] = DateTimeSchema()
        };
        if (includeRequest)
        {
            properties["requestMethod"] = StringSchema();
            properties["requestPath"] = StringSchema();
            properties["ipAddress"] = NullableStringSchema();
            properties["userAgent"] = NullableStringSchema();
            properties["traceId"] = NullableStringSchema();
            properties["detail"] = StringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject LoginLogSchema(bool includeUserAgent)
    {
        var required = includeUserAgent
            ? Required("id", "username", "result", "createdAt")
            : Required("id", "username", "result", "createdAt");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["username"] = StringSchema(),
            ["userId"] = IntegerSchema(nullable: true),
            ["ip"] = NullableStringSchema(),
            ["result"] = StringSchema(),
            ["reason"] = NullableStringSchema(),
            ["createdAt"] = DateTimeSchema()
        };
        if (includeUserAgent)
        {
            properties["userAgent"] = NullableStringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject SettingSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("key", "valueType", "groupCode", "name", "isSensitive", "isSystem", "updatedAt"),
            ["properties"] = new JsonObject
            {
                ["key"] = StringSchema(),
                ["value"] = NullableStringSchema(),
                ["valueType"] = StringSchema(),
                ["groupCode"] = StringSchema(),
                ["name"] = StringSchema(),
                ["description"] = NullableStringSchema(),
                ["isSensitive"] = BooleanSchema(),
                ["isSystem"] = BooleanSchema(),
                ["updatedAt"] = DateTimeSchema(),
                ["updatedBy"] = IntegerSchema(nullable: true)
            }
        };
    }

    private static JsonObject DictTypeSchema(bool includeTimestamps)
    {
        var required = includeTimestamps
            ? Required("id", "code", "name", "isSystem", "status", "sortOrder", "createdAt", "updatedAt")
            : Required("id", "code", "name", "isSystem", "status", "sortOrder", "createdAt");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["code"] = StringSchema(),
            ["name"] = StringSchema(),
            ["description"] = NullableStringSchema(),
            ["isSystem"] = BooleanSchema(),
            ["status"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["createdAt"] = DateTimeSchema()
        };
        if (includeTimestamps)
        {
            properties["updatedAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject PostSchema(bool includeTimestamps)
    {
        var required = includeTimestamps
            ? Required("id", "code", "name", "sortOrder", "status", "createdAt", "updatedAt")
            : Required("id", "code", "name", "sortOrder", "status", "createdAt");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["code"] = StringSchema(),
            ["name"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["status"] = StringSchema(),
            ["createdAt"] = DateTimeSchema()
        };
        if (includeTimestamps)
        {
            properties["updatedAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject DepartmentSchema(bool includeChildren, bool includeTimestamps)
    {
        var required = includeTimestamps
            ? Required("id", "code", "name", "sortOrder", "status", "createdAt", "updatedAt")
            : Required("id", "code", "name", "sortOrder", "status");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["parentId"] = IntegerSchema(nullable: true),
            ["code"] = StringSchema(),
            ["name"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["status"] = StringSchema()
        };
        if (includeChildren)
        {
            properties["children"] = ArrayOf(SchemaRef(nameof(DepartmentTreeDto)));
        }

        if (includeTimestamps)
        {
            properties["createdAt"] = DateTimeSchema();
            properties["updatedAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject PermissionSchema(bool includeTimestamps)
    {
        var required = includeTimestamps
            ? Required("id", "code", "name", "module", "status", "isBuiltin", "isRoleBound", "createdAt", "updatedAt")
            : Required("id", "code", "name", "module", "status", "isBuiltin", "isRoleBound");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["code"] = StringSchema(),
            ["name"] = StringSchema(),
            ["module"] = StringSchema(),
            ["description"] = NullableStringSchema(),
            ["status"] = StringSchema(),
            ["isBuiltin"] = BooleanSchema(),
            ["isRoleBound"] = BooleanSchema()
        };
        if (includeTimestamps)
        {
            properties["createdAt"] = DateTimeSchema();
            properties["updatedAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject MenuSchema(bool includeChildren, bool includeTimestamps)
    {
        var required = includeTimestamps
            ? Required("id", "type", "code", "path", "title", "sort", "hidden", "keepAlive", "status", "isBuiltin", "createdAt", "updatedAt")
            : Required("id", "type", "code", "path", "title", "sort", "hidden", "keepAlive", "status", "isBuiltin");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["parentId"] = IntegerSchema(nullable: true),
            ["type"] = StringSchema(),
            ["code"] = StringSchema(),
            ["path"] = StringSchema(),
            ["component"] = NullableStringSchema(),
            ["title"] = StringSchema(),
            ["i18nKey"] = NullableStringSchema(),
            ["icon"] = NullableStringSchema(),
            ["sort"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["hidden"] = BooleanSchema(),
            ["keepAlive"] = BooleanSchema(),
            ["externalUrl"] = NullableStringSchema(),
            ["permissionCode"] = NullableStringSchema(),
            ["status"] = StringSchema(),
            ["isBuiltin"] = BooleanSchema()
        };
        if (includeChildren)
        {
            properties["children"] = ArrayOf(SchemaRef(nameof(MenuTreeDto)));
        }

        if (includeTimestamps)
        {
            properties["createdAt"] = DateTimeSchema();
            properties["updatedAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = required,
            ["properties"] = properties
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

    private static JsonObject JsonContent(JsonObject schema, string mediaType = "application/json")
    {
        return new JsonObject
        {
            [mediaType] = new JsonObject
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

    private static JsonObject NullableStringSchema()
    {
        return new JsonObject { ["type"] = new JsonArray("string", "null") };
    }

    private static JsonObject DateTimeSchema(bool nullable = false)
    {
        return nullable
            ? new JsonObject { ["type"] = new JsonArray("string", "null"), ["format"] = "date-time" }
            : new JsonObject { ["type"] = "string", ["format"] = "date-time" };
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
