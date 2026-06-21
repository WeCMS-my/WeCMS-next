# S7 Configuration Migration Spec

## Goal

Migrate configuration-domain system administration capabilities from the transitional `WeCms.Modules.System` and `WeCms.Persistence` boundaries into `WeCms.Modules.Configuration` and `WeCms.Modules.Configuration.SqlSugar`.

Sprint 7 owns Settings, Dicts, I18n messages, and a configuration cache invalidation abstraction. It must preserve existing API behavior, security rules, permission metadata, audit coverage, OpenAPI coverage, and Minimal API explicit endpoint registration while moving ownership to the Configuration module boundary.

## Source Of Truth

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` is the execution plan for sprint numbering.
- Development-plan S7 is Configuration migration.
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md` contains older sprint-numbering sections: technical-book Sprint 5 maps to development-plan S7 Configuration, technical-book Sprint 6 maps to development-plan S8 Audit/Security/FileCenter/Platform, and technical-book Sprint 7 maps to the later Persistence removal sprint.
- When numbering conflicts, execute the development plan order: S7 Configuration before Audit/Security/FileCenter/Platform and before final Persistence removal.

## Scope

- `WeCms.Modules.Configuration` owns Settings, Dicts, and I18n DTOs, records, permissions, services, repository interfaces, endpoint definitions, and cache invalidation abstractions introduced during migration.
- `WeCms.Modules.Configuration.SqlSugar` owns Settings, Dicts, and I18n repository implementations and any Configuration CodeFirst model registration introduced during migration.
- Settings migration must preserve sensitive setting protections, including no plaintext sensitive values in API responses and existing setting validation behavior.
- Settings, Dicts, and I18n write operations must call a Configuration cache invalidation abstraction. A no-op implementation is acceptable in S7; real distributed cache integration remains later scope.
- Dict migration must preserve type/value separation, status behavior, write audit metadata, permission metadata, and OpenAPI coverage.
- I18n migration must preserve public message read behavior and keep public message endpoints explicitly `AllowAnonymous` where they are currently public. User language switching must retain its existing authentication or permission policy.
- S7 is an ownership migration, not a public route rewrite. Existing public routes such as `/api/v1/system/settings`, `/api/v1/system/dicts`, and existing I18n/account routes must remain stable unless a later explicit contract-change task is created.
- S7 preserves existing permission codes such as `sys:setting:*`, `sys:dict:*`, and `sys:i18n:*`. Renaming them to `config:*` is out of scope for S7 unless a separate permission-contract migration spec is approved.
- Public contract changes must be reflected in OpenAPI, generated schemas, endpoint coverage tests, permission coverage scripts, audit coverage scripts, and seed data.

## Non-Goals

- Do not migrate Audit, Security, FileCenter, Platform, CMS, caching infrastructure, AOP, EventBus, or Outbox in S7.
- Do not remove `WeCms.Modules.System` or `WeCms.Persistence` globally in S7; remove only Configuration-owned service/repository registrations and source from those boundaries.
- Do not change authentication, token, 2FA, AccessControl, Organization, file storage, audit log querying, security event querying, or platform health-check semantics.
- Do not introduce MVC Controller, Razor, runtime endpoint scanning, EF Core, dynamic query/return types, silent legacy fallback, or AI runtime capability.
- Do not hand-edit frontend generated contract files or frontend application code in S7 unless a later explicitly scoped frontend task is created.
- Do not remove or rename public routes, permission codes, seed permission codes, frontend route fixtures, or generated frontend contracts as a side effect of moving backend ownership.

## Acceptance

- `docs/specs/s7-configuration-migration/{spec.md,tasks.md,checklist.md}` exists before S7 production code changes.
- Settings DTOs, records, permissions, services, repository interfaces, endpoint definitions, and security rules live in `WeCms.Modules.Configuration`.
- Setting repository implementation lives in `WeCms.Modules.Configuration.SqlSugar`.
- Settings write operations call `IConfigurationCacheInvalidator` or equivalent Configuration abstraction.
- Sensitive Settings behavior is preserved: sensitive values are not returned in plaintext and sensitive writes remain audited.
- Dict DTOs, records, permissions, services, repository interfaces, endpoint definitions, and business rules live in `WeCms.Modules.Configuration`.
- Dict repository implementation lives in `WeCms.Modules.Configuration.SqlSugar`.
- Dict write operations call the Configuration cache invalidation abstraction.
- I18n DTOs, records, permissions, services, repository interfaces, endpoint definitions, and public message behavior live in `WeCms.Modules.Configuration`.
- I18n repository implementation lives in `WeCms.Modules.Configuration.SqlSugar`.
- I18n write operations call the Configuration cache invalidation abstraction.
- `WeCms.Modules.Configuration` does not reference `WeCms.Modules.Configuration.SqlSugar`, `WeCms.Persistence`, SqlSugar ORM, MySqlConnector, database connections, or SQL text.
- `WeCms.Modules.Configuration.SqlSugar` references only `WeCms.Modules.Configuration`, `WeCms.Data.SqlSugar`, and `WeCms.Shared` among production projects.
- Backend source has no old Configuration-owned `WeCms.Modules.System.Settings`, `WeCms.Modules.System.Dicts`, `WeCms.Modules.System.I18n`, or `WeCms.Persistence.Modules.System.{Settings,Dicts,I18n}` ownership residuals after the corresponding migration task closes.
- Settings, Dicts, and I18n endpoints remain explicit Minimal APIs and carry permission, audit, rate-limit, validation, and OpenAPI metadata as applicable.
- JSON source-generation metadata, OpenAPI hand-written schema/endpoint descriptors, seed data, permission coverage scripts, audit coverage scripts, and endpoint coverage tests are updated whenever DTO namespaces or endpoint ownership changes.
- SQL lives only in allowed SqlSugar adapter or migration/seed boundaries.
- Full backend quality gate and S7-focused audits pass after each completed S7 task.
