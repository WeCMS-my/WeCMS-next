# M1-BE-005 Permission API

## Goal

Implement backend-only permission management APIs under `/api/v1/system/permissions`.

## Scope

This task includes:

- Permission list, tree, detail, create, update, delete, enable, and disable APIs.
- Permission service business rules.
- Extending the existing permission repository port in `WeCms.Modules.System`.
- Extending the existing permission repository implementation in `WeCms.Persistence`.
- Supporting schema for permission status, built-in protection, and soft delete.
- Permission metadata for every Permission API endpoint.
- JSON source-generation registration.
- Tests for permission business rules, schema smoke, and endpoint/permission source coverage.

This task does not include:

- Role assignment APIs beyond the existing Role API.
- Frontend generated types or SoybeanAdmin pages.
- Runtime endpoint scanning.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/permissions              sys:permission:list
GET    /api/v1/system/permissions/tree         sys:permission:tree
GET    /api/v1/system/permissions/{id}         sys:permission:detail
POST   /api/v1/system/permissions              sys:permission:create
PUT    /api/v1/system/permissions/{id}         sys:permission:update
DELETE /api/v1/system/permissions/{id}         sys:permission:delete
POST   /api/v1/system/permissions/{id}/enable  sys:permission:enable
POST   /api/v1/system/permissions/{id}/disable sys:permission:disable
```

## Business Rules

- Permission code is required, trimmed, immutable after create, and unique.
- Permission module is required and trimmed.
- Permission name is required and trimmed.
- System built-in permissions cannot be deleted.
- Permission delete is soft delete.
- Permissions already bound to a role cannot be hard-deleted; this API never hard-deletes.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Schema

Add fields to `sys_permission`:

- `status`
- `is_builtin`
- `deleted_at`

## Acceptance

- Unit tests cover built-in delete protection, role-bound soft delete behavior, and code uniqueness validation.
- Integration migration/seed smoke proves the new permission schema exists.
- Permission endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-006.
