# S8 System Residual Migration Spec

## Goal

Migrate the remaining system-foundation capabilities out of the transitional `WeCms.Modules.System` and `WeCms.Persistence` boundaries into their target modules:

- Logs -> `WeCms.Modules.Audit` and `WeCms.Modules.Audit.SqlSugar`
- Security -> `WeCms.Modules.Security` and `WeCms.Modules.Security.SqlSugar`
- Files -> `WeCms.Modules.FileCenter` and `WeCms.Modules.FileCenter.SqlSugar`
- System health/probes -> `WeCms.Modules.Platform`

S8 must preserve existing API routes, permission codes, audit/security behavior, OpenAPI coverage, and explicit Minimal API registration while moving ownership to the new module boundaries.

## Source Of Truth

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` is the execution plan for sprint numbering.
- Development-plan S8 is Audit / Security / FileCenter / Platform migration.
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md` contains older sprint-numbering sections: technical-book Sprint 6 maps to development-plan S8 Audit/Security/FileCenter/Platform, and technical-book Sprint 7 maps to later Persistence removal.
- `docs/adr/0018-system-foundation-module-split.md` defines the final module mapping.
- When numbering conflicts, execute the development plan order: S8 residual system module migration before S9 deletion of old System/Persistence and before S11/S12 cache/AOP/EventBus work.

## Scope

- `WeCms.Modules.Audit` owns audit log and login log DTOs, records, permissions, services, repository interfaces, endpoint definitions, and audit query abstractions.
- `WeCms.Modules.Audit.SqlSugar` owns audit and login log repository implementations.
- `WeCms.Modules.Security` owns security event, security ban, security alerting, rate-limit security event abstractions, permissions, services, repository interfaces, endpoint definitions, and security event writer abstractions required by current behavior.
- `WeCms.Modules.Security.SqlSugar` owns security repository implementations.
- `WeCms.Modules.FileCenter` owns file DTOs, records, permissions, service, upload policy, object key generation abstractions, repository interfaces, and endpoint definitions.
- `WeCms.Modules.FileCenter.SqlSugar` owns file repository implementations.
- File storage implementation remains in `WeCms.Infrastructure`; S8 must not move physical storage provider implementations into FileCenter unless a later storage-specific spec approves it.
- `WeCms.Modules.Platform` owns platform/system health, ping, database probe contracts, migration probe contracts, records, and endpoint definitions.
- S8 preserves public API routes and permission codes unless a later explicit contract-change spec is approved.
- S8 must update JSON source-generation metadata, hand-written OpenAPI descriptors, endpoint coverage tests, permission scripts, audit scripts, rate-limit scripts, security-event scripts, and seed references whenever namespace ownership changes.

## Non-Goals

- Do not delete `WeCms.Modules.System` or `WeCms.Persistence` globally in S8; that is S9 scope. S8 removes only S8-owned source and registrations from those transitional boundaries.
- Do not migrate Identity, AccessControl, Organization, or Configuration again except for compile-time references required by the S8 modules.
- Do not introduce CMS APIs or include `WeCms.Modules.Cms` in API/OpenAPI/quality-gate feature coverage.
- Do not implement distributed cache, AOP transaction/cache interceptors, EventBus, Outbox, MiniProfiler, Swagger/Scalar feature changes, or SQL audit platform upgrades in S8.
- Do not move file storage provider implementations out of `WeCms.Infrastructure`.
- Do not rename existing public routes or permission codes as a side effect of ownership migration.
- Do not introduce MVC Controller, Razor, runtime endpoint scanning, EF Core, dynamic query/return types, silent legacy fallback, or AI runtime capability.
- Do not hand-edit frontend generated contract files or frontend application code in S8 unless a later explicitly scoped frontend task is created.

## Acceptance

- `docs/specs/s8-system-residual-migration/{spec.md,tasks.md,checklist.md}` exists before S8 production code changes.
- Audit and login log DTOs, records, permissions, services, repository interfaces, and endpoint definitions live in `WeCms.Modules.Audit`.
- Audit and login log repository implementations live in `WeCms.Modules.Audit.SqlSugar`.
- Security DTOs, records, permissions, services, repository interfaces, endpoint definitions, security alerting, and security-event writer abstractions live in `WeCms.Modules.Security`.
- Security repository implementations live in `WeCms.Modules.Security.SqlSugar`.
- File DTOs, records, permissions, services, upload policies, object key abstractions, repository interfaces, and endpoint definitions live in `WeCms.Modules.FileCenter`.
- File repository implementations live in `WeCms.Modules.FileCenter.SqlSugar`.
- Platform health/ping/database/migration probe contracts, records, and endpoint definitions live in `WeCms.Modules.Platform`.
- Each target `WeCms.Modules.*` project references only `WeCms.Shared` and approved cross-module contracts; it does not reference its `*.SqlSugar` implementation project, `WeCms.Persistence`, SqlSugar ORM, MySqlConnector, database connections, or SQL text.
- Each target `*.SqlSugar` project references only its matching module, `WeCms.Data.SqlSugar`, and `WeCms.Shared` among production projects unless an explicit contract dependency is documented in the task.
- Existing routes, permission codes, authentication policies, audit metadata, security event behavior, rate limits, OpenAPI descriptors, and JSON source-generation metadata remain covered.
- Backend source has no old S8-owned `WeCms.Modules.System.{Logs,Security,Files,System}` or `WeCms.Persistence.Modules.System.{Logs,Security,Files,System}` ownership residuals after the corresponding migration task closes.
- S8 does not migrate cache infrastructure, AOP, EventBus, Outbox, CMS, Identity, AccessControl, Organization, or Configuration ownership beyond required references.
- Full backend quality gate and S8-focused audits pass after each completed S8 implementation task.
