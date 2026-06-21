# S6 Organization Migration Tasks

Scope summary: migrate Departments and system job Positions into the Organization module boundary, rename legacy system Posts to Positions, and expose an Organization lookup abstraction for Identity.

## S6-T00 Spec Trio

Add the S6 Organization migration spec trio before production code changes.

## S6-T01 Rename Posts To Positions

Rename system-management Posts to Positions across backend source, tests, seed data, OpenAPI metadata, route coverage, audit coverage, permission coverage, and database table names.

This task must not migrate Department code. It should preserve the existing position CRUD behavior while replacing legacy naming.

Required proof includes:

- `rg "sys_post|sys_user_post|UserPost|PostService|IPostRepository|PostRepository|PostPermissions|CreatePostRequest|UpdatePostRequest|/system/posts" backend/src backend/tests database scripts` returns no system-position residuals.
- `PositionServiceTests` cover existing position business behavior.
- `PositionRepositoryIntegrationTests` cover persisted position data and user-position assignment table usage.
- Backend quality gate passes.

## S6-T02 Department Migration

Move Department DTOs, records, permissions, service, repository interface, and tree builder behavior into `WeCms.Modules.Organization`.

Move `DepartmentRepository` into `WeCms.Modules.Organization.SqlSugar`, preserve delete dependency checks, and keep audit metadata and endpoint permission metadata intact.

Required proof includes:

- `DepartmentServiceTests`
- `DepartmentTreeTests`
- `DepartmentRepositoryIntegrationTests`
- `DepartmentEndpointPermissionTests`
- backend boundary scripts for layer, DB, DI, permissions, and audit coverage

## S6-T03 Position Migration

Move Position DTOs, records, permissions, service, repository interface, and enable/disable/delete logic into `WeCms.Modules.Organization`.

Move `PositionRepository` into `WeCms.Modules.Organization.SqlSugar` and remove old System/Persistence registrations for position services and repositories.

Required proof includes:

- `PositionServiceTests`
- `PositionRepositoryIntegrationTests`
- backend boundary scripts for layer, DB, DI, permissions, and audit coverage

## S6-T04 OrganizationLookupService

Add `IOrganizationLookupService` in `WeCms.Modules.Organization` for department existence and batch position ID existence checks.

Identity user create/update must use this abstraction and must not depend on Organization repositories or SqlSugar implementations.

Required proof includes:

- `IdentityUserService_UsesOrganizationLookup`
- `OrganizationLookupServiceTests`
- `LayerDependencyTests`
- final S6 checklist and total audit
