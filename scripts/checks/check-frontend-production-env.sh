#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import re
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/specs/ph6-frontend-production/spec.md",
    "docs/specs/ph6-frontend-production/tasks.md",
    "docs/specs/ph6-frontend-production/checklist.md",
    "docs/ops/frontend-production.md",
    "frontend/soybean-admin/.env.production.example",
    "frontend/soybean-admin/tests/production-env.test.mjs",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""

env_example = read("frontend/soybean-admin/.env.production.example")
match = re.search(r"^VITE_API_BASE_URL=(.*)$", env_example, re.MULTILINE)
if not match:
    violations.append(".env.production.example missing VITE_API_BASE_URL")
else:
    api_base = match.group(1).strip()
    if not api_base.startswith("https://"):
        violations.append("production API base example must use HTTPS")
    if "localhost" in api_base.lower() or "127.0.0.1" in api_base or "[::1]" in api_base:
        violations.append("production API base example must not use localhost")
    if api_base.startswith("http://"):
        violations.append("production API base example must not use HTTP")

request = read("frontend/soybean-admin/src/api/request.ts")
for token in [
    "same-origin",
    'import.meta.env.VITE_API_BASE_URL ?? ""',
    "handleTerminalUnauthorized",
    "clientSafeErrorResult",
    "response.status === 403",
    "response.status === 429",
    "response.status >= 500",
    "无权限访问。",
    "请求过于频繁，请稍后再试。",
    "系统异常，请稍后再试。",
]:
    if token not in request:
        violations.append(f"request.ts missing {token}")

package = read("frontend/soybean-admin/package.json")
if "tests/production-env.test.mjs" not in package:
    violations.append("frontend test:config must include production-env.test.mjs")

frontend_gate = read("scripts/quality-gate-frontend.sh")
if "check-frontend-production-env.sh" not in frontend_gate:
    violations.append("frontend gate must run check-frontend-production-env.sh")

docs = read("docs/ops/frontend-production.md")
for token in ["Same-origin mode", "Split-domain mode", "HTTPS", "401", "403", "429", "5xx"]:
    if token not in docs:
        violations.append(f"frontend production docs missing {token}")

if violations:
    print("check-frontend-production-env: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-frontend-production-env: ok")
PY
