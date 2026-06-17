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
    raise SystemExit(f"check-openapi-auth-request-body: missing {path}")

document = json.loads(path.read_text(encoding="utf-8"))
paths = document.get("paths", {})
schemas = document.get("components", {}).get("schemas", {})

body_required = {
    "/api/v1/auth/login": "LoginRequest",
}

body_forbidden = (
    "/api/v1/auth/refresh",
    "/api/v1/auth/logout",
)

for route, schema_name in body_required.items():
    operation = paths.get(route, {}).get("post")
    if operation is None:
        raise SystemExit(f"check-openapi-auth-request-body: missing POST {route}")

    request_body = operation.get("requestBody")
    if not request_body:
        raise SystemExit(f"check-openapi-auth-request-body: POST {route} missing requestBody")

    if request_body.get("required") is not True:
        raise SystemExit(f"check-openapi-auth-request-body: POST {route} requestBody must be required")

    schema_ref = (
        request_body
        .get("content", {})
        .get("application/json", {})
        .get("schema", {})
        .get("$ref")
    )
    if schema_ref != f"#/components/schemas/{schema_name}":
        raise SystemExit(
            f"check-openapi-auth-request-body: POST {route} requestBody ref expected {schema_name}, got {schema_ref}"
        )

for route in body_forbidden:
    operation = paths.get(route, {}).get("post")
    if operation is None:
        raise SystemExit(f"check-openapi-auth-request-body: missing POST {route}")

    if "requestBody" in operation:
        raise SystemExit(f"check-openapi-auth-request-body: POST {route} must not declare requestBody")

for schema_name in ("LoginRequest", "LoginResponse", "AuthMeResponse"):
    if schema_name not in schemas:
        raise SystemExit(f"check-openapi-auth-request-body: missing schema {schema_name}")

for schema_name in ("RefreshTokenRequest", "LogoutRequest"):
    if schema_name in schemas:
        raise SystemExit(f"check-openapi-auth-request-body: obsolete schema {schema_name} must not be exported")

print("check-openapi-auth-request-body: ok")
PY
