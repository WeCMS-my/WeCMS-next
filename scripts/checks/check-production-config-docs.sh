#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/ops/production-configuration.md",
    "docs/ops/security-baseline.md",
    "docs/ops/deployment-reverse-proxy.md",
    "docs/ops/database-production.md",
    "docs/ops/logging-observability.md",
    "docs/ops/security-alerting.md",
    "docs/ops/file-storage-production.md",
    "docs/ops/frontend-production.md",
    "docs/runbooks/database-backup-restore.md",
    "docs/runbooks/release-checklist.md",
    "docs/runbooks/rollback.md",
    "docs/runbooks/incident-response.md",
    "docs/reports/wecms-production-hardening-final-acceptance.md",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""

config = read("docs/ops/production-configuration.md")
for token in [
    "ConnectionStrings:Default",
    "Auth:AccessTokenSecret",
    "Security:TwoFactor:SecretProtectionKey",
    "Security:AllowedOrigins",
    "FileStorage:Provider",
    "VITE_API_BASE_URL",
    "Database:SeedAdminPassword",
]:
    if token not in config:
        violations.append(f"production configuration docs missing {token}")

acceptance = read("docs/reports/wecms-production-hardening-final-acceptance.md")
for token in ["PH-0", "PH-1", "PH-2", "PH-3", "PH-4", "PH-5", "PH-6", "PH-7", "Production readiness gate", "Final Decision"]:
    if token not in acceptance:
        violations.append(f"final acceptance report missing {token}")

readme = read("README.md")
for relative in required_files:
    if relative not in readme and relative not in {
        "docs/ops/deployment-reverse-proxy.md",
        "docs/ops/security-alerting.md",
    }:
        violations.append(f"README missing link to {relative}")

if violations:
    print("check-production-config-docs: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-production-config-docs: ok")
PY
