# M1-BE-003 Role API

## Goal

Implement backend-only role management APIs under `/api/v1/system/roles`.

## Scope

This task includes:

- Role list, detail, create, update, delete, enable, disable, assign permissions, and assign menus APIs.
- Role service business rules.
- Role repository port in `WeCms.Modules.System`.
- Role repository implementation in `WeCms.Persistence`.
- Supporting schema for role built-in protection, soft delete, and role-menu assignments.
- Permission metadata for every Role API endpoint.
- JSON source-generation registration.
- Tests for role business rules, schema smoke, and endpoint/permission source coverage.

This task does not include:

- Menu management APIs.
- Permission management APIs.
- Department or post management APIs.
- Frontend generated types or SoybeanAdmin pages.
- CMS content APIs.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/roles                    sys:role:list
GET    /api/v1/system/roles/{id}               sys:role:detail
POST   /api/v1/system/roles                    sys:role:create
PUT    /api/v1/system/roles/{id}               sys:role:update
DELETE /api/v1/system/roles/{id}               sys:role:delete
POST   /api/v1/system/roles/{id}/enable        sys:role:enable
POST   /api/v1/system/roles/{id}/disable       sys:role:disable
PUT    /api/v1/system/roles/{id}/permissions   sys:role:assign-permission
PUT    /api/v1/system/roles/{id}/menus         sys:role:assign-menu
```

List responses use the standard page shape:

```json
{
  "records": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

## Business Rules

- `page >= 1`.
- `1 <= pageSize <= 100`.
- Role code is required, trimmed, immutable after create, and unique.
- Role name is required and trimmed.
- System built-in roles cannot be deleted.
- `super_admin` cannot be deleted.
- `super_admin` cannot be disabled.
- Permission and menu assignments accept positive ids only, deduplicate ids, and reject unknown ids.
- A role cannot remove the last active `super_admin` role's critical permissions.
- Role delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Schema

Add fields to `sys_role`:

- `is_builtin`
- `deleted_at`

Add supporting M1 role assignment table:

- `sys_role_menu`

## Acceptance

- Unit tests cover `super_admin` delete/disable protection.
- Integration migration/seed smoke proves the new role schema exists.
- Role endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-004.
