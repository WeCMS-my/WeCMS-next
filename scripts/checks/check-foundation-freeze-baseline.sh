#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

baseline_path = repo / "docs/releases/WeCMS-next H3 基础系统冻结基线.md"
if not baseline_path.is_file():
    violations.append("missing H3 foundation freeze baseline document")
else:
    text = baseline_path.read_text(encoding="utf-8")
    for token in [
        "H3 总体验收",
        "不启动 CMS 二期",
        "禁止",
        "AI runtime",
        "旧 ThinkPHP runtime compatibility",
        "复制旧 AdminGate",
        "artifacts/openapi/wecms-api-v1.json",
        "docs/specs/h3-final-acceptance/{spec.md,tasks.md,checklist.md}",
        "database/migrations/000001_init_identity.sql",
        "database/migrations/000019_h2_security_event_classifier.sql",
        "database/seeds/000001_seed_base_permissions.sql",
        "database/seeds/000010_seed_h2_setting_hardening_permissions.sql",
        "scripts/quality-gate-backend.sh",
        "scripts/quality-gate-frontend.sh",
        "H3-001",
        "H3-010",
        "WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend bash scripts/quality-gate-backend.sh",
        "bash scripts/quality-gate-frontend.sh",
        "未创建 Git tag",
        "生产 `Security:AllowedOrigins`",
    ]:
        if token not in text:
            violations.append(f"freeze baseline missing token {token!r}")

required_paths = [
    "artifacts/openapi/wecms-api-v1.json",
    "docs/specs/h3-final-acceptance/spec.md",
    "docs/specs/h3-final-acceptance/tasks.md",
    "docs/specs/h3-final-acceptance/checklist.md",
    "database/migrations/000001_init_identity.sql",
    "database/migrations/000019_h2_security_event_classifier.sql",
    "database/seeds/000001_seed_base_permissions.sql",
    "database/seeds/000010_seed_h2_setting_hardening_permissions.sql",
    "scripts/quality-gate-backend.sh",
    "scripts/quality-gate-frontend.sh",
    "scripts/checks/check-cookie-auth-origin-protection.sh",
    "scripts/checks/check-admingate-csrf-migration.sh",
    "scripts/checks/check-thinkphp-feature-delta.sh",
]

for relative_path in required_paths:
    if not (repo / relative_path).is_file():
        violations.append(f"freeze baseline required artifact missing: {relative_path}")

cms_module_files = [
    path.relative_to(repo).as_posix()
    for path in (repo / "backend/src/WeCms.Modules.Cms").rglob("*")
    if path.is_file()
    and "bin" not in path.relative_to(repo / "backend/src/WeCms.Modules.Cms").parts
    and "obj" not in path.relative_to(repo / "backend/src/WeCms.Modules.Cms").parts
    and path.name not in {"AssemblyMarker.cs", "WeCms.Modules.Cms.csproj"}
]
if cms_module_files:
    violations.append("CMS module contains phase-two implementation files: " + ", ".join(cms_module_files))

if violations:
    raise SystemExit("check-foundation-freeze-baseline: " + "; ".join(violations))

print("check-foundation-freeze-baseline: ok")
PY
