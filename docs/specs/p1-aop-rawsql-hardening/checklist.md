# P1 AOP and Raw SQL Hardening Checklist

- [x] Red tests were observed before production code changes for P1-001 and P1-002; P1-003 was coverage-only and production-code change was N/A.
- [x] Raw SQL builders live inside `WeCms.Data.SqlSugar`.
- [x] Guard behavior remains present and tested.
- [x] AOP supports only `Task` and `Task<T>`.
- [x] No sync-over-async audit path remains.
- [x] AOP transaction commit and rollback are tested.
- [x] AOP cache hit and eviction are tested.
- [x] Cache tenant id comes from the registered tenant accessor.
- [x] `Task<T>` return values and target exceptions are preserved.
- [x] No frontend files changed by this spec.
- [x] No public API, OpenAPI, permission, menu, migration, CMS, AI runtime, or legacy compatibility changes by this spec.
- [x] Targeted tests passed.
- [x] Backend quality gate or equivalent commands were run, with environment blockers recorded.
