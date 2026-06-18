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
    raise SystemExit(f"check-openapi-endpoint-coverage: missing {path}")

document = json.loads(path.read_text(encoding="utf-8"))
paths = document.get("paths", {})

expected = {
    "/health/live": ["get"],
    "/health/ready": ["get"],
    "/api/v1/system/ping": ["get"],
    "/api/v1/system/version": ["get"],
    "/api/v1/system/db-check": ["get"],
    "/api/v1/system/secure-ping": ["get"],
    "/api/v1/auth/login": ["post"],
    "/api/v1/auth/refresh": ["post"],
    "/api/v1/auth/logout": ["post"],
    "/api/v1/auth/2fa/verify": ["post"],
    "/api/v1/auth/2fa/recovery-code": ["post"],
    "/api/v1/auth/me": ["get"],
    "/api/v1/account/2fa/status": ["get"],
    "/api/v1/account/2fa/setup": ["post"],
    "/api/v1/account/2fa/confirm": ["post"],
    "/api/v1/account/2fa/disable": ["post"],
    "/api/v1/account/2fa/recovery-codes/regenerate": ["post"],
    "/api/v1/account/profile": ["get", "put"],
    "/api/v1/account/password": ["put"],
    "/api/v1/account/avatar": ["post"],
    "/api/v1/account/avatar/content": ["get"],
    "/api/v1/account/security": ["get"],
}

for route, methods in expected.items():
    for method in methods:
        if method not in paths.get(route, {}):
            raise SystemExit(f"check-openapi-endpoint-coverage: missing {method.upper()} {route}")

secure_ping = paths["/api/v1/system/secure-ping"]["get"]
security = secure_ping.get("security")
if security != [{"bearerAuth": []}]:
    raise SystemExit("check-openapi-endpoint-coverage: secure-ping missing bearerAuth security")

permission = secure_ping.get("x-wecms-permission")
if permission != "sys:system:secure-ping":
    raise SystemExit("check-openapi-endpoint-coverage: secure-ping missing permission metadata")

schemes = document.get("components", {}).get("securitySchemes", {})
if schemes.get("bearerAuth", {}).get("scheme") != "bearer":
    raise SystemExit("check-openapi-endpoint-coverage: missing bearerAuth security scheme")

schemas = document.get("components", {}).get("schemas", {})
if "ApiResult" not in schemas:
    raise SystemExit("check-openapi-endpoint-coverage: missing ApiResult schema")

def walk(value):
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from walk(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk(child)

for node in walk(document):
    ref = node.get("$ref") if isinstance(node, dict) else None
    if not ref:
        continue

    prefix = "#/components/schemas/"
    if not ref.startswith(prefix):
        raise SystemExit(f"check-openapi-endpoint-coverage: unsupported ref {ref}")

    schema_name = ref.removeprefix(prefix)
    if schema_name not in schemas:
        raise SystemExit(f"check-openapi-endpoint-coverage: dangling ref {ref}")

print("check-openapi-endpoint-coverage: ok")
PY
