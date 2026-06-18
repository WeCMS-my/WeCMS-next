#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

adr_path = repo / "docs/adr/0016-admingate-csrf-migration-strategy.md"
legacy_reference_path = repo / "database/legacy-reference/thinkphp_schema_reference.sql"

if not adr_path.is_file():
    violations.append("docs/adr/0016-admingate-csrf-migration-strategy.md is required")
else:
    adr = adr_path.read_text(encoding="utf-8")
    for token in [
        "We will not create an `AdminGateMiddleware`",
        "Authentication",
        "Refresh Token Repository + token family revocation",
        "`RequirePermission` + `PermissionEndpointFilter`",
        "Auth challenge + TwoFactorService",
        "SecurityEventClassifier",
        "`IIpRuleMatcher` + IpAccessControlMiddleware",
        "SecurityBanService + SecurityBanMiddleware",
        "Audit middleware / AuditLogService",
        "SettingService + SettingCache",
        "Rate limiting + SecurityBanService",
        "SecureHeadersMiddleware",
        "Origin / Referer / SameSite checks",
        "does not provide old ThinkPHP runtime compatibility",
    ]:
        if token not in adr:
            violations.append(f"ADR-0016 missing AdminGate/CSRF mapping token {token!r}")

if not legacy_reference_path.is_file():
    violations.append("database/legacy-reference/thinkphp_schema_reference.sql is required as reference-only evidence")
else:
    legacy_reference = legacy_reference_path.read_text(encoding="utf-8")
    for token in [
        "reference-only",
        "not a migration, seed, or compatibility script",
        "no legacy user import and no old password hash compatibility",
        "dynamic URL matching is replaced by explicit permission codes",
    ]:
        if token not in legacy_reference:
            violations.append(f"legacy reference missing non-compatibility token {token!r}")

component_tokens = {
    "backend/src/WeCms.Modules.System/Auth/AccessTokenAuthenticationHandler.cs": ["AuthenticationHandler"],
    "backend/src/WeCms.Modules.System/Auth/RefreshTokenRotationService.cs": ["RevokeRefreshTokenFamilyAsync"],
    "backend/src/WeCms.Modules.System/Permissions/PermissionEndpointFilter.cs": ["PermissionEndpointFilter"],
    "backend/src/WeCms.Modules.System/Auth/AuthTwoFactorChallengeService.cs": ["AuthTwoFactorChallengeService"],
    "backend/src/WeCms.Shared/Security/SecurityEventClassifier.cs": ["csrf_origin_rejected", "permission_denied"],
    "backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs": ["IIpRuleMatcher", "security.ip_access_denied"],
    "backend/src/WeCms.Modules.System/Security/SecurityBanService.cs": ["SecurityBanService"],
    "backend/src/WeCms.Api/Middleware/SecurityBanMiddleware.cs": ["SecurityBanMiddleware"],
    "backend/src/WeCms.Modules.System/Logs/AuditLogEndpoints.cs": ["AuditLog"],
    "backend/src/WeCms.Modules.System/Settings/SettingService.cs": ["SettingService"],
    "backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs": ["RateLimitPolicyNames"],
    "backend/src/WeCms.Api/Middleware/SecureHeadersMiddleware.cs": ["SecureHeadersMiddleware"],
    "backend/src/WeCms.Modules.System/Auth/CookieAuthOriginValidation.cs": ["TryReadNormalizedRefererOrigin", "auth.cookie_origin_rejected"],
}

for relative_path, tokens in component_tokens.items():
    path = repo / relative_path
    if not path.is_file():
        violations.append(f"missing decomposed AdminGate responsibility owner: {relative_path}")
        continue
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            violations.append(f"{relative_path} missing responsibility token {token!r}")

for root in ["backend/src", "frontend/soybean-admin/src"]:
    for path in (repo / root).rglob("*"):
        if not path.is_file() or path.suffix.lower() not in {".cs", ".ts", ".tsx", ".vue", ".js", ".mjs"}:
            continue
        text = path.read_text(encoding="utf-8")
        lowered = text.lower()
        if "admingate" in lowered:
            violations.append(f"{path.relative_to(repo)} contains AdminGate runtime reference")
        if "thinkphp" in lowered or "think_" in lowered:
            violations.append(f"{path.relative_to(repo)} contains legacy ThinkPHP runtime reference")

if violations:
    raise SystemExit("check-admingate-csrf-migration: " + "; ".join(violations))

print("check-admingate-csrf-migration: ok (13 decomposed responsibilities checked)")
PY
