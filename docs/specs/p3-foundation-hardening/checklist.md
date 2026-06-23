# P3 Foundation Hardening Checklist

- [x] P3 backlog is documented separately from mainline feature development.
- [x] `RawSqlFilterGuard` does not silently pass unsupported complex guarded SQL.
- [x] New guarded raw SQL is covered by PredicateBuilder-first checks or explicit exceptions.
- [x] Audit context fields are consistently written or intentionally N/A.
- [x] AOP, cache, and Outbox boundary behavior is locked by tests.
- [x] System-foundation operations guide exists and is linked from release documentation.
- [x] No frontend generated types are hand-edited.
- [x] No CMS, AI runtime, legacy compatibility, Controller, EF Core, or distributed transaction work is introduced.
- [x] Targeted tests pass.
- [x] Backend quality gate passes or blockers are clearly classified.

Backend quality gate blocker: `WECMS_TEST_MYSQL_CONNECTION_STRING` is required for MySQL integration tests and is not present in this worktree environment.
