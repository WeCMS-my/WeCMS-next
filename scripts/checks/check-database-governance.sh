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
    "docs/specs/ph2-database-governance/spec.md",
    "docs/specs/ph2-database-governance/tasks.md",
    "docs/specs/ph2-database-governance/checklist.md",
    "docs/ops/database-production.md",
    "docs/runbooks/database-backup-restore.md",
    "backend/src/WeCms.Api/Extensions/DatabaseMigrationCommand.cs",
    "backend/src/WeCms.Api/Extensions/DatabaseStartupMigrationOptions.cs",
    "backend/src/WeCms.Data.SqlSugar/SqlSugarDataServiceCollectionExtensions.cs",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

database_docs = (repo / "docs/ops/database-production.md").read_text(encoding="utf-8")
for token in [
    "wecms_app",
    "wecms_migration",
    "wecms_backup",
    "ConnectionStrings:Migration",
    "Database:RunMigrationsOnStartup=false",
    "Database:CommandTimeoutSeconds",
    "--migrate",
]:
    if token not in database_docs:
        violations.append(f"database production docs missing {token}")

backup_docs = (repo / "docs/runbooks/database-backup-restore.md").read_text(encoding="utf-8")
for token in ["daily full backup", "7 daily", "4 weekly", "3 monthly", "Restore Drill", "Do not restore over production"]:
    if token not in backup_docs:
        violations.append(f"backup restore runbook missing {token}")

program = (repo / "backend/src/WeCms.Api/Program.cs").read_text(encoding="utf-8")
if "DatabaseMigrationCommand.IsMigrationCommand(args)" not in program:
    violations.append("Program.cs missing explicit migration command detection")
if "useMigrationConnectionString: isMigrationCommand" not in program:
    violations.append("Program.cs must use migration connection string for migration command")
if "DatabaseStartupMigrationOptions.ShouldRunMigrationsOnStartup" not in program:
    violations.append("Program.cs missing configured startup migration policy")

command_source = (repo / "backend/src/WeCms.Api/Extensions/DatabaseMigrationCommand.cs").read_text(encoding="utf-8")
for token in ["--migrate", "IDbMigrationRunner", "ISeedRunner", "Database:SeedAdminPassword", "FindRepoRoot"]:
    if token not in command_source:
        violations.append(f"migration command missing {token}")

data_source = (repo / "backend/src/WeCms.Data.SqlSugar/SqlSugarDataServiceCollectionExtensions.cs").read_text(encoding="utf-8")
for token in ["GetConnectionString(\"Migration\")", "DatabasePlatformOptions", "useMigrationConnectionString"]:
    if token not in data_source:
        violations.append(f"SqlSugar data registration missing {token}")

template = json.loads((repo / "backend/src/WeCms.Api/appsettings.Production.example.json").read_text(encoding="utf-8"))
if template.get("ConnectionStrings", {}).get("Migration") != "__SET_BY_SECRET_MANAGER__":
    violations.append("production template migration connection string must use secret-manager placeholder")
database = template.get("Database") or {}
if database.get("RunMigrationsOnStartup") is not False:
    violations.append("production template must disable RunMigrationsOnStartup")
if database.get("CommandTimeoutSeconds") != 30:
    violations.append("production template must set Database:CommandTimeoutSeconds to 30")

dev_template = json.loads((repo / "backend/src/WeCms.Api/appsettings.Development.example.json").read_text(encoding="utf-8"))
if (dev_template.get("Database") or {}).get("RunMigrationsOnStartup") is not True:
    violations.append("development template must enable RunMigrationsOnStartup")

integration_db = (repo / "backend/tests/WeCms.Tests.Integration/IntegrationTestDatabase.cs").read_text(encoding="utf-8")
if 'AllowedHost = "127.0.0.1"' not in integration_db:
    violations.append("IntegrationTestDatabase default host must be 127.0.0.1")
if 'AllowedHost = "192.168.101.199"' in integration_db:
    violations.append("IntegrationTestDatabase must not default to 192.168.101.199")

forbidden_remote_db = "server=192.168.101." + "199"
forbidden_remote_password = "wecms-dev-" + "123"

for relative in [
    "backend/tests",
    "scripts",
]:
    for path in (repo / relative).rglob("*"):
        if not path.is_file():
            continue
        if path.suffix not in {".cs", ".sh", ".md"}:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        if forbidden_remote_db in text or forbidden_remote_password in text:
            violations.append(f"{path.relative_to(repo)} contains hard-coded remote development DB credentials")

readme = (repo / "README.md").read_text(encoding="utf-8")
if "测试默认白名单也为 `127.0.0.1`" not in readme:
    violations.append("README must document 127.0.0.1 as default integration DB host")

if violations:
    raise SystemExit("check-database-governance: " + "; ".join(violations))

print("check-database-governance: ok")
PY
