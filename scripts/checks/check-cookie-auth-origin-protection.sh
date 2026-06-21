#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

auth_endpoints_path = repo / "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs"
validator_path = repo / "backend/src/WeCms.Modules.Identity/Services/CookieAuthOriginValidation.cs"
validator_tests_path = repo / "backend/tests/WeCms.Tests.Unit/Auth/CookieAuthOriginValidatorTests.cs"
endpoint_tests_path = repo / "backend/tests/WeCms.Tests.Unit/Auth/AuthEndpointSourceTests.cs"

required_files = [
    auth_endpoints_path,
    validator_path,
    validator_tests_path,
    endpoint_tests_path,
]

for path in required_files:
    if not path.is_file():
        violations.append(f"missing required cookie auth evidence file: {path.relative_to(repo)}")

if not violations:
    source = auth_endpoints_path.read_text(encoding="utf-8")
    route_requirements = {
        'MapPost("/refresh"': ["IdentityCookieAuthOriginEndpoints.Refresh", "ReadRefreshTokenCookie(context)", "AppendRefreshTokenCookie(context, session)"],
        'MapPost("/logout"': ["IdentityCookieAuthOriginEndpoints.Logout", "ReadRefreshTokenCookie(context)", "DeleteRefreshTokenCookie(context)"],
        'MapPost("/2fa/verify"': ["IdentityCookieAuthOriginEndpoints.TwoFactorVerify", "AppendRefreshTokenCookie(context, session)"],
        'MapPost("/2fa/recovery-code"': ["IdentityCookieAuthOriginEndpoints.TwoFactorRecoveryCode", "AppendRefreshTokenCookie(context, session)"],
    }

    for route_fragment, tokens in route_requirements.items():
        route_index = source.find(route_fragment)
        if route_index < 0:
            violations.append(f"missing cookie auth route {route_fragment}")
            continue

        next_route_index = source.find("group.Map", route_index + 1)
        route_source = source[route_index:] if next_route_index < 0 else source[route_index:next_route_index]
        if "cookieAuthOriginValidator.ValidateAsync" not in route_source:
            violations.append(f"{route_fragment} missing cookieAuthOriginValidator.ValidateAsync")
        for token in tokens:
            if token not in route_source:
                violations.append(f"{route_fragment} missing token {token!r}")

    for token in [
        'RefreshCookieName = "__Host-wecms_refresh"',
        "HttpOnly = true",
        "Secure = true",
        "SameSite = SameSiteMode.Strict",
        'Path = "/"',
    ]:
        if token not in source:
            violations.append(f"AuthEndpointDefinition.cs missing secure refresh cookie token {token!r}")

    validator_source = validator_path.read_text(encoding="utf-8")
    for token in [
        "Security:RequireOriginForCookieAuth=false is only allowed in Development.",
        "Security:AllowedOrigins must contain at least one origin outside Development.",
        "Security:AllowedOrigins must not contain wildcard origins.",
        "TryReadNormalizedRefererOrigin",
        "auth.cookie_origin_rejected",
        "RecordSecurityEventAsync",
    ]:
        if token not in validator_source:
            violations.append(f"CookieAuthOriginValidation.cs missing token {token!r}")

    validator_tests = validator_tests_path.read_text(encoding="utf-8")
    for token in [
        "ValidateAsync_AllowsConfiguredOrigin",
        "ValidateAsync_AllowsConfiguredRefererFallbackWhenOriginIsMissing",
        "ValidateAsync_RejectsIllegalOriginAndWritesSecurityEvent",
        "ValidateAsync_RejectsMissingOriginWhenRefererFallbackIsDisabled",
        "ValidateAsync_RejectsMissingOriginWithIllegalRefererFallback",
        "Constructor_RejectsWildcardAllowedOrigins",
        "Constructor_RejectsEmptyAllowedOriginsOutsideDevelopment",
        "Constructor_RejectsDisabledOriginRequirementOutsideDevelopment",
    ]:
        if token not in validator_tests:
            violations.append(f"CookieAuthOriginValidatorTests.cs missing token {token!r}")

    endpoint_tests = endpoint_tests_path.read_text(encoding="utf-8")
    for token in [
        "IdentityCookieAuthOriginEndpoints.Refresh",
        "IdentityCookieAuthOriginEndpoints.Logout",
        "IdentityCookieAuthOriginEndpoints.TwoFactorVerify",
        "IdentityCookieAuthOriginEndpoints.TwoFactorRecoveryCode",
        "AuthEndpoints_UseSecureHttpOnlyRefreshCookie",
    ]:
        if token not in endpoint_tests:
            violations.append(f"AuthEndpointSourceTests.cs missing token {token!r}")

for relative_config_path in [
    "backend/src/WeCms.Api/appsettings.json",
    "backend/src/WeCms.Api/appsettings.Development.json",
]:
    path = repo / relative_config_path
    if not path.is_file():
        violations.append(f"missing config file {relative_config_path}")
        continue

    data = json.loads(path.read_text(encoding="utf-8"))
    security = data.get("Security") or {}
    allowed_origins = security.get("AllowedOrigins") or []
    if not isinstance(allowed_origins, list):
        violations.append(f"{relative_config_path} Security:AllowedOrigins must be an array when present")
        continue

    for origin in allowed_origins:
        if not isinstance(origin, str) or not origin.strip():
            violations.append(f"{relative_config_path} contains blank allowed origin")
        elif "*" in origin:
            violations.append(f"{relative_config_path} contains wildcard allowed origin")

    require_origin = security.get("RequireOriginForCookieAuth")
    if relative_config_path.endswith("appsettings.json") and require_origin is False:
        violations.append("appsettings.json must not disable Security:RequireOriginForCookieAuth")

if violations:
    raise SystemExit("check-cookie-auth-origin-protection: " + "; ".join(violations))

print("check-cookie-auth-origin-protection: ok (4 cookie auth endpoints checked)")
PY
