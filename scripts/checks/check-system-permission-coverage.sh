#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import re
import sys
from pathlib import Path

repo = Path(sys.argv[1])
seed = (repo / "database" / "seeds" / "000003_seed_m1_system_permissions.sql").read_text(encoding="utf-8")
role_seed = (repo / "database" / "seeds" / "000005_seed_m1_role_permissions.sql").read_text(encoding="utf-8")

expected = {
    "sys:user:page", "sys:user:list", "sys:user:detail", "sys:user:create", "sys:user:update", "sys:user:delete", "sys:user:enable", "sys:user:disable", "sys:user:reset-password", "sys:user:assign-role", "sys:user:assign-post",
    "sys:role:page", "sys:role:list", "sys:role:detail", "sys:role:create", "sys:role:update", "sys:role:delete", "sys:role:enable", "sys:role:disable", "sys:role:assign-permission", "sys:role:assign-menu",
    "sys:menu:page", "sys:menu:list", "sys:menu:tree", "sys:menu:detail", "sys:menu:create", "sys:menu:update", "sys:menu:delete", "sys:menu:enable", "sys:menu:disable",
    "sys:permission:page", "sys:permission:list", "sys:permission:tree", "sys:permission:detail", "sys:permission:create", "sys:permission:update", "sys:permission:delete", "sys:permission:enable", "sys:permission:disable",
    "sys:dept:page", "sys:dept:list", "sys:dept:tree", "sys:dept:detail", "sys:dept:create", "sys:dept:update", "sys:dept:delete", "sys:dept:enable", "sys:dept:disable",
    "sys:post:page", "sys:post:list", "sys:post:detail", "sys:post:create", "sys:post:update", "sys:post:delete", "sys:post:enable", "sys:post:disable",
    "sys:dict:page", "sys:dict:type:list", "sys:dict:type:create", "sys:dict:type:update", "sys:dict:type:delete", "sys:dict:value:list", "sys:dict:value:create", "sys:dict:value:update", "sys:dict:value:delete",
    "sys:setting:page", "sys:setting:list", "sys:setting:detail", "sys:setting:update",
    "sys:login-log:page", "sys:login-log:list", "sys:login-log:detail",
    "sys:audit-log:page", "sys:audit-log:list", "sys:audit-log:detail",
    "sys:security-event:page", "sys:security-event:list", "sys:security-event:detail",
    "sys:file:page", "sys:file:list", "sys:file:detail", "sys:file:upload", "sys:file:delete",
}

actual = set(re.findall(r"'(sys:[^']+)'", seed))
missing = sorted(expected - actual)
if missing:
    raise SystemExit("check-system-permission-coverage: missing permission seed(s): " + ", ".join(missing))

if len(expected) != len(actual & expected):
    raise SystemExit("check-system-permission-coverage: duplicate or unexpected expected permission accounting")

required_role_seed_fragments = ["JOIN sys_permission p", "WHERE r.code = 'super_admin'", "WHERE rp.role_id = r.id"]
for fragment in required_role_seed_fragments:
    if fragment not in role_seed:
        raise SystemExit(f"check-system-permission-coverage: role permission seed missing {fragment}")

if "p.code IN" in role_seed:
    raise SystemExit("check-system-permission-coverage: super_admin seed must grant all current permissions without a static IN list")

print("check-system-permission-coverage: ok")
PY
