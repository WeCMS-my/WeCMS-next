# P1 AOP and Raw SQL Hardening Spec

## Context

WeCMS Next is in the post-phase hardening stage. The current raw SQL guard detects missing soft-delete, tenant, and data-scope predicates with regular expressions, but raw SQL callers still lack a single explicit way to generate those predicates. The AOP interceptor supports async application service methods, but it also has a synchronous void path that blocks on async audit writes. AOP tests currently cover audit behavior more than transaction and cache behavior.

## Goals

- Add explicit raw SQL predicate builders for tenant, data scope, and soft delete predicates.
- Keep `RawSqlFilterGuard` as a final execution-time guard.
- Make application service AOP async-only by rejecting non-`Task` and non-`Task<T>` return types.
- Cover the main AOP paths for transaction commit/rollback, cache hit/eviction, tenant-aware cache keys, `Task<T>` return values, and exception preservation.

## Non-Goals

- No public API, OpenAPI, permission, menu, database table, or migration changes.
- No frontend changes.
- No CMS module, AI runtime, or legacy compatibility work.
- No attempt to parse complex SQL, CTE, subquery, or `UNION` semantics.

## Design

Raw SQL predicate generation is added under `WeCms.Data.SqlSugar`, where SQL platform helpers are allowed by ADR-0019. Builders return small immutable value objects containing SQL text and parameters so repository implementations can compose explicit predicates without hard-coding every system predicate string.

The builders are intentionally narrow:

- `TenantSqlPredicateBuilder` produces alias-qualified `tenant_id = @tenantId` predicates.
- `DataScopeSqlPredicateBuilder` produces alias-qualified `<column> IN @dataScopeUserIds` predicates and supports caller-selected column names.
- `SoftDeleteSqlPredicateBuilder` produces alias-qualified `deleted_at IS NULL` predicates.

The AOP interceptor rejects unsupported return types before any audit, transaction, cache, or target method execution. Application service interfaces are therefore limited to `Task` and `Task<T>`.

## Acceptance Criteria

- Predicate builders qualify aliases correctly and expose expected parameter values.
- Data scope builder can use a column other than `created_by_user_id`.
- Predicate builders fail fast on missing required inputs.
- AOP void methods throw `NotSupportedException` and do not call the target method.
- `[UnitOfWork]` commits on success and rolls back on exception.
- `[Cacheable]` cache hit returns cached value and does not execute the target method.
- `[CacheEvict]` removes key or prefix after successful execution.
- Cache keys use tenant id from the registered tenant accessor.
- `Task<T>` results are preserved.
- Target exceptions are not wrapped.
