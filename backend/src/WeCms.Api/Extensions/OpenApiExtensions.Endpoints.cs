using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.Identity.Contracts;
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
using WeCms.Modules.Platform.System;
using WeCms.Modules.Identity.Permissions;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static readonly IReadOnlyList<OpenApiEndpointDescriptor> RegisteredDiscoveryEndpoints =
    [
        new OpenApiEndpointDescriptor("get", "/health/live", false, null, null, nameof(SystemLiveResponse)),
        new OpenApiEndpointDescriptor("get", "/health/ready", false, null, null, nameof(SystemReadyResponse)),
        new OpenApiEndpointDescriptor("get", "/health/dependencies", true, PlatformPermissions.SecurePing, null, nameof(SystemDependenciesResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/db-check", false, null, null, nameof(SystemDbCheckResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/ping", false, null, null, nameof(SystemPingResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/version", false, null, null, nameof(SystemVersionResponse)),
        new OpenApiEndpointDescriptor(
            "get",
            "/api/v1/system/secure-ping",
            true,
            PlatformPermissions.SecurePing,
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
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users", true, IdentityUserPermissions.List, null, "PagedUserSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/users/{id:long}", true, IdentityUserPermissions.Detail, null, nameof(UserDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users", true, IdentityUserPermissions.Create, nameof(CreateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}", true, IdentityUserPermissions.Update, nameof(UpdateUserRequest), nameof(UserMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/users/{id:long}", true, IdentityUserPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/enable", true, IdentityUserPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/disable", true, IdentityUserPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/reset-password", true, IdentityUserPermissions.ResetPassword, nameof(ResetUserPasswordRequest), "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/users/{id:long}/reset-2fa", true, IdentityUserPermissions.ResetTwoFactor, nameof(ResetUserTwoFactorRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/roles", true, IdentityUserPermissions.AssignRole, nameof(AssignUserRolesRequest), "Object"),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/users/{id:long}/positions", true, IdentityUserPermissions.AssignPosition, nameof(AssignUserPositionsRequest), "Object"),
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
        new OpenApiEndpointDescriptor("put", "/api/v1/system/menus/sort", true, MenuPermissions.Sort, nameof(SortMenusRequest), "Object"),
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
        new OpenApiEndpointDescriptor("get", "/api/v1/system/positions", true, PositionPermissions.List, null, "PagedPositionSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/positions/{id:long}", true, PositionPermissions.Detail, null, nameof(PositionDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/positions", true, PositionPermissions.Create, nameof(CreatePositionRequest), nameof(PositionMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/positions/{id:long}", true, PositionPermissions.Update, nameof(UpdatePositionRequest), nameof(PositionMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/positions/{id:long}", true, PositionPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/positions/{id:long}/enable", true, PositionPermissions.Enable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/positions/{id:long}/disable", true, PositionPermissions.Disable, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types", true, DictPermissions.TypeList, null, "PagedDictTypeSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeList, null, nameof(DictTypeDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types", true, DictPermissions.TypeCreate, nameof(CreateDictTypeRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeUpdate, nameof(UpdateDictTypeRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/dict-types/{id:long}", true, DictPermissions.TypeDelete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types/{id:long}/enable", true, DictPermissions.TypeEnable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types/{id:long}/disable", true, DictPermissions.TypeDisable, nameof(DisableDictTypeRequest), "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/dict-types/{typeCode}/values", true, DictPermissions.ValueList, null, "DictValueList"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-types/{typeCode}/values", true, DictPermissions.ValueCreate, nameof(CreateDictValueRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/dict-values/{id:long}", true, DictPermissions.ValueUpdate, nameof(UpdateDictValueRequest), nameof(DictMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/dict-values/{id:long}", true, DictPermissions.ValueDelete, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-values/{id:long}/enable", true, DictPermissions.ValueEnable, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/dict-values/{id:long}/disable", true, DictPermissions.ValueDisable, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/settings", true, SettingPermissions.List, null, "PagedSettingSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/settings/{key}", true, SettingPermissions.Detail, null, nameof(SettingDetailDto)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/settings/{key}", true, SettingPermissions.Update, nameof(UpdateSettingRequest), nameof(SettingMutationResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/settings/validate-ip-rules", true, SettingPermissions.ValidateIpRules, nameof(ValidateIpRulesRequest), nameof(ValidateIpRulesResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/settings/reload-cache", true, SettingPermissions.ReloadCache, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/i18n/messages", true, I18nPermissions.List, null, "PagedI18nMessageSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/i18n/messages/{id:long}", true, I18nPermissions.Detail, null, nameof(I18nMessageDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/i18n/messages", true, I18nPermissions.Create, nameof(CreateI18nMessageRequest), nameof(I18nMutationResponse)),
        new OpenApiEndpointDescriptor("put", "/api/v1/system/i18n/messages/{id:long}", true, I18nPermissions.Update, nameof(UpdateI18nMessageRequest), nameof(I18nMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/i18n/messages/{id:long}", true, I18nPermissions.Delete, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/i18n/messages", false, null, null, nameof(I18nMessagesResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/account/i18n/switch", true, I18nPermissions.AccountSwitch, nameof(SwitchAccountLocaleRequest), nameof(AccountI18nSwitchResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/login-logs", true, AuditLogPermissions.LoginLogList, null, "PagedLoginLogSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/login-logs/{id:long}", true, AuditLogPermissions.LoginLogDetail, null, nameof(LoginLogDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/audit-logs", true, AuditLogPermissions.AuditLogList, null, "PagedAuditLogSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/audit-logs/{id:long}", true, AuditLogPermissions.AuditLogDetail, null, nameof(AuditLogDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/status", true, SecurityPermissions.Status, null, nameof(SecurityStatusDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/bans", true, SecurityPermissions.BanList, null, "PagedSecurityBanSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security/bans/{id:long}", true, SecurityPermissions.BanDetail, null, nameof(SecurityBanDetailDto)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/security/bans/{id:long}/unban", true, SecurityPermissions.BanUnban, nameof(UnbanSecurityBanRequest), nameof(SecurityBanMutationResponse)),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/security/bans/batch-unban", true, SecurityPermissions.BanBatchUnban, nameof(BatchUnbanSecurityBansRequest), nameof(BatchUnbanSecurityBansResponse)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security-events", true, SecurityEventPermissions.SecurityEventList, null, "PagedSecurityEventSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/security-events/{id:long}", true, SecurityEventPermissions.SecurityEventDetail, null, nameof(SecurityEventDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files", true, FilePermissions.List, null, "PagedFileSummary"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}", true, FilePermissions.Detail, null, nameof(FileDetailDto)),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}/download", true, FilePermissions.Download, null, "Object"),
        new OpenApiEndpointDescriptor("get", "/api/v1/system/files/{id:long}/preview", true, FilePermissions.Download, null, "Object"),
        new OpenApiEndpointDescriptor("post", "/api/v1/system/files", true, FilePermissions.Upload, nameof(CreateFileRequest), nameof(FileMutationResponse)),
        new OpenApiEndpointDescriptor("delete", "/api/v1/system/files/{id:long}", true, FilePermissions.Delete, null, "Object"),
    ];

    private sealed record OpenApiEndpointDescriptor(
        string Method,
        string Path,
        bool Security,
        string? Permission,
        string? RequestBodyType,
        string ResponseType);
}
