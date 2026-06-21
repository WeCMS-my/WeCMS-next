# S5 AccessControl Migration Spec

## Goal

Migrate Roles, Permissions, and Menus from the transitional `WeCms.Modules.System` namespace into `WeCms.Modules.AccessControl` and `WeCms.Modules.AccessControl.SqlSugar`, while upgrading the permission platform to support RBAC, URL permission bindings, button permission definitions, and AccessProfile data for Identity.

## Scope

- `WeCms.Modules.AccessControl` owns Role, Permission, and Menu DTOs, records, service abstractions, services, endpoint definitions, permission definitions, permission endpoint metadata integration, AccessProfile abstractions, and repository interfaces.
- `WeCms.Modules.AccessControl.SqlSugar` owns Role, Permission, Menu, permission binding, button permission, permission version, and permission security event persistence implementations.
- Existing HTTP routes and existing permission code strings remain stable unless a later task explicitly changes the public contract.
- OpenAPI must keep accurate `x-wecms-permission` metadata for protected endpoints.
- Identity may depend only on AccessControl contracts needed for AccessProfile; it must not depend on concrete AccessControl or SqlSugar implementations.

## Non-Goals

- Do not migrate Organization, Configuration, Audit, Security, FileCenter, Platform, or CMS in S5.
- Do not change existing permission code strings such as `sys:role:*`, `sys:permission:*`, or `sys:menu:*`.
- Do not introduce MVC Controller, Razor, runtime endpoint scanning, EF Core, dynamic query/return types, or AI runtime capability.
- Do not rewrite authentication, token, 2FA, or Identity user CRUD semantics beyond calling the AccessProfile abstraction when that S5 task is reached.
- Do not remove `WeCms.Persistence` in S5; repository implementations move only for AccessControl scope.

## Acceptance

- Roles, Permissions, and Menus contracts and records live under `WeCms.Modules.AccessControl`.
- Role, Permission, and Menu application services depend on interfaces and do not reference SqlSugar, MySqlConnector, `DbConnection`, or `DbTransaction`.
- Role, Permission, and Menu endpoints are registered through explicit Minimal API endpoint definitions and carry permission, audit, rate-limit, and OpenAPI metadata.
- Repository interfaces live in `WeCms.Modules.AccessControl.Repositories`; implementations live in `WeCms.Modules.AccessControl.SqlSugar`.
- `PermissionDefinition`, `PermissionGroupDefinition`, `PermissionKind`, `PermissionAction`, `PermissionDefinitionProvider`, and `PermissionRegistry` exist with duplicate-code and code-format validation.
- URL permission binding can be generated from endpoint metadata and remains reflected in OpenAPI `x-wecms-permission`.
- Button permission definitions can be modeled and returned through AccessProfile.
- `IAccessProfileService` returns roles, permissions, menus, buttons, and permission version for Identity consumers.
- Full backend quality gate and S5-focused audits pass after each completed task.
