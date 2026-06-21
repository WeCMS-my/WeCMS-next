#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
openapi_path="${1:-$repo_root/artifacts/openapi/wecms-api-v1.json}"

python3 - "$openapi_path" <<'PY'
import json
import sys
from pathlib import Path

path = Path(sys.argv[1])
if not path.is_file():
    raise SystemExit(f"check-system-openapi-coverage: missing {path}")

document = json.loads(path.read_text(encoding="utf-8"))
paths = document.get("paths", {})

expected = {
    "/api/v1/system/users": {"get", "post"},
    "/api/v1/system/users/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/users/{id:long}/enable": {"post"},
    "/api/v1/system/users/{id:long}/disable": {"post"},
    "/api/v1/system/users/{id:long}/reset-password": {"post"},
    "/api/v1/system/users/{id:long}/reset-2fa": {"post"},
    "/api/v1/system/users/{id:long}/roles": {"put"},
    "/api/v1/system/users/{id:long}/positions": {"put"},
    "/api/v1/system/roles": {"get", "post"},
    "/api/v1/system/roles/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/roles/{id:long}/enable": {"post"},
    "/api/v1/system/roles/{id:long}/disable": {"post"},
    "/api/v1/system/roles/{id:long}/permissions": {"put"},
    "/api/v1/system/roles/{id:long}/menus": {"put"},
    "/api/v1/system/menus": {"get", "post"},
    "/api/v1/system/menus/tree": {"get"},
    "/api/v1/system/menus/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/menus/{id:long}/enable": {"post"},
    "/api/v1/system/menus/{id:long}/disable": {"post"},
    "/api/v1/system/permissions": {"get", "post"},
    "/api/v1/system/permissions/tree": {"get"},
    "/api/v1/system/permissions/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/permissions/{id:long}/enable": {"post"},
    "/api/v1/system/permissions/{id:long}/disable": {"post"},
    "/api/v1/system/depts": {"get", "post"},
    "/api/v1/system/depts/tree": {"get"},
    "/api/v1/system/depts/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/depts/{id:long}/enable": {"post"},
    "/api/v1/system/depts/{id:long}/disable": {"post"},
    "/api/v1/system/positions": {"get", "post"},
    "/api/v1/system/positions/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/positions/{id:long}/enable": {"post"},
    "/api/v1/system/positions/{id:long}/disable": {"post"},
    "/api/v1/system/dict-types": {"get", "post"},
    "/api/v1/system/dict-types/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/dict-types/{id:long}/enable": {"post"},
    "/api/v1/system/dict-types/{id:long}/disable": {"post"},
    "/api/v1/system/dict-types/{typeCode}/values": {"get", "post"},
    "/api/v1/system/dict-values/{id:long}": {"put", "delete"},
    "/api/v1/system/dict-values/{id:long}/enable": {"post"},
    "/api/v1/system/dict-values/{id:long}/disable": {"post"},
    "/api/v1/system/settings": {"get"},
    "/api/v1/system/settings/{key}": {"get", "put"},
    "/api/v1/system/settings/validate-ip-rules": {"post"},
    "/api/v1/system/settings/reload-cache": {"post"},
    "/api/v1/system/i18n/messages": {"get", "post"},
    "/api/v1/system/i18n/messages/{id:long}": {"get", "put", "delete"},
    "/api/v1/system/login-logs": {"get"},
    "/api/v1/system/login-logs/{id:long}": {"get"},
    "/api/v1/system/audit-logs": {"get"},
    "/api/v1/system/audit-logs/{id:long}": {"get"},
    "/api/v1/system/security/status": {"get"},
    "/api/v1/system/security/bans": {"get"},
    "/api/v1/system/security/bans/{id:long}": {"get"},
    "/api/v1/system/security/bans/{id:long}/unban": {"post"},
    "/api/v1/system/security/bans/batch-unban": {"post"},
    "/api/v1/system/security-events": {"get"},
    "/api/v1/system/security-events/{id:long}": {"get"},
    "/api/v1/system/files": {"get", "post"},
    "/api/v1/system/files/{id:long}": {"get", "delete"},
    "/api/v1/system/files/{id:long}/download": {"get"},
    "/api/v1/system/files/{id:long}/preview": {"get"},
}

for route, methods in expected.items():
    actual_methods = set(paths.get(route, {}).keys())
    missing = methods - actual_methods
    if missing:
        raise SystemExit(f"check-system-openapi-coverage: missing {','.join(sorted(missing)).upper()} {route}")

