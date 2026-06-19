#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/specs/ph5-release-runbook/spec.md",
    "docs/specs/ph5-release-runbook/tasks.md",
    "docs/specs/ph5-release-runbook/checklist.md",
    "docs/runbooks/release-checklist.md",
    "docs/runbooks/rollback.md",
    "docs/runbooks/incident-response.md",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""

release = read("docs/runbooks/release-checklist.md")
release_tokens = [
    "Current commit SHA",
    "Current tag",
    "backend",
    "frontend",
    "Database backup completed",
    "Migration plan reviewed",
    "environment variables reviewed",
    "Secrets verified",
    "/health/ready",
    "smoke admin login",
    "Rollback target",
    "Release owner",
    "Release timestamp",
    "Backend gate result",
    "Frontend gate result",
    "Appsettings / env review result",
    "Secrets verification result",
    "`/health/live` result",
    "`/health/ready` result",
    "`/health/dependencies` result",
    "Smoke admin login result",
    "Final go / no-go decision",
    "Known residual risks",
    "Go / No-Go",
    "docs/runbooks/database-backup-restore.md",
    "docs/runbooks/rollback.md",
]
for token in release_tokens:
    if token not in release:
        violations.append(f"release checklist missing {token}")

rollback = read("docs/runbooks/rollback.md")
rollback_tokens = [
    "Application Rollback",
    "Database Rollback",
    "migration failed",
    "Configuration Rollback",
    "File Storage Rollback",
    "DNS / Proxy Rollback",
    "/health/ready",
    "Smoke admin login",
    "Audit",
    "Do not restore over production",
    "explicit human approval",
]
for token in rollback_tokens:
    if token not in rollback:
        violations.append(f"rollback runbook missing {token}")

incident = read("docs/runbooks/incident-response.md")
incident_sections = [
    "Login Brute Force",
    "Refresh Token Reuse",
    "Permission Anomaly",
    "DB Connection Exhaustion",
    "File Upload Anomaly",
    "Disk Space Low",
    "Migration Failure",
    "Frontend Contract Mismatch",
    "API 5xx Spike",
]
for section in incident_sections:
    if section not in incident:
        violations.append(f"incident response missing {section}")

for token in ["Triage:", "Containment:", "Recovery:", "Postmortem:", "sys_security_event", "Audit logs", "/health/dependencies"]:
    if token not in incident:
        violations.append(f"incident response missing {token}")

readme = read("README.md")
for token in ["docs/runbooks/release-checklist.md", "docs/runbooks/rollback.md", "docs/runbooks/incident-response.md"]:
    if token not in readme:
        violations.append(f"README missing {token}")

combined = release + rollback + incident
for forbidden in ["password=", "pwd=wecms", "sk-", "ghp_", "AKIA"]:
    if forbidden in combined:
        violations.append(f"runbooks must not contain secret-like token {forbidden}")

if violations:
    print("check-release-runbooks: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-release-runbooks: ok")
PY
