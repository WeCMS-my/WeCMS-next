# M1-BE-004 Menu API

## Goal

Implement backend-only menu management APIs under `/api/v1/system/menus`.

## Scope

This task includes:

- Menu list, tree, detail, create, update, delete, enable, and disable APIs.
- Menu service business rules.
- Menu repository port in `WeCms.Modules.System`.
- Menu repository implementation in `WeCms.Persistence`.
- Supporting schema for menu built-in protection and soft delete.
- Permission metadata for every Menu API endpoint.
- JSON source-generation registration.
- Tests for menu business rules, schema smoke, and endpoint/permission source coverage.

This task does not include:

- Permission management APIs.
- SoybeanAdmin generated types or pages.
- Runtime route scanning.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/menus              sys:menu:list
GET    /api/v1/system/menus/tree         sys:menu:tree
GET    /api/v1/system/menus/{id}         sys:menu:detail
POST   /api/v1/system/menus              sys:menu:create
PUT    /api/v1/system/menus/{id}         sys:menu:update
DELETE /api/v1/system/menus/{id}         sys:menu:delete
POST   /api/v1/system/menus/{id}/enable  sys:menu:enable
POST   /api/v1/system/menus/{id}/disable sys:menu:disable
```

## Business Rules

- Menu `code` maps to `sys_menu.name`, is required, trimmed, immutable after create, and unique.
- Menu type must be `catalog`, `menu`, or `button`.
- Parent menu, when present, must exist.
- A menu cannot be its own parent.
- A menu cannot select one of its descendants as parent.
- A menu with child menus cannot be deleted.
- System built-in menus cannot be deleted.
- Menu delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Schema

Add fields to `sys_menu`:

- `is_builtin`
- `deleted_at`

## Acceptance

- Unit tests cover cycle prevention, child-delete prevention, and built-in delete protection.
- Integration migration/seed smoke proves the new menu schema exists.
- Menu endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-005.