request_body_required = {
    ("/api/v1/system/users", "post"),
    ("/api/v1/system/users/{id:long}", "put"),
    ("/api/v1/system/users/{id:long}/reset-password", "post"),
    ("/api/v1/system/users/{id:long}/reset-2fa", "post"),
    ("/api/v1/system/users/{id:long}/roles", "put"),
    ("/api/v1/system/users/{id:long}/positions", "put"),
    ("/api/v1/system/roles", "post"),
    ("/api/v1/system/roles/{id:long}", "put"),
    ("/api/v1/system/roles/{id:long}/permissions", "put"),
    ("/api/v1/system/roles/{id:long}/menus", "put"),
    ("/api/v1/system/menus", "post"),
    ("/api/v1/system/menus/{id:long}", "put"),
    ("/api/v1/system/permissions", "post"),
    ("/api/v1/system/permissions/{id:long}", "put"),
    ("/api/v1/system/depts", "post"),
    ("/api/v1/system/depts/{id:long}", "put"),
    ("/api/v1/system/positions", "post"),
    ("/api/v1/system/positions/{id:long}", "put"),
    ("/api/v1/system/dict-types", "post"),
    ("/api/v1/system/dict-types/{id:long}", "put"),
    ("/api/v1/system/dict-types/{id:long}/disable", "post"),
    ("/api/v1/system/dict-types/{typeCode}/values", "post"),
    ("/api/v1/system/dict-values/{id:long}", "put"),
    ("/api/v1/system/settings/{key}", "put"),
    ("/api/v1/system/settings/validate-ip-rules", "post"),
    ("/api/v1/system/i18n/messages", "post"),
    ("/api/v1/system/i18n/messages/{id:long}", "put"),
    ("/api/v1/system/security/bans/{id:long}/unban", "post"),
    ("/api/v1/system/security/bans/batch-unban", "post"),
    ("/api/v1/system/files", "post"),
}

request_body_forbidden = {
    ("/api/v1/system/users/{id:long}/enable", "post"),
    ("/api/v1/system/users/{id:long}/disable", "post"),
    ("/api/v1/system/roles/{id:long}/enable", "post"),
    ("/api/v1/system/roles/{id:long}/disable", "post"),
    ("/api/v1/system/menus/{id:long}/enable", "post"),
    ("/api/v1/system/menus/{id:long}/disable", "post"),
    ("/api/v1/system/permissions/{id:long}/enable", "post"),
    ("/api/v1/system/permissions/{id:long}/disable", "post"),
    ("/api/v1/system/depts/{id:long}/enable", "post"),
    ("/api/v1/system/depts/{id:long}/disable", "post"),
    ("/api/v1/system/positions/{id:long}/enable", "post"),
    ("/api/v1/system/positions/{id:long}/disable", "post"),
}

for route, methods in expected.items():
    for method in methods:
        operation = paths[route][method]
        if operation.get("security") != [{"bearerAuth": []}]:
            raise SystemExit(f"check-system-openapi-coverage: {method.upper()} {route} missing bearerAuth security")
        if not operation.get("x-wecms-permission"):
            raise SystemExit(f"check-system-openapi-coverage: {method.upper()} {route} missing permission metadata")
        if (route, method) in request_body_required and "requestBody" not in operation:
            raise SystemExit(f"check-system-openapi-coverage: {method.upper()} {route} missing requestBody")
        if (route, method) in request_body_forbidden and "requestBody" in operation:
            raise SystemExit(f"check-system-openapi-coverage: {method.upper()} {route} must not declare requestBody")

list_routes = {
    "/api/v1/system/users", "/api/v1/system/roles", "/api/v1/system/positions", "/api/v1/system/dict-types",
    "/api/v1/system/settings", "/api/v1/system/login-logs", "/api/v1/system/audit-logs",
    "/api/v1/system/i18n/messages", "/api/v1/system/security/bans", "/api/v1/system/security-events", "/api/v1/system/files",
}
for route in list_routes:
    parameters = paths[route]["get"].get("parameters", [])
    names = {parameter.get("name") for parameter in parameters}
    if not {"page", "pageSize"}.issubset(names):
        raise SystemExit(f"check-system-openapi-coverage: GET {route} missing list pagination query parameters")

print("check-system-openapi-coverage: ok")
PY
