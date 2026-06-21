#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/specs/ph1-security-baseline/spec.md",
    "docs/specs/ph1-security-baseline/tasks.md",
    "docs/specs/ph1-security-baseline/checklist.md",
    "docs/ops/security-baseline.md",
    "docs/ops/deployment-reverse-proxy.md",
    "docs/ops/rate-limit-baseline.md",
    "backend/src/WeCms.Api/Security/WeCmsCorsExtensions.cs",
    "backend/src/WeCms.Api/Security/WeCmsForwardedHeadersExtensions.cs",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

program = (repo / "backend/src/WeCms.Api/Program.cs").read_text(encoding="utf-8")
for token in [
    "AddWeCmsForwardedHeaders",
    "AddWeCmsCors",
    "UseWeCmsForwardedHeaders",
    "UseHsts",
    "UseCors(WeCmsCorsPolicyNames.AdminApi)",
    "UseAuthentication",
    "UseAuthorization",
]:
    if token not in program:
        violations.append(f"Program.cs missing {token}")

if "UseHttpsRedirection" in program:
    violations.append("Program.cs must not enable blind HTTPS redirection for proxy TLS mode")

ordered = [
    "UseWeCmsForwardedHeaders",
    "UseHsts",
    "UseMiddleware<RequestIdMiddleware>",
    "UseCors",
    "UseAuthentication",
    "UseAuthorization",
]
positions = [program.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions):
    violations.append("Program.cs security middleware order is invalid")

cors = (repo / "backend/src/WeCms.Api/Security/WeCmsCorsExtensions.cs").read_text(encoding="utf-8")
if "AllowAnyOrigin" in cors:
    violations.append("CORS policy must not use AllowAnyOrigin")
for token in ["WithOrigins(origins)", "AllowCredentials", "Security:AllowedOrigins"]:
    if token not in cors:
        violations.append(f"CORS policy missing {token}")

auth = (repo / "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs").read_text(encoding="utf-8")
for token in [
    "RefreshCookieOptionsFactory",
    "CreateAppendOptions(session)",
    "CreateDeleteOptions()",
    "HttpOnly = true",
    "Secure = true",
    "SameSite = SameSiteMode.Strict",
    "Path = \"/\"",
]:
    if token not in auth:
        violations.append(f"AuthEndpointDefinition refresh cookie policy missing {token}")
if "Domain =" in auth:
    violations.append("Refresh cookie must not set Domain")

template = json.loads((repo / "backend/src/WeCms.Api/appsettings.Production.example.json").read_text(encoding="utf-8"))
security = template.get("Security") or {}
forwarded = security.get("ForwardedHeaders") or {}
if forwarded.get("Enabled") is not True:
    violations.append("production template must enable forwarded headers for proxy deployment example")
if not forwarded.get("KnownProxies") and not forwarded.get("KnownNetworks"):
    violations.append("production template forwarded headers must include known proxy or network placeholders")

secure_headers = security.get("SecureHeaders") or {}
for key in ["CspEnabled", "CspReportOnlyEnabled", "Csp", "CspReportOnly"]:
    if key not in secure_headers:
        violations.append(f"production template SecureHeaders missing {key}")
for key in ["Csp", "CspReportOnly"]:
    value = secure_headers.get(key, "")
    if "object-src 'none'" not in value:
        violations.append(f"production template {key} missing object-src none")
    if "frame-ancestors" not in value:
        violations.append(f"production template {key} missing frame-ancestors")

security_docs = (repo / "docs/ops/security-baseline.md").read_text(encoding="utf-8")
for token in ["ForwardedHeaders", "CORS", "CSP", "Rate Limiting", "__Host-wecms_refresh"]:
    if token not in security_docs:
        violations.append(f"security baseline docs missing {token}")

if violations:
    raise SystemExit("check-security-baseline: " + "; ".join(violations))

print("check-security-baseline: ok")
PY
