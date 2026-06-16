# M1-BE-007 Post API

## Goal

Implement backend-only post management APIs under `/api/v1/system/posts`.

## Scope

This task includes:

- Post list, detail, create, update, delete, enable, and disable APIs.
- Post service business rules.
- Post repository port in `WeCms.Modules.System`.
- Post repository implementation in `WeCms.Persistence`.
- Permission metadata for every Post API endpoint.
- JSON source-generation registration.
- Tests for post business rules, schema smoke, and endpoint/permission source coverage.

This task does not include frontend generated types or SoybeanAdmin pages.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/posts              sys:post:list
GET    /api/v1/system/posts/{id}         sys:post:detail
POST   /api/v1/system/posts              sys:post:create
PUT    /api/v1/system/posts/{id}         sys:post:update
DELETE /api/v1/system/posts/{id}         sys:post:delete
POST   /api/v1/system/posts/{id}/enable  sys:post:enable
POST   /api/v1/system/posts/{id}/disable sys:post:disable
```

## Business Rules

- `page >= 1`.
- `1 <= pageSize <= 100`.
- Post code is required, trimmed, immutable after create, and unique.
- Post name is required and trimmed.
- A post assigned to users cannot be deleted.
- Post delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Acceptance

- Unit tests cover `pageSize` validation, duplicate code validation, and user-assigned delete protection.
- Integration migration/seed smoke proves `sys_post` exists.
- Post endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-008.
