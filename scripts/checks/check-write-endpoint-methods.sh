#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
openapi_path="${1:-$repo_root/artifacts/openapi/wecms-api-v1.json}"

python3 - "$openapi_path" <<'PY'
import json
import re
import sys
from pathlib import Path

path = Path(sys.argv[1])
if not path.is_file():
    raise SystemExit(f"check-write-endpoint-methods: missing {path}")

document = json.loads(path.read_text(encoding="utf-8"))
paths = document.get("paths", {})

mutating_words = re.compile(
    r"(^|/|-)(create|update|delete|remove|enable|disable|reset|assign|unban|batch-unban|upload|revoke|restore|lock|unlock)($|/|-)",
    re.IGNORECASE,
)

violations: list[str] = []
for route, operations in sorted(paths.items()):
    if not isinstance(operations, dict):
        continue

    get_operation = operations.get("get")
    if get_operation is None:
        continue

    if "requestBody" in get_operation:
        violations.append(f"GET {route} declares requestBody")

    if mutating_words.search(route):
        violations.append(f"GET {route} looks like a write operation")

if violations:
    raise SystemExit("check-write-endpoint-methods: " + "; ".join(violations))

print("check-write-endpoint-methods: ok")
PY
