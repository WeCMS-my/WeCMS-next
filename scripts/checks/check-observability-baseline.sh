#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/specs/ph3-observability/spec.md",
    "docs/specs/ph3-observability/tasks.md",
    "docs/specs/ph3-observability/checklist.md",
    "docs/ops/logging-observability.md",
    "docs/ops/security-alerting.md",
    "backend/src/WeCms.Api/Middleware/RequestLoggingMiddleware.cs",
    "backend/src/WeCms.Modules.System/Security/SecurityAlerting.cs",
    "backend/src/WeCms.Persistence/Modules/System/System/SystemMigrationProbe.cs",
    "backend/src/WeCms.Modules.System/System/ISystemMigrationProbe.cs",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""

program = read("backend/src/WeCms.Api/Program.cs")
if "app.UseMiddleware<RequestIdMiddleware>();" not in program:
    violations.append("Program.cs must register RequestIdMiddleware")
if "app.UseMiddleware<RequestLoggingMiddleware>();" not in program:
    violations.append("Program.cs must register RequestLoggingMiddleware")
if program.find("RequestIdMiddleware") > program.find("RequestLoggingMiddleware"):
    violations.append("RequestLoggingMiddleware must run after RequestIdMiddleware")

request_logging = read("backend/src/WeCms.Api/Middleware/RequestLoggingMiddleware.cs")
for token in ["TraceId", "UserId", "Username", "Method", "Path", "StatusCode", "ElapsedMs", "EventType", "BeginScope"]:
    if token not in request_logging:
        violations.append(f"RequestLoggingMiddleware missing {token}")
for forbidden in ["Authorization", "Cookie", "ReadFromJsonAsync", "EnableBuffering", "password", "refresh token", "access token"]:
    if forbidden in request_logging:
        violations.append(f"RequestLoggingMiddleware must not reference {forbidden}")

system_endpoints = read("backend/src/WeCms.Modules.System/System/SystemEndpointExtensions.cs")
for token in ['MapGet("/health/live"', 'MapGet("/health/ready"', 'MapGet("/health/dependencies"', "ISystemDatabaseProbe", "ISystemMigrationProbe"]:
    if token not in system_endpoints:
        violations.append(f"SystemEndpointExtensions missing {token}")
live_start = system_endpoints.find('MapGet("/health/live"')
ready_start = system_endpoints.find('MapGet("/health/ready"')
if live_start < 0 or ready_start < 0 or "ISystemDatabaseProbe" in system_endpoints[live_start:ready_start]:
    violations.append("/health/live must not depend on database probe")
dependencies_start = system_endpoints.find('MapGet("/health/dependencies"')
ping_start = system_endpoints.find('MapGet("/api/v1/system/ping"', dependencies_start)
dependencies_block = system_endpoints[dependencies_start:ping_start] if dependencies_start >= 0 and ping_start >= 0 else ""
if "RequireAuthorization()" not in dependencies_block:
    violations.append("/health/dependencies must be protected")
if "RequirePermission(SystemPermissions.SecurePing)" not in dependencies_block:
    violations.append("/health/dependencies must require sys:system:secure-ping permission")
if ".Message" in system_endpoints:
    violations.append("health endpoints must not expose exception messages")

records = read("backend/src/WeCms.Modules.System/System/SystemRecords.cs")
for token in ["SystemDependenciesResponse", "SystemDependencyStatus", "SystemMigrationProbeResult", "LatencyMs", "FailureCode"]:
    if token not in records:
        violations.append(f"SystemRecords missing {token}")

migration_probe = read("backend/src/WeCms.Persistence/Modules/System/System/SystemMigrationProbe.cs")
for token in ["Database:LatestRequiredMigration", "WHERE version = @version", "latest_required_migration_missing"]:
    if token not in migration_probe:
        violations.append(f"SystemMigrationProbe missing {token}")

json_context = read("backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs")
for token in ["ApiResult<SystemDependenciesResponse>", "SystemDependencyStatus", "SystemDependenciesResponse"]:
    if token not in json_context:
        violations.append(f"JsonSerializerContext missing {token}")

openapi = read("backend/src/WeCms.Api/Extensions/OpenApiExtensions.cs")
if '"/health/dependencies"' not in openapi:
    violations.append("OpenAPI endpoints missing /health/dependencies")

alerts = read("backend/src/WeCms.Modules.System/Security/SecurityAlerting.cs")
for token in ["ISecurityAlertSink", "LoggingSecurityAlertSink", "ISecurityAlertService", '"critical"', '"high"']:
    if token not in alerts:
        violations.append(f"SecurityAlerting missing {token}")

alert_sources = {
    "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs": "rate limit alert routing",
    "backend/src/WeCms.Modules.System/Auth/AuthSecurityEventWriter.cs": "auth security alert routing",
    "backend/src/WeCms.Modules.System/Auth/LoginFailureLimiter.cs": "login failure alert routing",
    "backend/src/WeCms.Modules.System/Auth/AuthTwoFactorChallengeService.cs": "2FA alert routing",
    "backend/src/WeCms.Modules.System/Security/SecurityBanService.cs": "security ban alert routing",
    "backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs": "IP access alert routing",
}
for relative, label in alert_sources.items():
    source = read(relative)
    for token in ["ISecurityAlertService", "PublishIfRequiredAsync", "SecurityAlertRecord"]:
        if token not in source:
            violations.append(f"{label} missing {token}")

ip_access = read("backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs")
if "context.TraceIdentifier" not in ip_access:
    violations.append("IpAccessControlMiddleware must record trace id in security event")
if "security.ip_rejected" not in ip_access:
    violations.append("IpAccessControlMiddleware must use classifier-backed security.ip_rejected event")

docs = read("docs/ops/logging-observability.md") + read("docs/ops/security-alerting.md")
for token in ["Authorization", "Cookie", "password", "/health/live", "/health/ready", "/health/dependencies", "ISecurityAlertSink"]:
    if token not in docs:
        violations.append(f"PH-3 docs missing {token}")

if violations:
    print("check-observability-baseline: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-observability-baseline: ok")
PY
