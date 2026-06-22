# P1 AOP and Raw SQL Hardening Tasks

## Task 1: Raw SQL Predicate Builders

- Add failing unit tests for tenant, data scope, and soft-delete predicate builders.
- Implement builder classes in `WeCms.Data.SqlSugar`.
- Keep existing `RawSqlFilterGuard` tests passing.

## Task 2: Async-Only AOP

- Add a failing unit test proving void application service methods are rejected without executing the target.
- Remove the synchronous interception path from `ApplicationServiceAopInterceptor`.
- Verify existing audit tests still pass.

## Task 3: AOP Main Path Coverage

- Add unit tests for `[UnitOfWork]` commit and rollback.
- Add unit tests for `[Cacheable]` cache-hit short-circuit and `[CacheEvict]` key/prefix eviction.
- Add unit tests for tenant-aware cache keys, `Task<T>` result preservation, and exception preservation.

## Task 4: Verification and Audit

- Run targeted unit tests for Data.SqlSugar and AOP.
- Run backend build/test/publish or the backend quality gate.
- Audit changed files against AGENTS and `code_review.md`.
