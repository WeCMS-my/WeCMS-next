# S9 System and Persistence Removal Spec

## Goal

Delete the transitional `WeCms.Modules.System` and `WeCms.Persistence` boundaries after S4-S8 moved system-foundation ownership into target modules and `WeCms.Data.SqlSugar` / module `.SqlSugar` adapters.

Sprint 9 closes the migration allow-list, resets the system-foundation database baseline, and turns the architecture and script gates from transition-tolerant checks into final-state checks.

## Source Of Truth

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` defines development-plan Sprint 9 as deleting old System / Persistence and resetting the baseline.
- `docs/adr/0018-system-foundation-module-split.md` requires `WeCms.Modules.System` to be deleted after the migration allow-list closes.
- `docs/adr/0019-sqlsugar-data-platform.md` requires `WeCms.Persistence` to be deleted after `WeCms.Data.SqlSugar` and module `.SqlSugar` adapters own persistence.
- `docs/specs/s8-system-residual-migration/spec.md` explicitly leaves global System/Persistence deletion to S9.

## Scope

- Remove the `WeCms.Modules.System` project, source files, namespace references, registrations, tests, and OpenAPI/source-generation dependencies.
- Move or replace any remaining System-owned compatibility surfaces, including menu, role, permission endpoint compatibility, permission constants, permission metadata extensions, and `IPermissionVersionRepository` contracts.
- Update all System removal consumers, including solution/project references, `Program.cs`, JSON source generation, handwritten OpenAPI descriptors/schema helpers, endpoint metadata tests, unit/integration tests, and scripts such as ThinkPHP feature delta and rate-limit coverage.
- Remove the `WeCms.Persistence` project, source files, namespace references, service registrations, tests, and solution references.
- Move or replace any remaining Persistence-owned implementations, including `PermissionVersionRepository`, database probe implementation, migration probe implementation, and any transitional registration code.
- Consolidate remaining Persistence DI responsibilities into approved boundaries: shared SqlSugar platform registration in `WeCms.Data.SqlSugar`, module repositories in matching `.SqlSugar` adapters, permission-version persistence in the appropriate AccessControl adapter, and platform probes in an approved data/platform adapter boundary.
- Reset the system-foundation database baseline with the current destructive-upgrade schema and seed state.
- Rename remaining system-position database objects and code references from `post` / `Post` where they represent organization positions.
- Reset migration/seed tests, appsettings latest migration requirements, seed count assertions, permission coverage scripts, and clean-DB smoke assumptions to the new baseline file set.
- Close transition allow-lists in architecture tests and quality-gate scripts so final rules fail if old `System` or `Persistence` production boundaries reappear.

## Non-Goals

- Do not implement Sprint 10 SqlSugar platform upgrades such as QueryFilter, tenant provisioning, data-scope filters, SQL audit hooks, or advanced CodeFirst behavior beyond what is needed for the baseline reset.
- Do not implement cache, AOP, EventBus, Outbox, CMS, frontend feature work, Swagger/Scalar feature changes, or AI runtime capability.
- Do not rename public API routes or permission codes unless a route still contains the domain word `post` for organization positions and the Sprint 9 baseline task explicitly owns that breaking rename.
- Do not add compatibility branches, legacy fallbacks, silent behavior changes, MVC Controller, Razor, EF Core, dynamic query/return types, or runtime endpoint scanning.
- Do not hand-edit frontend generated contracts unless a backend contract generation task explicitly updates them with a real generated artifact.

## Acceptance

- `docs/specs/s9-system-persistence-removal/{spec.md,tasks.md,checklist.md}` exists before Sprint 9 production code changes.
- `WeCms.Modules.System` is removed from production source, solution/project references, registrations, source-generation metadata, OpenAPI descriptors, and tests.
- `rg "WeCms.Modules.System" backend/src backend/tests` has no production/test result except intentionally named historical audit fixtures if explicitly documented.
- `WeCms.Persistence` is removed from production source, solution/project references, registrations, tests, and scripts.
- `rg "WeCms.Persistence" backend/src backend/tests` has no production/test result.
- SqlSugar, MySqlConnector, ORM clients, database connections, and SQL text appear only in `WeCms.Data.SqlSugar`, `WeCms.Modules.*.SqlSugar`, and `database/**`.
- `WeCms.Modules.*` projects do not depend on `WeCms.Data.SqlSugar`, any `WeCms.Modules.*.SqlSugar`, `WeCms.Persistence`, SqlSugar ORM, MySqlConnector, database connections, or SQL text.
- The new baseline schema and seeds can initialize a clean database and preserve required system permissions, menus, locked-role behavior, super-admin bootstrap, and migration smoke coverage.
- Baseline smoke tests no longer depend on the old incremental migration/seed counts or old latest-migration id.
- `sys_post`, `sys_user_post`, `PostService`, `IPostRepository`, and `PostPermissions` no longer exist in production system-foundation code after the baseline reset and position rename task closes.
- Final architecture tests and quality-gate scripts fail on reintroduced `WeCms.Modules.System`, `WeCms.Persistence`, MVC Controller, Razor, EF Core, AI runtime, SQL-in-module, and dependency-boundary violations.
- Final quality gates no longer require optional environment flags to reject `WeCms.Modules.System` or `WeCms.Persistence`.
- Full backend quality gate passes with MySQL for each completed Sprint 9 implementation task and for the final Sprint 9 audit.
