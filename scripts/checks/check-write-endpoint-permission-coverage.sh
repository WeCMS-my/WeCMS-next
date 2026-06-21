#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
openapi_path="${1:-$repo_root/artifacts/openapi/wecms-api-v1.json}"

python3 - "$repo_root" "$openapi_path" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
path = Path(sys.argv[2])
if not path.is_file():
    raise SystemExit(f"check-write-endpoint-permission-coverage: missing {path}")

document = json.loads(path.read_text(encoding="utf-8"))
paths = document.get("paths", {})

write_methods = {"post", "put", "patch", "delete"}
anonymous_allowlist = {
    ("post", "/api/v1/auth/login"): {
        "owner": "auth",
        "reason": "credential exchange before user identity exists",
        "risk_compensation": "DTO validation, login audit, login failure limiter, and security events",
    },
    ("post", "/api/v1/auth/refresh"): {
        "owner": "auth",
        "reason": "refresh cookie endpoint is intentionally anonymous to renew expired access tokens",
        "risk_compensation": "refresh token rotation, audit log, and cookie Origin/CSRF validation",
    },
    ("post", "/api/v1/auth/logout"): {
        "owner": "auth",
        "reason": "logout cookie endpoint must tolerate expired access tokens",
        "risk_compensation": "audit log and cookie Origin/CSRF validation",
    },
    ("post", "/api/v1/auth/2fa/verify"): {
        "owner": "auth",
        "reason": "2FA verification completes an anonymous pre-auth challenge",
        "risk_compensation": "short-lived challenge, failure limit, audit log, security events, and cookie Origin/CSRF validation",
    },
    ("post", "/api/v1/auth/2fa/recovery-code"): {
        "owner": "auth",
        "reason": "2FA recovery code completes an anonymous pre-auth challenge",
        "risk_compensation": "short-lived challenge, one-time hashed recovery code, failure limit, audit log, security events, and cookie Origin/CSRF validation",
    },
}
self_service_allowlist = {
    ("post", "/api/v1/account/2fa/setup"): {
        "owner": "account",
        "reason": "authenticated account self-service setup without system administration permission",
        "risk_compensation": "bearer authentication, audit log, and security event",
    },
    ("post", "/api/v1/account/2fa/confirm"): {
        "owner": "account",
        "reason": "authenticated account self-service confirmation without system administration permission",
        "risk_compensation": "bearer authentication, TOTP validation, audit log, and security event",
    },
    ("post", "/api/v1/account/2fa/disable"): {
        "owner": "account",
        "reason": "authenticated account self-service disable without system administration permission",
        "risk_compensation": "bearer authentication, current password or TOTP verification, audit log, and security event",
    },
    ("post", "/api/v1/account/2fa/recovery-codes/regenerate"): {
        "owner": "account",
        "reason": "authenticated account self-service recovery code regeneration without system administration permission",
        "risk_compensation": "bearer authentication, current password or TOTP verification, audit log, and security event",
    },
    ("put", "/api/v1/account/profile"): {
        "owner": "account",
        "reason": "authenticated account self-service profile update without system administration permission",
        "risk_compensation": "bearer authentication, editable-field whitelist, DTO validation, audit log, and security event",
    },
    ("put", "/api/v1/account/password"): {
        "owner": "account",
        "reason": "authenticated account self-service password change without system administration permission",
        "risk_compensation": "bearer authentication, current password verification, password policy, refresh-token revocation, audit log, and security event",
    },
    ("post", "/api/v1/account/avatar"): {
        "owner": "account",
        "reason": "authenticated account self-service avatar upload without system administration permission",
        "risk_compensation": "bearer authentication, MIME/extension/size/hash validation, audit log, and security event",
    },
}
account_permission_allowlist = {
    ("post", "/api/v1/account/i18n/switch"): {
        "owner": "account",
        "reason": "authenticated account locale switch is represented by an account-scoped permission",
        "risk_compensation": "bearer authentication, DTO validation, account permission metadata, and audit log",
    },
}

violations: list[str] = []
checked = 0
for (method, route), metadata in sorted(anonymous_allowlist.items()):
    for field in ("owner", "reason", "risk_compensation"):
        value = metadata.get(field)
        if not isinstance(value, str) or not value.strip():
            violations.append(f"{method.upper()} {route} anonymous allowlist missing {field}")

for (method, route), metadata in sorted(self_service_allowlist.items()):
    for field in ("owner", "reason", "risk_compensation"):
        value = metadata.get(field)
        if not isinstance(value, str) or not value.strip():
            violations.append(f"{method.upper()} {route} self-service allowlist missing {field}")

for (method, route), metadata in sorted(account_permission_allowlist.items()):
    for field in ("owner", "reason", "risk_compensation"):
        value = metadata.get(field)
        if not isinstance(value, str) or not value.strip():
            violations.append(f"{method.upper()} {route} account permission allowlist missing {field}")

auth_endpoint_source = repo / "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs"
if not auth_endpoint_source.is_file():
    violations.append("AuthEndpointDefinition.cs missing for anonymous cookie auth validation evidence")
else:
    source = auth_endpoint_source.read_text(encoding="utf-8")
    for route_fragment in ('MapPost("/refresh"', 'MapPost("/logout"', 'MapPost("/2fa/verify"', 'MapPost("/2fa/recovery-code"'):
        route_index = source.find(route_fragment)
        if route_index < 0:
            violations.append(f"anonymous cookie auth endpoint missing source route: {route_fragment}")
            continue

        next_route_index = source.find("group.Map", route_index + 1)
        route_source = source[route_index:] if next_route_index < 0 else source[route_index:next_route_index]
        if "cookieAuthOriginValidator.ValidateAsync" not in route_source:
            violations.append(f"{route_fragment} missing cookieAuthOriginValidator.ValidateAsync")

for route, operations in sorted(paths.items()):
    if not isinstance(operations, dict):
        continue

    for method, operation in sorted(operations.items()):
        if method.lower() not in write_methods:
            continue

        checked += 1
        key = (method.lower(), route)
        if key in anonymous_allowlist:
            continue

        if key in self_service_allowlist:
            if operation.get("security") != [{"bearerAuth": []}]:
                violations.append(f"{method.upper()} {route} missing bearerAuth security")
            if operation.get("x-wecms-permission") is not None:
                violations.append(f"{method.upper()} {route} must not declare system permission metadata")
            continue

        if key in account_permission_allowlist:
            if operation.get("security") != [{"bearerAuth": []}]:
                violations.append(f"{method.upper()} {route} missing bearerAuth security")
            permission = operation.get("x-wecms-permission")
            if not isinstance(permission, str) or not permission.strip():
                violations.append(f"{method.upper()} {route} missing x-wecms-permission")
            continue

        if not route.startswith("/api/v1/system/"):
            violations.append(f"{method.upper()} {route} is not system-scoped and is not allowlisted")
            continue

        if operation.get("security") != [{"bearerAuth": []}]:
            violations.append(f"{method.upper()} {route} missing bearerAuth security")

        permission = operation.get("x-wecms-permission")
        if not isinstance(permission, str) or not permission.strip():
            violations.append(f"{method.upper()} {route} missing x-wecms-permission")

if violations:
    raise SystemExit("check-write-endpoint-permission-coverage: " + "; ".join(violations))

print(f"check-write-endpoint-permission-coverage: ok ({checked} write endpoints checked)")
PY
