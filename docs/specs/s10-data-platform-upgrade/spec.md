# S10 Data Platform Upgrade Spec

## Goal

Complete the SqlSugar data platform upgrade after Sprint 9 removed the old System and Persistence boundaries. Sprint 10 owns CodeFirst entity modeling, model-provider registration, schema validation, query filters, multi-connection / tenant connection resolution, and SQL audit primitives.

## Source Of Truth

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` defines Sprint 10 as `SqlSugar 数据平台完整升级`.
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md` requires CodeFirst modeling, migration hardening, QueryFilter isolation, multi-database / multi-tenant connection governance, and SQL audit observability.
- `docs/adr/0019-sqlsugar-data-platform.md` keeps database infrastructure in `WeCms.Data.SqlSugar` and module adapter persistence in `WeCms.Modules.*.SqlSugar`.
- `docs/specs/s9-system-persistence-removal/spec.md` explicitly leaves QueryFilter, tenant provisioning, data-scope filters, SQL audit hooks, and advanced CodeFirst behavior to Sprint 10.

## Scope

- Define shared entity marker interfaces in `WeCms.Shared` only when module-layer contracts must reference them.
- Keep concrete entity base classes and CodeFirst infrastructure in `WeCms.Data.SqlSugar`.
- Add system-foundation entity models in the owning module `.SqlSugar` adapter projects.
- Add `ICodeFirstModelProvider` implementations in each data-backed `.SqlSugar` adapter.
- Add a central CodeFirst model registry in `WeCms.Data.SqlSugar` with duplicate table and missing `SugarTable` validation.
- Add `CodeFirstRunner` and `MigrationScaffold` primitives for development/test workflows and reviewable migration output.
- Add schema validator primitives that compare registered entity metadata to the current database schema.
- Implement QueryFilter registration for soft delete, tenant, and data-scope filtering with explicit bypass reason and audit requirements.
- Complete named connection and tenant connection resolution for main, log, audit, file, and tenant roles.
- Add SQL audit primitives for slow SQL, failed SQL, redaction, trace/user/tenant context, repository/operation metadata, SQL hashing/templates, affected rows, timing, and recursion prevention.
- Update architecture, unit, and integration tests plus quality-gate scripts for the new data-platform guarantees.

## Non-Goals

- Do not implement Sprint 11 cache providers, transaction interceptors, cache interceptors, Autofac registration, or AOP runtime behavior.
- Do not implement Sprint 12 EventBus dispatching, Outbox dispatcher, idempotent event handling, or cross-database distributed transactions.
- Do not implement Sprint 13 Swagger / Scalar / MiniProfiler UI changes.
- Do not add CMS APIs, CMS runtime behavior, frontend features, AI runtime, MVC Controller, Razor, EF Core, `dynamic` query/return types, or legacy fallback behavior.
- Do not auto-run production DDL. CodeFirst `InitTables` remains development/test infrastructure; production structure is governed by the migration baseline and schema validation.
- Do not bypass query filters silently. Any bypass must require an explicit reason and produce audit evidence.

## Acceptance

- `docs/specs/s10-data-platform-upgrade/{spec.md,tasks.md,checklist.md}` exists before Sprint 10 production code changes.
- Entity interfaces and base classes are placed according to the dependency matrix.
- System-foundation CodeFirst entities have explicit `SugarTable`, `SugarColumn`, nullable, length, and index metadata.
- Every data-backed `.SqlSugar` adapter exposes exactly its module entity models through `ICodeFirstModelProvider`.
- CodeFirst registry rejects duplicate table names and missing `SugarTable` metadata.
- Schema validator detects missing tables, missing columns, nullable mismatch, length mismatch, and index mismatch.
- QueryFilter registration hides soft-deleted rows, isolates tenants, enforces data scope, and requires audited bypass reasons.
- Named connection and tenant connection resolution support main, log, audit, file, shared-tenant, and configured dedicated-tenant modes without distributed transactions.
- SQL audit records slow SQL and failed SQL, redacts sensitive parameters, includes connection name / traceId / userId / tenantId, and prevents recursive self-audit.
- SQL audit records the required fields: TraceId, UserId, Username, TenantId, ConnectionName, RepositoryName, OperationType, SqlHash, SqlTemplate, ParametersRedacted, ElapsedMs, AffectedRows, IsSlowSql, ErrorMessage, and CreatedAt.
- SQL audit redaction covers password, password_hash, token, refresh_token, access_token, secret, two_factor, recovery_code, private_key, and connection_string.
- Production SQL audit defaults to slow SQL and failed SQL only; development/test may opt in to all-SQL capture for verification.
- SqlSugar, MySqlConnector, ORM clients, database connections, SQL text, CodeFirst, schema validation, QueryFilter, tenant resolution, and SQL audit infrastructure remain inside `WeCms.Data.SqlSugar`, `WeCms.Modules.*.SqlSugar`, and `database/**`.
- Full backend quality gate passes with MySQL for each completed Sprint 10 implementation task and for the final Sprint 10 audit.
