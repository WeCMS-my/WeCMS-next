# M1-BE-001 System Permission and Menu Seeds

## Goal

Seed the backend-only M1 system management permission codes and menu entries without adding frontend code or API implementations.

## Scope

This task adds:

- `database/seeds/000003_seed_m1_system_permissions.sql`
- `database/seeds/000004_seed_m1_system_menus.sql`
- `database/seeds/000005_seed_m1_role_permissions.sql`
- integration smoke coverage proving idempotency and `super_admin` permission coverage

This task does not add:

- System management API endpoints
- OpenAPI paths
- frontend generated types or SoybeanAdmin pages
- CMS content permissions or menus
- role-menu assignment, because `sys_role_menu` is not present in the current schema

## Contract

The M1 permission seed must insert all permission codes listed by the M1-BE plan sections 7.1 through 7.10:

- `sys:user:*`
- `sys:role:*`
- `sys:menu:*`
- `sys:permission:*`
- `sys:dept:*`
- `sys:post:*`
- `sys:dict:*`
- `sys:setting:*`
- `sys:login-log:*`
- `sys:audit-log:*`
- `sys:security-event:*`
- `sys:file:*`

The menu seed must create system management menu entries against the current `sys_menu` schema. Because `sys_menu` currently has no `code` column, `name` is the stable menu code and is protected by `ux_sys_menu_name`.

The role-permission seed must grant every current `sys_permission` row to the `super_admin` role and must be idempotent.

## Acceptance

- Running all migrations and seeds twice succeeds.
- M1 permission codes are unique.
- M1 menu names are unique.
- `super_admin` has every seeded M1 permission.
- Existing M0 `sys:system:secure-ping` permission remains present and granted.
- `frontend/**` remains unchanged.
