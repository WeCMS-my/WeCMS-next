# M1-BE-008 Dict API

## Goal

Implement backend-only dictionary type and dictionary value APIs.

## Scope

This task includes:

- Dictionary type page, detail, create, update, and delete APIs.
- Dictionary value list by type code, create by type code, update, and delete APIs.
- Dictionary service business rules.
- Dictionary repository port in `WeCms.Modules.System`.
- Dictionary repository implementation in `WeCms.Persistence`.
- Schema for `sys_dict_type` and `sys_dict_value`.
- Permission metadata for every Dict API endpoint.
- JSON source-generation registration.
- Tests for dictionary business rules, schema smoke, and endpoint/permission source coverage.

This task does not include frontend generated types or SoybeanAdmin pages.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/dict-types                sys:dict:type:list
GET    /api/v1/system/dict-types/{id}           sys:dict:type:list
POST   /api/v1/system/dict-types                sys:dict:type:create
PUT    /api/v1/system/dict-types/{id}           sys:dict:type:update
DELETE /api/v1/system/dict-types/{id}           sys:dict:type:delete
GET    /api/v1/system/dict-types/{typeCode}/values sys:dict:value:list
POST   /api/v1/system/dict-types/{typeCode}/values sys:dict:value:create
PUT    /api/v1/system/dict-values/{id}          sys:dict:value:update
DELETE /api/v1/system/dict-values/{id}          sys:dict:value:delete
```

## Business Rules

- `page >= 1`.
- `1 <= pageSize <= 100`.
- Dict type code is required, trimmed, immutable after create, and unique.
- Dict type name is required and trimmed.
- System dict types cannot be deleted.
- Dict type delete is soft delete and rejected when active values exist.
- Dict value must belong to an existing dict type.
- Dict value `value` is required and unique within the same dict type.
- Dict value delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Schema

Add:

- `sys_dict_type`
- `sys_dict_value`

## Acceptance

- Unit tests cover system dict type delete protection, duplicate type code validation, and duplicate value validation within type.
- Integration migration smoke proves the dict schema exists.
- Dict endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-009.
