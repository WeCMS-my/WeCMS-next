#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import re
import sys
from pathlib import Path

repo = Path(sys.argv[1])
role_migration = (repo / "database" / "migrations" / "000007_m1_role_management.sql").read_text(encoding="utf-8")
super_admin_seed = (repo / "database" / "seeds" / "000002_seed_super_admin.sql").read_text(encoding="utf-8")
role_permission_seed = (repo / "database" / "seeds" / "000005_seed_m1_role_permissions.sql").read_text(encoding="utf-8")
role_dtos = (repo / "backend" / "src" / "WeCms.Modules.System" / "Roles" / "RoleDtos.cs").read_text(encoding="utf-8")
role_repository = (repo / "backend" / "src" / "WeCms.Persistence" / "Modules" / "System" / "Roles" / "RoleRepository.cs").read_text(encoding="utf-8")
user_service = (repo / "backend" / "src" / "WeCms.Modules.System" / "Users" / "UserService.cs").read_text(encoding="utf-8")

required_fragments = {
    "migration adds is_locked default false": "ADD COLUMN is_locked BOOLEAN NOT NULL DEFAULT FALSE" in role_migration,
    "super_admin insert includes is_locked": "INSERT INTO sys_role (code, name, status, is_builtin, is_locked" in super_admin_seed,
    "super_admin insert sets is_locked true": "SELECT 'super_admin', 'Super Administrator', 'enabled', TRUE, TRUE" in super_admin_seed,
    "super_admin seed updates is_locked true": "is_locked = TRUE" in super_admin_seed,
    "admin user role seed joins super_admin": "JOIN sys_role r ON r.code = 'super_admin'" in super_admin_seed,
    "super_admin role permission seed grants all permissions": "JOIN sys_permission p" in role_permission_seed,
    "role summary exposes IsLocked": re.search(r"RoleSummaryDto\([^)]*bool\s+IsLocked", role_dtos, re.S) is not None,
    "role detail exposes IsLocked": re.search(r"RoleDetailDto\([^)]*bool\s+IsLocked", role_dtos, re.S) is not None,
    "role repository reads is_locked": "r.is_locked AS IsLocked" in role_repository,
    "role repository creates unlocked roles explicitly": "is_builtin, is_locked" in role_repository and "FALSE, FALSE" in role_repository,
    "user service protects locked holder invariant": "Locked role must have at least one enabled user." in user_service,
}

missing = [name for name, ok in required_fragments.items() if not ok]
if missing:
    raise SystemExit("check-locked-role-seed: missing required locked-role invariant(s): " + ", ".join(missing))

if "p.code IN" in role_permission_seed:
    raise SystemExit("check-locked-role-seed: super_admin role permission seed must not use a static permission IN list")

for request_name in ("CreateRoleRequest", "UpdateRoleRequest"):
    match = re.search(rf"public sealed record {request_name}\((.*?)\);", role_dtos, re.S)
    if match is None:
        raise SystemExit(f"check-locked-role-seed: {request_name} record was not found")
    body = match.group(1)
    if re.search(r"\bIsLocked\b|\bisLocked\b|\bIsBuiltin\b|\bisBuiltin\b", body):
        raise SystemExit(f"check-locked-role-seed: {request_name} must not expose isLocked/isBuiltin")

print("check-locked-role-seed: ok")
PY
