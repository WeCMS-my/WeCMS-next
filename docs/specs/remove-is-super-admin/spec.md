# Remove isSuperAdmin Field

## Goal

Remove the `isSuperAdmin` / `IsSuperAdmin` / `is_super_admin` user flag from active WeCMS runtime code, database baseline, seeds, OpenAPI contract, generated frontend types, and UI usage.

All users, including the seeded administrator, must receive access only through role and permission assignments.

## Scope

- Remove `sys_user.is_super_admin` from the reset database baseline and seed SQL.
- Remove `IsSuperAdmin` from Identity records, DTOs, OpenAPI schema, and frontend generated types.
- Remove access-profile branching based on a super-admin flag.
- Make visible menus derive from role-menu assignments only.
- Replace security-ban self-unban special handling with role/permission based lookup.
- Keep the `super_admin` role code and locked-role invariant.

## Non-Goals

- Do not remove the `super_admin` role.
- Do not weaken endpoint permission metadata checks.
- Do not introduce compatibility fallbacks for the removed field.
- Do not add Controller, EF Core, dynamic SQL, or frontend-side authorization bypasses.

## Acceptance Criteria

- No active production source, OpenAPI artifact, generated frontend type, DB baseline, or seed contains `isSuperAdmin`, `IsSuperAdmin`, or `is_super_admin`.
- Endpoint permission checks continue to use `sys_user_role`, `sys_role_permission`, and `sys_permission`.
- Access profile menus are resolved from role-menu assignments.
- The seeded `admin` user gets privileges only through the `super_admin` role assignment.
- Tests prove the forbidden field cannot be reintroduced.
