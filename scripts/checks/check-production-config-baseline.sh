#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import json
import ipaddress
import re
import sys
from pathlib import Path
from urllib.parse import urlparse

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/plans/wecms-production-hardening-plan-v1.0.md",
    "docs/specs/ph0-production-config-baseline/spec.md",
    "docs/specs/ph0-production-config-baseline/tasks.md",
    "docs/specs/ph0-production-config-baseline/checklist.md",
    "docs/ops/production-configuration.md",
    "backend/src/WeCms.Api/appsettings.Production.example.json",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

docs_path = repo / "docs/ops/production-configuration.md"
if docs_path.is_file():
    docs = docs_path.read_text(encoding="utf-8")
    for token in [
        "ConnectionStrings:Default",
        "Auth:AccessTokenSecret",
        "Security:TwoFactor:SecretProtectionKey",
        "Security:AllowedOrigins",
        "Security:SecureHeaders",
        "Security:RateLimiting",
        "Security:LoginFailure",
        "FileStorage:Provider",
        "Logging:LogLevel:Default",
        "Database:SeedAdminPassword",
        "VITE_API_BASE_URL",
        "Do not commit production connection strings",
    ]:
        if token not in docs:
            violations.append(f"production configuration docs missing {token}")

template_path = repo / "backend/src/WeCms.Api/appsettings.Production.example.json"
if template_path.is_file():
    try:
        template = json.loads(template_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        violations.append(f"production example json is invalid: {exc}")
        template = {}

    def get(path: str):
        value = template
        for part in path.split(":"):
            if not isinstance(value, dict) or part not in value:
                violations.append(f"production example missing {path}")
                return None
            value = value[part]
        return value

    for path in [
        "ConnectionStrings:Default",
        "Auth:AccessTokenSecret",
        "Security:TwoFactor:SecretProtectionKey",
        "Security:AllowedOrigins",
        "Database:SeedAdminPassword",
        "Security:RateLimiting",
        "Security:SecureHeaders",
        "FileStorage:Provider",
        "Logging:LogLevel",
    ]:
        get(path)

    if get("ConnectionStrings:Default") != "__SET_BY_ENV__":
        violations.append("production example connection string must use __SET_BY_ENV__")
    if get("Auth:AccessTokenSecret") != "__SET_BY_ENV__":
        violations.append("production example auth secret must use __SET_BY_ENV__")
    if get("Security:TwoFactor:SecretProtectionKey") != "__SET_BY_ENV__":
        violations.append("production example 2FA key must use __SET_BY_ENV__")
    if get("Database:SeedAdminPassword") != "__SET_BY_SECRET_MANAGER__":
        violations.append("production example seed password must use __SET_BY_SECRET_MANAGER__")

    origins = get("Security:AllowedOrigins")
    if isinstance(origins, list):
        if not origins:
            violations.append("production example must include at least one allowed origin placeholder")
        for origin in origins:
            if origin == "*" or "localhost" in origin or origin.startswith("http://"):
                violations.append(f"production example contains non-production origin {origin!r}")
            host = urlparse(origin).hostname
            if host:
                try:
                    ipaddress.ip_address(host)
                except ValueError:
                    pass
                else:
                    violations.append(f"production example contains IP origin {origin!r}")

    raw = template_path.read_text(encoding="utf-8")
    forbidden_patterns = [
        r"pwd=(?!__SET_BY_ENV__|__SET_BY_SECRET_MANAGER__)[^;\" ]+",
        r"sk-[A-Za-z0-9]",
        r"AKIA[0-9A-Z]{16}",
        r"Admin@123",
        r"replace-me",
    ]
    for pattern in forbidden_patterns:
        if re.search(pattern, raw):
            violations.append(f"production example contains forbidden secret-like pattern {pattern}")

dev_path = repo / "backend/src/WeCms.Api/appsettings.Development.json"
if dev_path.is_file():
    dev_text = dev_path.read_text(encoding="utf-8")
    if "pwd=replace-me" in dev_text:
        violations.append("Development connection string still contains pwd=replace-me")
    if "pwd=__SET_BY_USER_SECRETS__" not in dev_text:
        violations.append("Development connection string must use pwd=__SET_BY_USER_SECRETS__")

if violations:
    raise SystemExit("check-production-config-baseline: " + "; ".join(violations))

print("check-production-config-baseline: ok")
PY
