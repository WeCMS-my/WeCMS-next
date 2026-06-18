#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
FRONTEND_SRC_DIR="${ROOT_DIR}/frontend/soybean-admin/src"
STATIC_ROUTES="${FRONTEND_SRC_DIR}/router/static-routes.ts"

if [[ ! -f "${STATIC_ROUTES}" ]]; then
  echo "Missing static routes: ${STATIC_ROUTES}" >&2
  exit 1
fi

python3 - "$FRONTEND_SRC_DIR" "$STATIC_ROUTES" <<'PY'
import re
import sys
from pathlib import Path

frontend_src_dir = Path(sys.argv[1])
static_routes = Path(sys.argv[2])
routes_text = static_routes.read_text(encoding="utf-8")

expected_routes = {
    "/system/users": "views/system/users/UsersView.vue",
    "/system/roles": "views/system/roles/RolesView.vue",
    "/system/permissions": "views/system/permissions/PermissionsView.vue",
    "/system/menus": "views/system/menus/MenusView.vue",
    "/system/depts": "views/system/depts/DepartmentsView.vue",
    "/system/posts": "views/system/posts/PostsView.vue",
    "/system/dicts": "views/system/dicts/DictsView.vue",
    "/system/settings": "views/system/settings/SettingsView.vue",
    "/system/i18n": "views/system/i18n/I18nMessagesView.vue",
    "/system/logs/login": "views/system/logs/LoginLogsView.vue",
    "/system/logs/audit": "views/system/logs/AuditLogsView.vue",
    "/system/security": "views/system/security/SecurityCenterView.vue",
    "/system/logs/security": "views/system/logs/SecurityEventsView.vue",
    "/system/files": "views/system/files/FilesView.vue",
}

hardening_requirements = {
    "views/system/users/UsersView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/roles/RolesView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/permissions/PermissionsView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/menus/MenusView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {", "sortMenuApi", "sys:menu:sort"],
    "views/system/depts/DepartmentsView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/posts/PostsView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/dicts/DictsView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {", "enableDictTypeApi", "disableDictTypeApi", "enableDictValueApi", "disableDictValueApi", "sys:dict:type:enable", "sys:dict:value:disable"],
    "views/system/settings/SettingsView.vue": ["reloadSettingCacheApi", "validateIpRulesApi", "sys:setting:reload-cache"],
    "views/system/i18n/I18nMessagesView.vue": ["NDataTable", ":loading=", "#empty", "apiErrorMessage", "FormRules", "try {"],
    "views/system/files/FilesView.vue": ["selectedPolicy", "policyOptions", "policy: selectedPolicy.value", "isPreviewable"],
}

issues: list[str] = []

for route, relative_path in expected_routes.items():
    route_pattern = re.escape(f'path: "{route}"')
    if not re.search(route_pattern, routes_text):
        issues.append(f"missing route {route}")

    import_target = relative_path.removeprefix("views/")
    if f'@/views/{import_target}' not in routes_text:
        issues.append(f"route {route} does not import {relative_path}")

    view_path = frontend_src_dir / relative_path
    if not view_path.is_file():
        issues.append(f"missing view file {relative_path}")
        continue

    text = view_path.read_text(encoding="utf-8")
    if relative_path.startswith("views/system/") and "get" not in text:
        issues.append(f"{relative_path} does not appear to call a read API")

    for required in hardening_requirements.get(relative_path, []):
        if required not in text:
            issues.append(f"{relative_path} missing smoke requirement: {required}")

api_dir = frontend_src_dir / "api" / "system"
if not api_dir.is_dir():
    issues.append(f"missing system api directory {api_dir}")
else:
    for path in sorted(api_dir.glob("*.ts")):
        text = path.read_text(encoding="utf-8")
        if re.search(r"pageSize\s*[:=]\s*100", text):
            issues.append(f"{path.relative_to(frontend_src_dir)} still uses fixed pageSize=100")

if issues:
    print("check-frontend-smoke-fixtures: failed", file=sys.stderr)
    for issue in issues:
        print(f"  - {issue}", file=sys.stderr)
    raise SystemExit(1)

print(f"check-frontend-smoke-fixtures: ok ({len(expected_routes)} system routes checked)")
PY
