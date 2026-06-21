# S6 Organization Migration Spec

## Goal

Migrate organization-domain system administration capabilities from the transitional `WeCms.Modules.System` and `WeCms.Persistence` boundaries into `WeCms.Modules.Organization` and `WeCms.Modules.Organization.SqlSugar`.

Sprint 6 owns Departments and system job positions. The legacy system-management `Post` naming represents organization positions, not CMS content posts. S6 must rename this domain from Posts to Positions across backend source, tests, seeds, OpenAPI metadata, route coverage, audit coverage, and database table names.

## Source Of Truth

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` is the execution plan for sprint numbering.
- Development-plan S6 is Organization migration.
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md` contains older sprint-numbering sections: technical-book Sprint 4 maps to development-plan S6 Organization, technical-book Sprint 5 maps to development-plan S7 Configuration, and technical-book Sprint 6 maps to development-plan S8 Audit/Security/FileCenter/Platform.
- When numbering conflicts, execute the development plan order: S6 Organization, S7 Configuration, S8 Audit/Security/FileCenter/Platform.

## Scope

- `WeCms.Modules.Organization` owns Department and Position DTOs, records, permissions, services, repository interfaces, lookup abstractions, and any organization endpoint definitions introduced during migration.
- `WeCms.Modules.Organization.SqlSugar` owns Department and Position repository implementations and organization CodeFirst model registration.
- Existing Department behavior, tree construction, status changes, delete dependency checks, audit metadata, permission metadata, and OpenAPI coverage must remain intact.
- System job Posts must be renamed to Positions:
  - `PostDtos` to `PositionDtos`
  - `PostService` to `PositionService`
  - `IPostRepository` to `IPositionRepository`
  - `PostRepository` to `PositionRepository`
  - `PostPermissions` to `PositionPermissions`
  - `/api/v1/system/posts` to `/api/v1/system/positions`
  - `sys_post` to `sys_position`
  - `sys_user_post` to `sys_user_position`
- Identity may depend on an Organization lookup abstraction for department and position existence checks. Identity must not depend on Organization repository implementations or `WeCms.Modules.Organization.SqlSugar`.
- Public contract changes must be reflected in OpenAPI, generated schemas, endpoint coverage tests, permission coverage scripts, audit coverage scripts, and seed data.

## Non-Goals

- Do not migrate Configuration, Audit, Security, FileCenter, Platform, CMS, caching, AOP, EventBus, or Outbox in S6.
- Do not implement CMS content Post APIs; any `Post` naming allowed after S6 must be explicitly scoped to a future CMS content domain and should not appear in current system organization source.
- Do not remove `WeCms.Modules.System` or `WeCms.Persistence` globally in S6; remove only Organization-owned service/repository registrations and source from those boundaries.
- Do not introduce MVC Controller, Razor, runtime endpoint scanning, EF Core, dynamic query/return types, silent legacy fallback, or AI runtime capability.
- Do not change authentication, token, 2FA, or AccessControl semantics except for Identity user create/update validation through the Organization lookup abstraction.

## Acceptance

- `docs/specs/s6-organization-migration/{spec.md,tasks.md,checklist.md}` exists before S6 production code changes.
- Department DTOs, records, permissions, services, repository interfaces, and tree logic live in `WeCms.Modules.Organization`.
- Department repository implementation lives in `WeCms.Modules.Organization.SqlSugar`.
- Position DTOs, records, permissions, services, repository interfaces, and business rules live in `WeCms.Modules.Organization`.
- Position repository implementation lives in `WeCms.Modules.Organization.SqlSugar`.
- Identity user create/update uses `IOrganizationLookupService` or equivalent Organization abstraction for department and position validation.
- Identity does not reference Organization SqlSugar implementations or Organization repositories directly.
- Backend source has no old system-position naming residuals for `sys_post`, `sys_user_post`, `UserPost`, `PostService`, `IPostRepository`, `PostRepository`, `PostPermissions`, `CreatePostRequest`, or `/system/posts`.
- Department and Position endpoints remain explicit Minimal APIs and carry permission, audit, rate-limit, validation, and OpenAPI metadata.
- SQL lives only in allowed SqlSugar adapter or migration/seed boundaries.
- Full backend quality gate and S6-focused audits pass after each completed S6 task.
