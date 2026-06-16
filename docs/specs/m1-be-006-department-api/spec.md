# M1-BE-006 Department API

## Goal

Implement backend-only department management APIs under `/api/v1/system/depts`.

## Scope

This task includes:

- Department list, tree, detail, create, update, delete, enable, and disable APIs.
- Department service business rules.
- Department repository port in `WeCms.Modules.System`.
- Department repository implementation in `WeCms.Persistence`.
- Permission metadata for every Department API endpoint.
- JSON source-generation registration.
- Tests for department business rules, schema smoke, and endpoint/permission source coverage.

This task does not include:

- User API changes beyond checking whether users are assigned to a department.
- Frontend generated types or SoybeanAdmin pages.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/depts              sys:dept:list
GET    /api/v1/system/depts/tree         sys:dept:tree
GET    /api/v1/system/depts/{id}         sys:dept:detail
POST   /api/v1/system/depts              sys:dept:create
PUT    /api/v1/system/depts/{id}         sys:dept:update
DELETE /api/v1/system/depts/{id}         sys:dept:delete
POST   /api/v1/system/depts/{id}/enable  sys:dept:enable
POST   /api/v1/system/depts/{id}/disable sys:dept:disable
```

## Business Rules

- Department code is required, trimmed, immutable after create, and unique.
- Department name is required and trimmed.
- Parent department, when present, must exist.
- A department cannot be its own parent.
- A department cannot select one of its descendants as parent.
- A department with child departments cannot be deleted.
- A department assigned to users cannot be deleted.
- Department delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Acceptance

- Unit tests cover cycle prevention, child-delete prevention, and user-assigned delete protection.
- Integration migration/seed smoke proves `sys_dept` exists.
- Department endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-007.
