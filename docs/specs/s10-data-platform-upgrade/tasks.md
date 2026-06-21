# S10 Data Platform Upgrade Tasks

Scope summary: upgrade the SqlSugar data platform now that Sprint 9 removed the old System/Persistence transition boundaries.

## S10-T00 Spec Trio

Add the Sprint 10 spec trio before production code changes.

Required proof includes:

- `docs/specs/s10-data-platform-upgrade/spec.md`
- `docs/specs/s10-data-platform-upgrade/tasks.md`
- `docs/specs/s10-data-platform-upgrade/checklist.md`
- S9-to-S10 boundary documented: S9 only removed old boundaries and reset baseline; S10 now owns CodeFirst, QueryFilter, tenant connections, schema validation, and SQL audit
- docs/rules audit

## S10-T01 Entity Base Interfaces And System Entities

Add entity contracts and base classes required by CodeFirst and QueryFilter.

Required work includes:

- define `IEntity<TKey>`, `ISoftDeleteEntity`, `IAuditedEntity`, `ITenantEntity`, `ISiteScopedEntity`, and `IDataScopedEntity`
- place module-visible interfaces in `WeCms.Shared`
- place concrete base classes in `WeCms.Data.SqlSugar`
- add `EntityBase`, `TenantEntityBase`, and `SiteScopedEntityBase`
- add system-foundation entity classes for current baseline tables in the owning `.SqlSugar` adapters
- add explicit `SugarTable`, `SugarColumn`, nullable, length, and index metadata
- avoid any dependency from business modules to SqlSugar or `.SqlSugar` adapters

Required proof includes:

- red entity metadata architecture tests
- `EntityMetadataTests`
- targeted unit tests for base entity contracts
- DB boundary, SqlSugar boundary, layer dependency, no SQL in modules, and backend quality gates

## S10-T02 CodeFirst Model Provider

Register CodeFirst model providers in the data-backed `.SqlSugar` adapters and aggregate them centrally.

Required work includes:

- add `ICodeFirstModelProvider` in `WeCms.Data.SqlSugar`
- implement one provider per data-backed module `.SqlSugar` adapter
- return only the owning module entity types
- add central registry aggregation in `WeCms.Data.SqlSugar`
- add `CodeFirstRunner` for development/test `InitTables` validation without production auto-DDL
- reject duplicate table names
- reject entity types missing `SugarTable`
- keep CMS excluded unless explicitly enabled by a later CMS task

Required proof includes:

- `CodeFirstModelRegistry_FailsOnDuplicateTable`
- `CodeFirstModelRegistry_FailsOnMissingSugarTable`
- `CodeFirstRunner_ValidatesRegisteredModelsInDevelopmentOrTestOnly`
- provider ownership tests per adapter
- DB boundary, SqlSugar boundary, layer dependency, and backend quality gates

## S10-T03 Schema Validator

Add schema validation that compares registered entity metadata to a live database schema.

Required work includes:

- compare entity table names against database tables
- detect missing tables
- detect missing columns
- detect nullable mismatches
- detect column length mismatches
- detect index mismatches
- add `MigrationScaffold` output for reviewable migration diffs
- keep CI migration smoke tests wired to the migration baseline
- produce CI-readable validation output
- keep production DDL disabled; validation reports failures rather than silently changing production schema

Required proof includes:

- `SchemaValidator_DetectsMissingTable`
- `SchemaValidator_DetectsMissingColumn`
- `SchemaValidator_DetectsIndexMismatch`
- `MigrationScaffold_ProducesReviewableBaselineDiff`
- migration smoke test against the Sprint 9 baseline
- clean MySQL validation against the Sprint 9 baseline
- backend quality gate

## S10-T04 QueryFilter Registrar

Implement runtime query-filter governance.

Required work includes:

- add `IQueryFilterRegistrar`
- implement soft-delete filter
- implement tenant filter
- implement data-scope filter
- add a controlled bypass mechanism
- require a non-empty bypass reason
- write audit evidence for bypasses
- document raw SQL limitations and require explicit audited builder/bypass for raw SQL that cannot use Queryable filters

Required proof includes:

- `SoftDeletedRowsHiddenByDefault`
- `TenantRowsAreIsolated`
- `DataScopeFiltersRows`
- `BypassFilterRequiresReason`
- `BypassFilterWritesAudit`
- backend quality gate

## S10-T05 Multi-Connection And Tenant Resolution

Complete named connection and tenant connection management.

Required work includes:

- support main, log, audit, file, and tenant connection roles
- support tenant connection resolver abstraction
- default to shared database plus `tenant_id`
- reserve dedicated database mode behind explicit configuration
- enforce one UnitOfWork connection per scope by default
- forbid distributed transactions; cross-database consistency belongs to Sprint 12 Outbox/EventBus

Required proof includes:

- `DefaultConnectionResolutionTests`
- `NamedConnectionResolutionTests`
- `TenantSharedDbResolutionTests`
- `TenantDedicatedDbResolutionTests_WhenConfigured`
- UnitOfWork single-connection tests
- backend quality gate

## S10-T06 SQL Audit

Add SQL audit infrastructure for SqlSugar execution.

Required work includes:

- add `ISqlAuditSink`
- add `SqlAuditRecord`
- add `SqlAuditRedactor`
- add `SqlSugarSqlAuditRegistrar`
- add `SqlAuditOptions`
- register SqlSugar AOP for slow SQL and failed SQL
- include TraceId, UserId, Username, TenantId, ConnectionName, RepositoryName, OperationType, SqlHash, SqlTemplate, ParametersRedacted, ElapsedMs, AffectedRows, IsSlowSql, ErrorMessage, and CreatedAt
- redact sensitive parameters including password, password_hash, token, refresh_token, access_token, secret, two_factor, recovery_code, private_key, and connection_string
- prevent recursive audit of audit writes
- default production SQL audit to slow SQL and failed SQL only
- allow development/test verification to opt in to all-SQL capture

Required proof includes:

- `SqlAudit_RecordsSlowSql`
- `SqlAudit_RecordsFailedSql`
- `SqlAudit_RedactsSensitiveParameters`
- `SqlAudit_RedactsKnownSensitiveFieldNames`
- `SqlAudit_IncludesRequiredRecordFields`
- `SqlAudit_ProductionDefaultRecordsOnlySlowAndFailedSql`
- `SqlAudit_DoesNotAuditItselfRecursively`
- backend quality gate

## S10-T07 Final Sprint 10 Audit

Run a total audit after S10-T01 through S10-T06 complete.

Required proof includes:

- CodeFirst validation usable against the Sprint 9 baseline
- migration baseline matches registered entity metadata
- QueryFilter behavior proven for soft delete, tenant, data scope, and audited bypass
- named and tenant connection resolution proven
- SQL audit redaction and recursion prevention proven
- no S11/S12/S13 scope drift
- no Controller/MVC/Razor/EF Core/AI runtime capability
- final checklist complete
- full backend quality gate with MySQL
