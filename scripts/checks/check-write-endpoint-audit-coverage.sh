#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
openapi_path="${1:-$repo_root/artifacts/openapi/wecms-api-v1.json}"

python3 - "$repo_root" "$openapi_path" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
openapi_path = Path(sys.argv[2])
if not openapi_path.is_file():
    raise SystemExit(f"check-write-endpoint-audit-coverage: missing {openapi_path}")

document = json.loads(openapi_path.read_text(encoding="utf-8"))
paths = document.get("paths", {})
write_methods = {"post", "put", "patch", "delete"}

coverage = {
    ("post", "/api/v1/auth/login"): ("backend/src/WeCms.Modules.System/Auth/AuthService.cs", "_auditWriter.RecordAsync(", None),
    ("post", "/api/v1/auth/refresh"): ("backend/src/WeCms.Modules.System/Auth/RefreshTokenRotationService.cs", "RefreshAuditAction", None),
    ("post", "/api/v1/auth/logout"): ("backend/src/WeCms.Modules.System/Auth/LogoutTokenRevoker.cs", "LogoutAuditAction", None),
    ("post", "/api/v1/auth/2fa/verify"): ("backend/src/WeCms.Modules.System/Auth/AuthTwoFactorChallengeService.cs", "TwoFactorVerifyAuditAction", "auth.two_factor_failed"),
    ("post", "/api/v1/auth/2fa/recovery-code"): ("backend/src/WeCms.Modules.System/Auth/AuthTwoFactorChallengeService.cs", "TwoFactorRecoveryCodeAuditAction", "auth.two_factor_failed"),
    ("post", "/api/v1/account/2fa/setup"): ("backend/src/WeCms.Modules.System/Auth/AccountTwoFactorService.cs", "account-2fa-setup", "auth.account_2fa_setup_started"),
    ("post", "/api/v1/account/2fa/confirm"): ("backend/src/WeCms.Modules.System/Auth/AccountTwoFactorService.cs", "account-2fa-confirm", "auth.account_2fa_enabled"),
    ("post", "/api/v1/account/2fa/disable"): ("backend/src/WeCms.Modules.System/Auth/AccountTwoFactorService.cs", "account-2fa-disable", "auth.account_2fa_disabled"),
    ("post", "/api/v1/account/2fa/recovery-codes/regenerate"): ("backend/src/WeCms.Modules.System/Auth/AccountTwoFactorService.cs", "account-2fa-recovery-codes-regenerate", "auth.account_2fa_recovery_codes_regenerated"),
    ("put", "/api/v1/account/profile"): ("backend/src/WeCms.Modules.System/Auth/AccountProfileService.cs", "profile-update", "auth.account_profile_updated"),
    ("put", "/api/v1/account/password"): ("backend/src/WeCms.Modules.System/Auth/AccountProfileService.cs", "password-change", "auth.account_password_changed"),
    ("post", "/api/v1/account/avatar"): ("backend/src/WeCms.Modules.System/Auth/AccountProfileService.cs", "avatar-update", "auth.account_avatar_updated"),
    ("post", "/api/v1/system/users"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/users/{id:long}"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/users/{id:long}"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/users/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/users/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"disable\"", None),
    ("post", "/api/v1/system/users/{id:long}/reset-password"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"reset-password\"", None),
    ("post", "/api/v1/system/users/{id:long}/reset-2fa"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"reset-2fa\"", "auth.user_2fa_reset"),
    ("put", "/api/v1/system/users/{id:long}/roles"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"assign-role\"", None),
    ("put", "/api/v1/system/users/{id:long}/posts"): ("backend/src/WeCms.Modules.System/Users/UserService.cs", "AuditAsync(context, \"assign-post\"", None),
    ("post", "/api/v1/system/roles"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/roles/{id:long}"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/roles/{id:long}"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/roles/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/roles/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"disable\"", None),
    ("put", "/api/v1/system/roles/{id:long}/permissions"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"assign-permission\"", None),
    ("put", "/api/v1/system/roles/{id:long}/menus"): ("backend/src/WeCms.Modules.System/Roles/RoleService.cs", "AuditAsync(context, \"assign-menu\"", None),
    ("post", "/api/v1/system/menus"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/menus/{id:long}"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"update\"", None),
    ("put", "/api/v1/system/menus/sort"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"sort\"", None),
    ("delete", "/api/v1/system/menus/{id:long}"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/menus/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/menus/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Menus/MenuService.cs", "AuditAsync(context, \"disable\"", None),
    ("post", "/api/v1/system/permissions"): ("backend/src/WeCms.Modules.System/Permissions/PermissionManagementService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/permissions/{id:long}"): ("backend/src/WeCms.Modules.System/Permissions/PermissionManagementService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/permissions/{id:long}"): ("backend/src/WeCms.Modules.System/Permissions/PermissionManagementService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/permissions/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Permissions/PermissionManagementService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/permissions/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Permissions/PermissionManagementService.cs", "AuditAsync(context, \"disable\"", None),
    ("post", "/api/v1/system/depts"): ("backend/src/WeCms.Modules.System/Departments/DepartmentService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/depts/{id:long}"): ("backend/src/WeCms.Modules.System/Departments/DepartmentService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/depts/{id:long}"): ("backend/src/WeCms.Modules.System/Departments/DepartmentService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/depts/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Departments/DepartmentService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/depts/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Departments/DepartmentService.cs", "AuditAsync(context, \"disable\"", None),
    ("post", "/api/v1/system/posts"): ("backend/src/WeCms.Modules.System/Posts/PostService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/posts/{id:long}"): ("backend/src/WeCms.Modules.System/Posts/PostService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/posts/{id:long}"): ("backend/src/WeCms.Modules.System/Posts/PostService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/system/posts/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Posts/PostService.cs", "AuditAsync(context, \"enable\"", None),
    ("post", "/api/v1/system/posts/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Posts/PostService.cs", "AuditAsync(context, \"disable\"", None),
    ("post", "/api/v1/system/dict-types"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"create-type\"", None),
    ("put", "/api/v1/system/dict-types/{id:long}"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"update-type\"", None),
    ("delete", "/api/v1/system/dict-types/{id:long}"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"delete-type\"", None),
    ("post", "/api/v1/system/dict-types/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, action", None),
    ("post", "/api/v1/system/dict-types/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, action", None),
    ("post", "/api/v1/system/dict-types/{typeCode}/values"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"create-value\"", None),
    ("put", "/api/v1/system/dict-values/{id:long}"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"update-value\"", None),
    ("delete", "/api/v1/system/dict-values/{id:long}"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, \"delete-value\"", None),
    ("post", "/api/v1/system/dict-values/{id:long}/enable"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, action", None),
    ("post", "/api/v1/system/dict-values/{id:long}/disable"): ("backend/src/WeCms.Modules.System/Dicts/DictService.cs", "AuditAsync(context, action", None),
    ("put", "/api/v1/system/settings/{key}"): ("backend/src/WeCms.Modules.System/Settings/SettingService.cs", "RecordAuditAsync(new SettingAuditRecord", None),
    ("post", "/api/v1/system/settings/validate-ip-rules"): ("backend/src/WeCms.Modules.System/Settings/SettingService.cs", "validate-ip-rules", None),
    ("post", "/api/v1/system/settings/reload-cache"): ("backend/src/WeCms.Modules.System/Settings/SettingService.cs", "reload-cache", None),
    ("post", "/api/v1/system/i18n/messages"): ("backend/src/WeCms.Modules.System/I18n/I18nMessageService.cs", "AuditAsync(context, \"create\"", None),
    ("put", "/api/v1/system/i18n/messages/{id:long}"): ("backend/src/WeCms.Modules.System/I18n/I18nMessageService.cs", "AuditAsync(context, \"update\"", None),
    ("delete", "/api/v1/system/i18n/messages/{id:long}"): ("backend/src/WeCms.Modules.System/I18n/I18nMessageService.cs", "AuditAsync(context, \"delete\"", None),
    ("post", "/api/v1/account/i18n/switch"): ("backend/src/WeCms.Modules.System/I18n/I18nMessageService.cs", "AuditAsync(context, \"switch-locale\"", None),
    ("post", "/api/v1/system/security/bans/{id:long}/unban"): ("backend/src/WeCms.Modules.System/Security/SecurityBanService.cs", "RecordAuditAsync(", "RecordSecurityEventAsync("),
    ("post", "/api/v1/system/security/bans/batch-unban"): ("backend/src/WeCms.Modules.System/Security/SecurityBanService.cs", "RecordAuditAsync(", "RecordSecurityEventAsync("),
    ("post", "/api/v1/system/files"): ("backend/src/WeCms.Modules.System/Files/FileService.cs", "AuditAsync(context, \"upload\"", None),
    ("delete", "/api/v1/system/files/{id:long}"): ("backend/src/WeCms.Modules.System/Files/FileService.cs", "AuditAsync(context, \"delete\"", None),
}

actual = {
    (method.lower(), route)
    for route, operations in paths.items()
    if isinstance(operations, dict)
    for method in operations
    if method.lower() in write_methods
}

missing = sorted(actual - set(coverage))
stale = sorted(set(coverage) - actual)
violations: list[str] = []

if missing:
    violations.extend(f"{method.upper()} {route} missing audit coverage entry" for method, route in missing)
if stale:
    violations.extend(f"{method.upper()} {route} has stale audit coverage entry" for method, route in stale)

for (method, route), (relative_source, audit_token, security_event_token) in sorted(coverage.items()):
    source_path = repo / relative_source
    if not source_path.is_file():
        violations.append(f"{method.upper()} {route} audit source missing: {relative_source}")
        continue

    source = source_path.read_text(encoding="utf-8")
    if audit_token not in source:
        violations.append(f"{method.upper()} {route} audit token missing in {relative_source}: {audit_token}")
    if security_event_token is not None and security_event_token not in source:
        violations.append(f"{method.upper()} {route} security event token missing in {relative_source}: {security_event_token}")

if violations:
    raise SystemExit("check-write-endpoint-audit-coverage: " + "; ".join(violations))

print(f"check-write-endpoint-audit-coverage: ok ({len(actual)} write endpoints checked)")
PY
