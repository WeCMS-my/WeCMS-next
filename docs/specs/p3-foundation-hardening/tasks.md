# P3 Foundation Hardening Tasks

- [x] P3-001: Harden `RawSqlFilterGuard` for complex SQL patterns.
- [x] P3-002: Add PredicateBuilder-first guardrails for new raw SQL.
- [x] P3-003: Verify and close audit context field consistency gaps.
- [x] P3-004: Add AOP, cache, and Outbox boundary coverage.
- [x] P3-005: Add the system-foundation operations guide.
- [ ] Run final backend verification and scoped architecture audit.

Backend quality gate note: `bash scripts/quality-gate-backend.sh` is currently blocked because `WECMS_TEST_MYSQL_CONNECTION_STRING` is not set in this worktree environment.
