using WeCms.Modules.Identity.Services;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Audit.Logs;
using AuditLogPermissions = WeCms.Modules.Audit.Logs.LogPermissions;
using SecurityEventPermissions = WeCms.Modules.Security.Events.SecurityEventPermissions;
using WeCms.Modules.Security.Events;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.Platform.Permissions;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Modules.Security;
using WeCms.Modules.Configuration.Settings;
using WeCms.Modules.Identity.Permissions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed partial class OpenApiExportTests
{
    [Fact]
    public void SystemAuthAndAccountEndpointMetadata_IsCoveredForAuthorizationAndPermission()
    {
        var registered = CollectRegisteredEndpointMetadata();
        var endpoints = registered
            .Where(endpoint => IsSystemAuthOrAccountEndpoint(endpoint.Path))
            .ToDictionary(endpoint => $"{endpoint.Path}|{endpoint.Method}", endpoint => endpoint);

        var expected = new Dictionary<(string Path, string Method), (bool RequiresAuthorization, string? Permission)>
        {
            { ("/health/live", "get"), (false, null) },
            { ("/health/ready", "get"), (false, null) },
            { ("/health/dependencies", "get"), (true, "sys:system:secure-ping") },
            { ("/api/v1/system/ping", "get"), (false, null) },
            { ("/api/v1/system/version", "get"), (false, null) },
            { ("/api/v1/system/db-check", "get"), (false, null) },
            { ("/api/v1/system/secure-ping", "get"), (true, "sys:system:secure-ping") },
            { ("/api/v1/auth/login", "post"), (false, null) },
            { ("/api/v1/auth/refresh", "post"), (false, null) },
            { ("/api/v1/auth/logout", "post"), (false, null) },
            { ("/api/v1/auth/2fa/verify", "post"), (false, null) },
            { ("/api/v1/auth/2fa/recovery-code", "post"), (false, null) },
            { ("/api/v1/auth/me", "get"), (true, null) },
            { ("/api/v1/account/2fa/status", "get"), (true, null) },
            { ("/api/v1/account/2fa/setup", "post"), (true, null) },
            { ("/api/v1/account/2fa/confirm", "post"), (true, null) },
            { ("/api/v1/account/2fa/disable", "post"), (true, null) },
            { ("/api/v1/account/2fa/recovery-codes/regenerate", "post"), (true, null) },
            { ("/api/v1/account/profile", "get"), (true, null) },
            { ("/api/v1/account/profile", "put"), (true, null) },
            { ("/api/v1/account/password", "put"), (true, null) },
            { ("/api/v1/account/avatar", "post"), (true, null) },
            { ("/api/v1/account/avatar/content", "get"), (true, null) },
            { ("/api/v1/account/security", "get"), (true, null) },
            { ("/api/v1/system/users", "get"), (true, IdentityUserPermissions.List) },
            { ("/api/v1/system/users/{id:long}", "get"), (true, IdentityUserPermissions.Detail) },
            { ("/api/v1/system/users", "post"), (true, IdentityUserPermissions.Create) },
            { ("/api/v1/system/users/{id:long}", "put"), (true, IdentityUserPermissions.Update) },
            { ("/api/v1/system/users/{id:long}", "delete"), (true, IdentityUserPermissions.Delete) },
            { ("/api/v1/system/users/{id:long}/enable", "post"), (true, IdentityUserPermissions.Enable) },
            { ("/api/v1/system/users/{id:long}/disable", "post"), (true, IdentityUserPermissions.Disable) },
            { ("/api/v1/system/users/{id:long}/reset-password", "post"), (true, IdentityUserPermissions.ResetPassword) },
            { ("/api/v1/system/users/{id:long}/reset-2fa", "post"), (true, IdentityUserPermissions.ResetTwoFactor) },
            { ("/api/v1/system/users/{id:long}/roles", "put"), (true, IdentityUserPermissions.AssignRole) },
            { ("/api/v1/system/users/{id:long}/positions", "put"), (true, IdentityUserPermissions.AssignPosition) },
            { ("/api/v1/system/roles", "get"), (true, RolePermissions.List) },
            { ("/api/v1/system/roles/{id:long}", "get"), (true, RolePermissions.Detail) },
            { ("/api/v1/system/roles", "post"), (true, RolePermissions.Create) },
            { ("/api/v1/system/roles/{id:long}", "put"), (true, RolePermissions.Update) },
            { ("/api/v1/system/roles/{id:long}", "delete"), (true, RolePermissions.Delete) },
            { ("/api/v1/system/roles/{id:long}/enable", "post"), (true, RolePermissions.Enable) },
            { ("/api/v1/system/roles/{id:long}/disable", "post"), (true, RolePermissions.Disable) },
            { ("/api/v1/system/roles/{id:long}/permissions", "put"), (true, RolePermissions.AssignPermission) },
            { ("/api/v1/system/roles/{id:long}/menus", "put"), (true, RolePermissions.AssignMenu) },
            { ("/api/v1/system/menus", "get"), (true, MenuPermissions.List) },
            { ("/api/v1/system/menus/tree", "get"), (true, MenuPermissions.Tree) },
            { ("/api/v1/system/menus/{id:long}", "get"), (true, MenuPermissions.Detail) },
            { ("/api/v1/system/menus", "post"), (true, MenuPermissions.Create) },
            { ("/api/v1/system/menus/{id:long}", "put"), (true, MenuPermissions.Update) },
            { ("/api/v1/system/menus/sort", "put"), (true, MenuPermissions.Sort) },
            { ("/api/v1/system/menus/{id:long}", "delete"), (true, MenuPermissions.Delete) },
            { ("/api/v1/system/menus/{id:long}/enable", "post"), (true, MenuPermissions.Enable) },
            { ("/api/v1/system/menus/{id:long}/disable", "post"), (true, MenuPermissions.Disable) },
            { ("/api/v1/system/permissions", "get"), (true, PermissionManagementPermissions.List) },
            { ("/api/v1/system/permissions/tree", "get"), (true, PermissionManagementPermissions.Tree) },
            { ("/api/v1/system/permissions/{id:long}", "get"), (true, PermissionManagementPermissions.Detail) },
            { ("/api/v1/system/permissions", "post"), (true, PermissionManagementPermissions.Create) },
            { ("/api/v1/system/permissions/{id:long}", "put"), (true, PermissionManagementPermissions.Update) },
            { ("/api/v1/system/permissions/{id:long}", "delete"), (true, PermissionManagementPermissions.Delete) },
            { ("/api/v1/system/permissions/{id:long}/enable", "post"), (true, PermissionManagementPermissions.Enable) },
            { ("/api/v1/system/permissions/{id:long}/disable", "post"), (true, PermissionManagementPermissions.Disable) },
            { ("/api/v1/system/depts", "get"), (true, DepartmentPermissions.List) },
            { ("/api/v1/system/depts/tree", "get"), (true, DepartmentPermissions.Tree) },
            { ("/api/v1/system/depts/{id:long}", "get"), (true, DepartmentPermissions.Detail) },
            { ("/api/v1/system/depts", "post"), (true, DepartmentPermissions.Create) },
            { ("/api/v1/system/depts/{id:long}", "put"), (true, DepartmentPermissions.Update) },
            { ("/api/v1/system/depts/{id:long}", "delete"), (true, DepartmentPermissions.Delete) },
            { ("/api/v1/system/depts/{id:long}/enable", "post"), (true, DepartmentPermissions.Enable) },
            { ("/api/v1/system/depts/{id:long}/disable", "post"), (true, DepartmentPermissions.Disable) },
            { ("/api/v1/system/positions", "get"), (true, PositionPermissions.List) },
            { ("/api/v1/system/positions/{id:long}", "get"), (true, PositionPermissions.Detail) },
            { ("/api/v1/system/positions", "post"), (true, PositionPermissions.Create) },
            { ("/api/v1/system/positions/{id:long}", "put"), (true, PositionPermissions.Update) },
            { ("/api/v1/system/positions/{id:long}", "delete"), (true, PositionPermissions.Delete) },
            { ("/api/v1/system/positions/{id:long}/enable", "post"), (true, PositionPermissions.Enable) },
            { ("/api/v1/system/positions/{id:long}/disable", "post"), (true, PositionPermissions.Disable) },
            { ("/api/v1/system/dict-types", "get"), (true, DictPermissions.TypeList) },
            { ("/api/v1/system/dict-types/{id:long}", "get"), (true, DictPermissions.TypeList) },
            { ("/api/v1/system/dict-types", "post"), (true, DictPermissions.TypeCreate) },
            { ("/api/v1/system/dict-types/{id:long}", "put"), (true, DictPermissions.TypeUpdate) },
            { ("/api/v1/system/dict-types/{id:long}", "delete"), (true, DictPermissions.TypeDelete) },
            { ("/api/v1/system/dict-types/{id:long}/enable", "post"), (true, DictPermissions.TypeEnable) },
            { ("/api/v1/system/dict-types/{id:long}/disable", "post"), (true, DictPermissions.TypeDisable) },
            { ("/api/v1/system/dict-types/{typeCode}/values", "get"), (true, DictPermissions.ValueList) },
            { ("/api/v1/system/dict-types/{typeCode}/values", "post"), (true, DictPermissions.ValueCreate) },
            { ("/api/v1/system/dict-values/{id:long}", "put"), (true, DictPermissions.ValueUpdate) },
            { ("/api/v1/system/dict-values/{id:long}", "delete"), (true, DictPermissions.ValueDelete) },
            { ("/api/v1/system/dict-values/{id:long}/enable", "post"), (true, DictPermissions.ValueEnable) },
            { ("/api/v1/system/dict-values/{id:long}/disable", "post"), (true, DictPermissions.ValueDisable) },
            { ("/api/v1/system/settings", "get"), (true, SettingPermissions.List) },
            { ("/api/v1/system/settings/{key}", "get"), (true, SettingPermissions.Detail) },
            { ("/api/v1/system/settings/{key}", "put"), (true, SettingPermissions.Update) },
            { ("/api/v1/system/settings/validate-ip-rules", "post"), (true, SettingPermissions.ValidateIpRules) },
            { ("/api/v1/system/settings/reload-cache", "post"), (true, SettingPermissions.ReloadCache) },
            { ("/api/v1/system/i18n/messages", "get"), (true, I18nPermissions.List) },
            { ("/api/v1/system/i18n/messages/{id:long}", "get"), (true, I18nPermissions.Detail) },
            { ("/api/v1/system/i18n/messages", "post"), (true, I18nPermissions.Create) },
            { ("/api/v1/system/i18n/messages/{id:long}", "put"), (true, I18nPermissions.Update) },
            { ("/api/v1/system/i18n/messages/{id:long}", "delete"), (true, I18nPermissions.Delete) },
            { ("/api/v1/i18n/messages", "get"), (false, null) },
            { ("/api/v1/account/i18n/switch", "post"), (true, I18nPermissions.AccountSwitch) },
            { ("/api/v1/system/login-logs", "get"), (true, AuditLogPermissions.LoginLogList) },
            { ("/api/v1/system/login-logs/{id:long}", "get"), (true, AuditLogPermissions.LoginLogDetail) },
            { ("/api/v1/system/audit-logs", "get"), (true, AuditLogPermissions.AuditLogList) },
            { ("/api/v1/system/audit-logs/{id:long}", "get"), (true, AuditLogPermissions.AuditLogDetail) },
            { ("/api/v1/system/security/status", "get"), (true, SecurityPermissions.Status) },
            { ("/api/v1/system/security/bans", "get"), (true, SecurityPermissions.BanList) },
            { ("/api/v1/system/security/bans/{id:long}", "get"), (true, SecurityPermissions.BanDetail) },
            { ("/api/v1/system/security/bans/{id:long}/unban", "post"), (true, SecurityPermissions.BanUnban) },
            { ("/api/v1/system/security/bans/batch-unban", "post"), (true, SecurityPermissions.BanBatchUnban) },
            { ("/api/v1/system/security-events", "get"), (true, SecurityEventPermissions.SecurityEventList) },
            { ("/api/v1/system/security-events/{id:long}", "get"), (true, SecurityEventPermissions.SecurityEventDetail) },
            { ("/api/v1/system/files", "get"), (true, FilePermissions.List) },
            { ("/api/v1/system/files/{id:long}", "get"), (true, FilePermissions.Detail) },
            { ("/api/v1/system/files", "post"), (true, FilePermissions.Upload) },
            { ("/api/v1/system/files/{id:long}/download", "get"), (true, FilePermissions.Download) },
            { ("/api/v1/system/files/{id:long}/preview", "get"), (true, FilePermissions.Download) },
            { ("/api/v1/system/files/{id:long}", "delete"), (true, FilePermissions.Delete) }
        };

        foreach (var expectedEndpoint in expected)
        {
            Assert.True(
                endpoints.TryGetValue(BuildEndpointKey(expectedEndpoint.Key.Path, expectedEndpoint.Key.Method), out var actual),
                $"Endpoint {expectedEndpoint.Key.Method.ToUpperInvariant()} {expectedEndpoint.Key.Path} is missing.");
            Assert.Equal(expectedEndpoint.Value.RequiresAuthorization, actual.RequiresAuthorization);
            Assert.Equal(expectedEndpoint.Value.Permission, actual.Permission);
        }

        var unexpected = endpoints
            .Where(endpoint => !expected.ContainsKey(ParseEndpointKey(endpoint.Key)))
            .Select(endpoint =>
            {
                var endpointKey = ParseEndpointKey(endpoint.Key);
                return $"{endpointKey.Method.ToUpperInvariant()} {endpointKey.Path}";
            })
            .ToArray();

        Assert.True(unexpected.Length == 0, $"Unexpected endpoint metadata was found: {string.Join(", ", unexpected)}");
    }

    private static bool IsSystemAuthOrAccountEndpoint(string path)
    {
        return path is "/health/live" or "/health/ready" or "/health/dependencies"
            || path.StartsWith("/api/v1/system/", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/account/", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/i18n/", StringComparison.Ordinal);
    }

    private static string BuildEndpointKey(string path, string method)
    {
        return $"{path}|{method}";
    }

    private static (string Path, string Method) ParseEndpointKey(string key)
    {
        var separator = key.IndexOf('|');
        return separator < 0
            ? (key, string.Empty)
            : (key[..separator], key[(separator + 1)..]);
    }

    private static readonly HashSet<RegisteredEndpoint> RegisteredEndpointMetadata =
    [
        new RegisteredEndpoint("/health/live", "get", null, false, null),
        new RegisteredEndpoint("/health/ready", "get", null, false, null),
        new RegisteredEndpoint("/health/dependencies", "get", PlatformPermissions.SecurePing, true, null),
        new RegisteredEndpoint("/api/v1/system/ping", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/version", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/db-check", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/secure-ping", "get", PlatformPermissions.SecurePing, true, null),
        new RegisteredEndpoint("/api/v1/auth/login", "post", null, false, nameof(LoginRequest)),
        new RegisteredEndpoint("/api/v1/auth/refresh", "post", null, false, null),
        new RegisteredEndpoint("/api/v1/auth/logout", "post", null, false, null),
        new RegisteredEndpoint("/api/v1/auth/2fa/verify", "post", null, false, nameof(TwoFactorVerifyRequest)),
        new RegisteredEndpoint("/api/v1/auth/2fa/recovery-code", "post", null, false, nameof(TwoFactorRecoveryCodeRequest)),
        new RegisteredEndpoint("/api/v1/auth/me", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/account/2fa/status", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/account/2fa/setup", "post", null, true, null),
        new RegisteredEndpoint("/api/v1/account/2fa/confirm", "post", null, true, nameof(AccountTwoFactorConfirmRequest)),
        new RegisteredEndpoint("/api/v1/account/2fa/disable", "post", null, true, nameof(AccountTwoFactorDisableRequest)),
        new RegisteredEndpoint("/api/v1/account/2fa/recovery-codes/regenerate", "post", null, true, nameof(AccountTwoFactorRegenerateRecoveryCodesRequest)),
        new RegisteredEndpoint("/api/v1/account/profile", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/account/profile", "put", null, true, nameof(UpdateAccountProfileRequest)),
        new RegisteredEndpoint("/api/v1/account/password", "put", null, true, nameof(ChangeAccountPasswordRequest)),
        new RegisteredEndpoint("/api/v1/account/avatar", "post", null, true, nameof(AccountAvatarUploadRequest)),
        new RegisteredEndpoint("/api/v1/account/avatar/content", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/account/security", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/system/users", "get", IdentityUserPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "get", IdentityUserPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/users", "post", IdentityUserPermissions.Create, true, nameof(CreateUserRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "put", IdentityUserPermissions.Update, true, nameof(UpdateUserRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "delete", IdentityUserPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/enable", "post", IdentityUserPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/disable", "post", IdentityUserPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/reset-password", "post", IdentityUserPermissions.ResetPassword, true, nameof(ResetUserPasswordRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/reset-2fa", "post", IdentityUserPermissions.ResetTwoFactor, true, nameof(ResetUserTwoFactorRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/roles", "put", IdentityUserPermissions.AssignRole, true, nameof(AssignUserRolesRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/positions", "put", IdentityUserPermissions.AssignPosition, true, nameof(AssignUserPositionsRequest)),
        new RegisteredEndpoint("/api/v1/system/roles", "get", RolePermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "get", RolePermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/roles", "post", RolePermissions.Create, true, nameof(CreateRoleRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "put", RolePermissions.Update, true, nameof(UpdateRoleRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "delete", RolePermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/enable", "post", RolePermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/disable", "post", RolePermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/permissions", "put", RolePermissions.AssignPermission, true, nameof(AssignRolePermissionsRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/menus", "put", RolePermissions.AssignMenu, true, nameof(AssignRoleMenusRequest)),
        new RegisteredEndpoint("/api/v1/system/menus", "get", MenuPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/tree", "get", MenuPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "get", MenuPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/menus", "post", MenuPermissions.Create, true, nameof(CreateMenuRequest)),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "put", MenuPermissions.Update, true, nameof(UpdateMenuRequest)),
        new RegisteredEndpoint("/api/v1/system/menus/sort", "put", MenuPermissions.Sort, true, nameof(SortMenusRequest)),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "delete", MenuPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}/enable", "post", MenuPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}/disable", "post", MenuPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions", "get", PermissionManagementPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/tree", "get", PermissionManagementPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "get", PermissionManagementPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions", "post", PermissionManagementPermissions.Create, true, nameof(CreatePermissionRequest)),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "put", PermissionManagementPermissions.Update, true, nameof(UpdatePermissionRequest)),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "delete", PermissionManagementPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}/enable", "post", PermissionManagementPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}/disable", "post", PermissionManagementPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/depts", "get", DepartmentPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/tree", "get", DepartmentPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "get", DepartmentPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/depts", "post", DepartmentPermissions.Create, true, nameof(CreateDepartmentRequest)),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "put", DepartmentPermissions.Update, true, nameof(UpdateDepartmentRequest)),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "delete", DepartmentPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}/enable", "post", DepartmentPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}/disable", "post", DepartmentPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/positions", "get", PositionPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/positions/{id:long}", "get", PositionPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/positions", "post", PositionPermissions.Create, true, nameof(CreatePositionRequest)),
        new RegisteredEndpoint("/api/v1/system/positions/{id:long}", "put", PositionPermissions.Update, true, nameof(UpdatePositionRequest)),
        new RegisteredEndpoint("/api/v1/system/positions/{id:long}", "delete", PositionPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/positions/{id:long}/enable", "post", PositionPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/positions/{id:long}/disable", "post", PositionPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types", "get", DictPermissions.TypeList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "get", DictPermissions.TypeList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types", "post", DictPermissions.TypeCreate, true, nameof(CreateDictTypeRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "put", DictPermissions.TypeUpdate, true, nameof(UpdateDictTypeRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "delete", DictPermissions.TypeDelete, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}/enable", "post", DictPermissions.TypeEnable, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}/disable", "post", DictPermissions.TypeDisable, true, nameof(DisableDictTypeRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-types/{typeCode}/values", "get", DictPermissions.ValueList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{typeCode}/values", "post", DictPermissions.ValueCreate, true, nameof(CreateDictValueRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}", "put", DictPermissions.ValueUpdate, true, nameof(UpdateDictValueRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}", "delete", DictPermissions.ValueDelete, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}/enable", "post", DictPermissions.ValueEnable, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}/disable", "post", DictPermissions.ValueDisable, true, null),
        new RegisteredEndpoint("/api/v1/system/settings", "get", SettingPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/settings/{key}", "get", SettingPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/settings/{key}", "put", SettingPermissions.Update, true, nameof(UpdateSettingRequest)),
        new RegisteredEndpoint("/api/v1/system/settings/validate-ip-rules", "post", SettingPermissions.ValidateIpRules, true, nameof(ValidateIpRulesRequest)),
        new RegisteredEndpoint("/api/v1/system/settings/reload-cache", "post", SettingPermissions.ReloadCache, true, null),
        new RegisteredEndpoint("/api/v1/system/i18n/messages", "get", I18nPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/i18n/messages/{id:long}", "get", I18nPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/i18n/messages", "post", I18nPermissions.Create, true, nameof(CreateI18nMessageRequest)),
        new RegisteredEndpoint("/api/v1/system/i18n/messages/{id:long}", "put", I18nPermissions.Update, true, nameof(UpdateI18nMessageRequest)),
        new RegisteredEndpoint("/api/v1/system/i18n/messages/{id:long}", "delete", I18nPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/i18n/messages", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/account/i18n/switch", "post", I18nPermissions.AccountSwitch, true, nameof(SwitchAccountLocaleRequest)),
        new RegisteredEndpoint("/api/v1/system/login-logs", "get", AuditLogPermissions.LoginLogList, true, null),
        new RegisteredEndpoint("/api/v1/system/login-logs/{id:long}", "get", AuditLogPermissions.LoginLogDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/audit-logs", "get", AuditLogPermissions.AuditLogList, true, null),
        new RegisteredEndpoint("/api/v1/system/audit-logs/{id:long}", "get", AuditLogPermissions.AuditLogDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/security/status", "get", SecurityPermissions.Status, true, null),
        new RegisteredEndpoint("/api/v1/system/security/bans", "get", SecurityPermissions.BanList, true, null),
        new RegisteredEndpoint("/api/v1/system/security/bans/{id:long}", "get", SecurityPermissions.BanDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/security/bans/{id:long}/unban", "post", SecurityPermissions.BanUnban, true, nameof(UnbanSecurityBanRequest)),
        new RegisteredEndpoint("/api/v1/system/security/bans/batch-unban", "post", SecurityPermissions.BanBatchUnban, true, nameof(BatchUnbanSecurityBansRequest)),
        new RegisteredEndpoint("/api/v1/system/security-events", "get", SecurityEventPermissions.SecurityEventList, true, null),
        new RegisteredEndpoint("/api/v1/system/security-events/{id:long}", "get", SecurityEventPermissions.SecurityEventDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/files", "get", FilePermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}", "get", FilePermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/files", "post", FilePermissions.Upload, true, nameof(CreateFileRequest)),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}/download", "get", FilePermissions.Download, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}/preview", "get", FilePermissions.Download, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}", "delete", FilePermissions.Delete, true, null)
    ];
}
