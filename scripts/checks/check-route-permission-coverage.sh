#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STATIC_ROUTES="${ROOT_DIR}/frontend/soybean-admin/src/router/static-routes.ts"
FRONTEND_SRC_DIR="${ROOT_DIR}/frontend/soybean-admin/src"
PERMISSION_SEED="${ROOT_DIR}/database/seeds/000003_seed_m1_system_permissions.sql"
OPENAPI_FILE="${ROOT_DIR}/artifacts/openapi/wecms-api-v1.json"

if [[ ! -f "${STATIC_ROUTES}" ]]; then
  echo "Missing static routes: ${STATIC_ROUTES}" >&2
  exit 1
fi

if [[ ! -d "${FRONTEND_SRC_DIR}" ]]; then
  echo "Missing frontend source directory: ${FRONTEND_SRC_DIR}" >&2
  exit 1
fi

python3 - "$STATIC_ROUTES" "$FRONTEND_SRC_DIR" "$PERMISSION_SEED" "$OPENAPI_FILE" <<'PY'
import re
import sys
from pathlib import Path

static_routes = Path(sys.argv[1])
frontend_src_dir = Path(sys.argv[2])
permission_seed = Path(sys.argv[3])
openapi_file = Path(sys.argv[4])

seed_permissions: set[str] = set()
if permission_seed.is_file():
    seed_permissions = set(re.findall(r"'(sys:[^']+)'", permission_seed.read_text(encoding="utf-8")))

openapi_permissions: set[str] = set()
if openapi_file.is_file():
    openapi_permissions = set(re.findall(r'"x-wecms-permission"\s*:\s*"(sys:[^"]+)"', openapi_file.read_text(encoding="utf-8")))

known_permissions = seed_permissions | openapi_permissions
if not known_permissions:
    raise SystemExit(
        "check-route-permission-coverage: unable to load backend permission fact source from seed or OpenAPI artifact"
    )

route_blocks = re.findall(r'\{\s*path:\s*"(/system/[^"]+)"(.*?)\n  \}', static_routes.read_text(encoding="utf-8"), re.S)
missing_route_permissions: list[str] = []

for route_path, block in route_blocks:
    match = re.search(r'permissions:\s*\[(.*?)\]', block, re.S)
    if match is None:
        missing_route_permissions.append(route_path)
        continue

    route_permissions = re.findall(r'["\'](sys:[^"\']+)["\']', match.group(1))
    if not route_permissions:
        missing_route_permissions.append(route_path)

frontend_permission_usages: dict[str, list[str]] = {}
for path in frontend_src_dir.rglob("*"):
    if not path.is_file():
        continue
    if "service/generated" in path.as_posix():
        continue
    if path.suffix not in {".ts", ".vue"}:
        continue

    text = path.read_text(encoding="utf-8")
    matches = sorted(set(re.findall(r'["\'](sys:[^"\']+)["\']', text)))
    for permission in matches:
        frontend_permission_usages.setdefault(permission, []).append(path.relative_to(frontend_src_dir.parent).as_posix())

unknown_permissions = {
    permission: files
    for permission, files in sorted(frontend_permission_usages.items())
    if permission not in known_permissions
}

if missing_route_permissions:
    print("All /system routes must declare non-empty permission metadata.", file=sys.stderr)
    for route_path in missing_route_permissions:
        print(f"  {route_path}", file=sys.stderr)
    raise SystemExit(1)

if unknown_permissions:
    print(
        "Frontend permission metadata must match backend seed or OpenAPI permissions. Unknown code(s):",
        file=sys.stderr,
    )
    for permission, files in unknown_permissions.items():
        print(f"  {permission}", file=sys.stderr)
        for file in files:
            print(f"    - {file}", file=sys.stderr)
    raise SystemExit(1)

print(
    "check-route-permission-coverage: ok "
    f"({len(known_permissions)} known backend permissions, {len(frontend_permission_usages)} frontend permissions checked)"
)
PY
