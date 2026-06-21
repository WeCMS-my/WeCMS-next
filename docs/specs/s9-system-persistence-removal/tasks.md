# S9 System and Persistence Removal Tasks

Scope summary: close the migration allow-list by deleting old `WeCms.Modules.System` and `WeCms.Persistence`, reset the system-foundation database baseline, and harden final architecture gates.

## S9-T00 Spec Trio

Add the Sprint 9 removal spec trio before production code changes.

Required proof includes:

- `docs/specs/s9-system-persistence-removal/spec.md`
- `docs/specs/s9-system-persistence-removal/tasks.md`
- `docs/specs/s9-system-persistence-removal/checklist.md`
- S8-to-S9 boundary documented: S8 moved Audit/Security/FileCenter/Platform only; S9 deletes old System/Persistence globally
- docs/rules audit

## S9-T01 Delete Old WeCms.Modules.System

Delete the old `WeCms.Modules.System` project and namespace after replacing or moving the remaining compatibility surfaces.

Current known inputs include:

- `backend/WeCms.slnx` and backend project references
- `Program.cs` using directives, `AddWeCmsSystemPermissions`, and system endpoint mappings
- menu and role endpoint compatibility code that delegates to `WeCms.Modules.AccessControl`
- permission endpoint extension and secure-ping compatibility code
- permission constants and permission-version contracts still under `WeCms.Modules.System.Permissions`
- handwritten OpenAPI endpoint/schema helpers and JSON source-generation references
- unit, integration, and architecture tests that still import `WeCms.Modules.System`
- script paths that still point roles, permissions, or menus at old System files

Required proof includes:

- Red architecture test that fails while `WeCms.Modules.System` remains
- endpoint mapping and permission metadata tests after replacement
- OpenAPI endpoint/schema tests after namespace removal
- `rg "WeCms.Modules.System" backend/src backend/tests`
- layer, DB boundary, no-controller, minimal-api metadata, OpenAPI, permission, audit, and backend quality gates

## S9-T02 Delete Old WeCms.Persistence

Delete the old `WeCms.Persistence` project and namespace after replacing or moving the remaining persistence implementations and registrations.

Current known inputs include:

- `backend/WeCms.slnx` and backend project references
- `Program.cs` using directives and `AddWeCmsPersistence`
- `PersistenceServiceCollectionExtensions` ownership of transitional DI
- `PermissionVersionRepository`
- platform database and migration probe implementations
- `DatabaseOptions` / configuration exception leftovers if still under the Persistence namespace
- unit, integration, and architecture tests that still import `WeCms.Persistence`
- script paths that still point database governance or observability at old Persistence files

Required proof includes:

- Red architecture test that fails while `WeCms.Persistence` remains
- integration tests for moved permission-version repository behavior
- integration tests for moved platform database and migration probes
- `rg "WeCms.Persistence" backend/src backend/tests`
- SqlSugar boundary, DB boundary, layer, DI, no SQL in modules, and backend quality gates

## S9-T03 Reset Database Baseline

Replace the incremental migration/seed chain with the destructive-upgrade baseline required by the current system-foundation model.

Required work includes:

- remove old `database/migrations/*.sql` and `database/seeds/*.sql`
- add `000001_baseline_system_schema.sql`
- add `000002_seed_system_permissions.sql`
- add `000003_seed_super_admin.sql`
- reset latest-required-migration configuration to the new baseline id
- reset migration smoke tests and seed-count assertions away from the old 19-migration / 11-seed chain
- update permission coverage scripts that read old seed filenames
- rename organization position tables from `sys_post` / `sys_user_post` to `sys_position` / `sys_user_position`
- add permission endpoint/button baseline structures if not already represented
- keep Outbox only if the Sprint 9 baseline explicitly reserves it without implementing EventBus/Outbox behavior
- update migration and seed smoke tests for the new baseline

Required proof includes:

- `MigrationAndSeedSmokeTests`
- baseline schema tests
- locked-role seed tests
- permission seed coverage tests
- clean database initialization against MySQL
- backend quality gate

## S9-T04 Final Quality-Gate Rules

Turn transition-tolerant rules into final-state gates.

Required work includes:

- close `WeCms.Modules.System` allow-list
- close `WeCms.Persistence` allow-list
- make no-system-god-module and no-persistence-god-module checks final by default, without optional environment flags
- delete layer dependency script allowances that conditionally add old System/Persistence when those projects exist
- update DB boundary rules so SQL/ORM/database access is allowed only in `WeCms.Data.SqlSugar`, `WeCms.Modules.*.SqlSugar`, and `database/**`
- update database-governance, observability, ThinkPHP delta, rate-limit coverage, permission coverage, and OpenAPI coverage scripts away from old System/Persistence paths
- keep no-controller, Minimal API metadata, no AI runtime, and frontend generated-artifact rules enforced
- update script wording to match final rules

Required proof includes:

- architecture tests for final System and Persistence removal
- script checks for final layer, DB, SqlSugar, DI, and no-controller boundaries
- full backend quality gate with MySQL

## S9-T05 Final Sprint 9 Audit

Run a total audit after S9-T01 through S9-T04 complete.

Required proof includes:

- no old System/Persistence namespace or project references in backend production/test code
- no SQL/ORM/database access in business modules
- no CMS/cache/AOP/EventBus/Outbox/S10+ implementation drift
- no Controller/MVC/Razor/AI runtime capability
- final checklist complete
- full backend quality gate with MySQL
