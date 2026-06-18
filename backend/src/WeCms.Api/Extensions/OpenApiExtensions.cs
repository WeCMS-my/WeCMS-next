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
using WeCms.Modules.System.Security;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.System;
using WeCms.Modules.System.Users;

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
            null,
            nameof(LoginResponse)),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/logout",
            false,
            null,
            null,
            "Object"),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/2fa/verify",
            false,
            null,
            nameof(TwoFactorVerifyRequest),
            nameof(LoginResponse)),
        new OpenApiEndpointDescriptor(
            "post",
            "/api/v1/auth/2fa/recovery-code",
            false,
            null,
            nameof(TwoFactorRecoveryCodeRequest),
            nameof(LoginResponse)),
        new OpenApiEndpointDescriptor(
            "get",
            "/api/v1/auth/me",
            true,
            null,
            null,
            nameof(AuthMeResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/account/2fa/status", true, null, null, nameof(AccountTwoFactorStatusResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/2fa/setup", true, null, null, nameof(AccountTwoFactorSetupResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/2fa/confirm", true, null, nameof(AccountTwoFactorConfirmRequest), nameof(AccountTwoFactorStatusResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/2fa/disable", true, null, nameof(AccountTwoFactorDisableRequest), "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/2fa/recovery-codes/regenerate", true, null, nameof(AccountTwoFactorRegenerateRecoveryCodesRequest), nameof(AccountTwoFactorRecoveryCodesResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/account/profile", true, null, null, nameof(AccountProfileResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/account/profile", true, null, nameof(UpdateAccountProfileRequest), nameof(AccountProfileResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/account/password", true, null, nameof(ChangeAccountPasswordRequest), "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/avatar", true, null, nameof(AccountAvatarUploadRequest), nameof(AccountAvatarResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/account/avatar/content", true, null, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/account/security", true, null, null, nameof(AccountSecurityResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users", true, UserPermissions.List, null, "PagedUserSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users/{id:long}", true, UserPermissions.Detail, null, nameof(UserDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users", true, UserPermissions.Create, nameof(CreateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}", true, UserPermissions.Update, nameof(UpdateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/users/{id:long}", true, UserPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/enable", true, UserPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/disable", true, UserPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/reset-password", true, UserPermissions.ResetPassword, nameof(ResetUserPasswordRequest), "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/reset-2fa", true, UserPermissions.ResetTwoFactor, nameof(ResetUserTwoFactorRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/roles", true, UserPermissions.AssignRole, nameof(AssignUserRolesRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/posts", true, UserPermissions.AssignPost, nameof(AssignUserPostsRequest), "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/roles", true, RolePermissions.List, null, "PagedRoleSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/roles/{id:long}", true, RolePermissions.Detail, null, nameof(RoleDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles", true, RolePermissions.Create, nameof(CreateRoleRequest), nameof(RoleMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}", true, RolePermissions.Update, nameof(UpdateRoleRequest), nameof(RoleMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/roles/{id:long}", true, RolePermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles/{id:long}/enable", true, RolePermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/roles/{id:long}/disable", true, RolePermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}/permissions", true, RolePermissions.AssignPermission, nameof(AssignRolePermissionsRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/roles/{id:long}/menus", true, RolePermissions.AssignMenu, nameof(AssignRoleMenusRequest), "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus", true, MenuPermissions.List, null, "MenuSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus/tree", true, MenuPermissions.Tree, null, "MenuTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Detail, null, nameof(MenuDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus", true, MenuPermissions.Create, nameof(CreateMenuRequest), nameof(MenuMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Update, nameof(UpdateMenuRequest), nameof(MenuMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/menus/{id:long}", true, MenuPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus/{id:long}/enable", true, MenuPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/menus/{id:long}/disable", true, MenuPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions", true, PermissionManagementPermissions.List, null, "PermissionSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions/tree", true, PermissionManagementPermissions.Tree, null, "PermissionTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Detail, null, nameof(PermissionDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions", true, PermissionManagementPermissions.Create, nameof(CreatePermissionRequest), nameof(PermissionMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Update, nameof(UpdatePermissionRequest), nameof(PermissionMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/permissions/{id:long}", true, PermissionManagementPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions/{id:long}/enable", true, PermissionManagementPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/permissions/{id:long}/disable", true, PermissionManagementPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts", true, DepartmentPermissions.List, null, "DepartmentSummaryList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts/tree", true, DepartmentPermissions.Tree, null, "DepartmentTreeList"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Detail, null, nameof(DepartmentDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts", true, DepartmentPermissions.Create, nameof(CreateDepartmentRequest), nameof(DepartmentMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Update, nameof(UpdateDepartmentRequest), nameof(DepartmentMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/depts/{id:long}", true, DepartmentPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts/{id:long}/enable", true, DepartmentPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/depts/{id:long}/disable", true, DepartmentPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/posts", true, PostPermissions.List, null, "PagedPostSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/posts/{id:long}", true, PostPermissions.Detail, null, nameof(PostDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts", true, PostPermissions.Create, nameof(CreatePostRequest), nameof(PostMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/posts/{id:long}", true, PostPermissions.Update, nameof(UpdatePostRequest), nameof(PostMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/posts/{id:long}", true, PostPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts/{id:long}/enable", true, PostPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/posts/{id:long}/disable", true, PostPermissions.Disable, null, "Object"),
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
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/status", true, SecurityPermissions.Status, null, nameof(SecurityStatusDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/bans", true, SecurityPermissions.BanList, null, "PagedSecurityBanSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/bans/{id:long}", true, SecurityPermissions.BanDetail, null, nameof(SecurityBanDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/security/bans/{id:long}/unban", true, SecurityPermissions.BanUnban, nameof(UnbanSecurityBanRequest), nameof(SecurityBanMutationResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/security/bans/batch-unban", true, SecurityPermissions.BanBatchUnban, nameof(BatchUnbanSecurityBansRequest), nameof(BatchUnbanSecurityBansResponse)),
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

    private sealed record OpenApiEndpointDescriptor(
        string Method,
        string Path,
        bool Security,
        string? Permission,
        string? RequestBodyType,
        string ResponseType);
}
