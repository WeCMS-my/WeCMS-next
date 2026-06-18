#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
openapi_path="${1:-${repo_root}/artifacts/openapi/wecms-api-v1.json}"

python3 - "$repo_root" "$openapi_path" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
openapi_path = Path(sys.argv[2])
violations: list[str] = []

plan_path = repo / "docs/context/WeCMS_Next_一期后建议补齐清单详细开发修复计划书_v1.1_任务说明增强版.md"
scope_path = repo / "docs/context/一期范围调整说明.md"

for path, tokens in {
    plan_path: [
        "H3-009：旧 ThinkPHP 功能差异复核",
        "P1 差异清零",
        "CMS 能力仍保持二期边界",
    ],
    scope_path: [
        "CMS 模块整体移入二期",
        "一期不创建、不预留、不半实现 CMS 相关接口和表结构",
        "任何 CMS 表、接口、权限、菜单、页面都不得进入一期主线",
    ],
}.items():
    if not path.is_file():
        violations.append(f"missing scope document {path.relative_to(repo)}")
        continue
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            violations.append(f"{path.relative_to(repo)} missing token {token!r}")

backend_capability_files = {
    "auth login/refresh/logout/me": "backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs",
    "account profile/security": "backend/src/WeCms.Modules.System/Auth/AccountProfileEndpoints.cs",
    "account 2FA": "backend/src/WeCms.Modules.System/Auth/AccountTwoFactorEndpoints.cs",
    "users": "backend/src/WeCms.Modules.System/Users/UserEndpoints.cs",
    "roles": "backend/src/WeCms.Modules.System/Roles/RoleEndpoints.cs",
    "permissions": "backend/src/WeCms.Modules.System/Permissions/PermissionManagementEndpoints.cs",
    "menus": "backend/src/WeCms.Modules.System/Menus/MenuEndpoints.cs",
    "departments": "backend/src/WeCms.Modules.System/Departments/DepartmentEndpoints.cs",
    "posts": "backend/src/WeCms.Modules.System/Posts/PostEndpoints.cs",
    "dicts": "backend/src/WeCms.Modules.System/Dicts/DictEndpoints.cs",
    "settings": "backend/src/WeCms.Modules.System/Settings/SettingEndpoints.cs",
    "files": "backend/src/WeCms.Modules.System/Files/FileEndpoints.cs",
    "login logs": "backend/src/WeCms.Modules.System/Logs/LoginLogEndpoints.cs",
    "audit logs": "backend/src/WeCms.Modules.System/Logs/AuditLogEndpoints.cs",
    "security events": "backend/src/WeCms.Modules.System/Logs/SecurityEventEndpoints.cs",
    "security center": "backend/src/WeCms.Modules.System/Security/SecurityEndpoints.cs",
    "i18n": "backend/src/WeCms.Modules.System/I18n/I18nEndpoints.cs",
}

frontend_capability_files = {
    "login": "frontend/soybean-admin/src/views/LoginView.vue",
    "two factor login": "frontend/soybean-admin/src/views/auth/TwoFactorLoginView.vue",
    "account profile": "frontend/soybean-admin/src/views/account/AccountProfileView.vue",
    "account security": "frontend/soybean-admin/src/views/account/AccountSecurityView.vue",
    "users": "frontend/soybean-admin/src/views/system/users/UsersView.vue",
    "roles": "frontend/soybean-admin/src/views/system/roles/RolesView.vue",
    "permissions": "frontend/soybean-admin/src/views/system/permissions/PermissionsView.vue",
    "menus": "frontend/soybean-admin/src/views/system/menus/MenusView.vue",
    "departments": "frontend/soybean-admin/src/views/system/depts/DepartmentsView.vue",
    "posts": "frontend/soybean-admin/src/views/system/posts/PostsView.vue",
    "dicts": "frontend/soybean-admin/src/views/system/dicts/DictsView.vue",
    "settings": "frontend/soybean-admin/src/views/system/settings/SettingsView.vue",
    "files": "frontend/soybean-admin/src/views/system/files/FilesView.vue",
    "login logs": "frontend/soybean-admin/src/views/system/logs/LoginLogsView.vue",
    "audit logs": "frontend/soybean-admin/src/views/system/logs/AuditLogsView.vue",
    "security events": "frontend/soybean-admin/src/views/system/logs/SecurityEventsView.vue",
    "security center": "frontend/soybean-admin/src/views/system/security/SecurityCenterView.vue",
    "i18n": "frontend/soybean-admin/src/views/system/i18n/I18nMessagesView.vue",
}

for name, relative_path in {**backend_capability_files, **frontend_capability_files}.items():
    path = repo / relative_path
    if not path.is_file():
        violations.append(f"missing foundation capability {name}: {relative_path}")

if not openapi_path.is_file():
    violations.append(f"missing OpenAPI artifact {openapi_path}")
else:
    data = json.loads(openapi_path.read_text(encoding="utf-8"))
    paths = data.get("paths") or {}
    required_path_prefixes = [
        "/api/v1/auth/",
        "/api/v1/account",
        "/api/v1/system/users",
        "/api/v1/system/roles",
        "/api/v1/system/permissions",
        "/api/v1/system/menus",
        "/api/v1/system/depts",
        "/api/v1/system/posts",
        "/api/v1/system/dict",
        "/api/v1/system/settings",
        "/api/v1/system/files",
        "/api/v1/system/login-logs",
        "/api/v1/system/audit-logs",
        "/api/v1/system/security-events",
        "/api/v1/system/security/bans",
        "/api/v1/system/i18n",
    ]
    for prefix in required_path_prefixes:
        if not any(path.startswith(prefix) for path in paths):
            violations.append(f"OpenAPI missing foundation path prefix {prefix}")

    forbidden_path_prefixes = ["/api/v1/cms", "/api/v1/content", "/api/v1/articles", "/api/v1/pages", "/api/v1/tags"]
    for path in paths:
        if any(path.startswith(prefix) for prefix in forbidden_path_prefixes):
            violations.append(f"OpenAPI contains CMS phase-two path {path}")

for root in ["database/migrations", "database/seeds", "backend/src", "frontend/soybean-admin/src"]:
    root_path = repo / root
    if not root_path.exists():
        continue
    for path in root_path.rglob("*"):
        if not path.is_file() or path.suffix.lower() not in {".sql", ".cs", ".ts", ".tsx", ".vue", ".js", ".mjs"}:
            continue
        text = path.read_text(encoding="utf-8").lower()
        if "think_" in text or "thinkphp" in text:
            violations.append(f"{path.relative_to(repo)} contains legacy ThinkPHP runtime/migration reference")
        if path.suffix.lower() == ".sql" and "cms_" in text:
            violations.append(f"{path.relative_to(repo)} contains CMS phase-two table reference")

if violations:
    raise SystemExit("check-thinkphp-feature-delta: " + "; ".join(violations))

print("check-thinkphp-feature-delta: ok (17 backend and 18 frontend foundation capabilities checked)")
PY
