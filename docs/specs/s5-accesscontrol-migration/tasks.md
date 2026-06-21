# S5 AccessControl Migration Tasks

Scope summary: migrate Roles, Permissions, Menus, PermissionDefinition, URL permission, button permission, and AccessProfile capabilities under the AccessControl module boundary.

## S5-T00 Spec Trio

Add the S5 AccessControl migration spec trio before production code changes.

## S5-T01 Role / Permission / Menu DTO And Records

Move Role, Permission, and Menu DTOs and records into `WeCms.Modules.AccessControl`, update namespaces and OpenAPI schemas, and remove old System DTO/record usings.

## S5-T02 PermissionDefinition Model

Establish `PermissionDefinition`, `PermissionGroupDefinition`, `PermissionKind`, `PermissionAction`, `PermissionDefinitionProvider`, and `PermissionRegistry` with duplicate-code, code-format, module, kind, and action validation.

## S5-T03 URL Permission Model

Establish URL permission binding and endpoint metadata export while keeping public route paths and OpenAPI `x-wecms-permission` values stable.

## S5-T04 Button Permission Model

Establish button permission definitions and the AccessProfile return shape for button permissions without implementing frontend behavior.

## S5-T05 Role Service And Repository Migration

Move Role services, repository interfaces, and SqlSugar implementations into AccessControl boundaries while preserving locked role protection, permission version bump, and audit abstraction.

## S5-T06 Permission Service And Repository Migration

Move Permission checker, endpoint filter, management services, repository interfaces, and SqlSugar implementations into AccessControl boundaries while preserving permission denied security event recording.

## S5-T07 Menu Service And Repository Migration

Move Menu services, tree builder, repository interfaces, and SqlSugar implementations into AccessControl boundaries while preserving menu tree behavior and menu permission bindings.

## S5-T08 AccessProfile Service

Add `IAccessProfileService` and move AccessProfile role, permission, menu, button, and permission version composition into AccessControl so Identity consumes only the abstraction.
