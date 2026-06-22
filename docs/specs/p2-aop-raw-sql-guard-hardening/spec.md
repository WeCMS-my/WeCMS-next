# P2 AOP and Raw SQL Guard Hardening Spec

## Scope

This change hardens two existing backend platform surfaces:

- `ApplicationServiceAopInterceptor` must preserve original exceptions for `Task<T>` interception, including synchronous failures while building the AOP pipeline.
- `RawSqlFilterGuard` must reject alias-mismatched raw SQL predicates for tenant, data-scope, and soft-delete filters when SQL uses table aliases.

## Non-Goals

- No public API, OpenAPI, permission, menu, database schema, seed, or frontend contract changes.
- No new runtime AI capability.
- No repository SQL rewrite in this task.

## Requirements

- Existing AOP tests for audit, unit-of-work, cacheable, cache-evict, tenant cache keys, `Task<T>` results, and rollback behavior must stay green.
- A synchronous exception thrown during generic `Task<T>` AOP pipeline construction must surface as the original exception, not `TargetInvocationException`.
- Raw SQL guard must continue allowing existing no-alias SQL behavior.
- When a guarded table has an explicit alias, required predicates must bind to that alias:
  - `alias.tenant_id = @tenantId`
  - `alias.created_by_user_id IN @dataScopeUserIds`
  - `alias.deleted_at IS NULL`

## Validation

- Targeted unit tests for AOP and Raw SQL guard.
- Full unit test project.
- Backend quality gate, or explicit environment blocker if MySQL integration configuration is unavailable.
