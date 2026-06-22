# P2 AOP and Raw SQL Guard Hardening Checklist

- [x] Red tests observed before implementation.
- [x] AOP original exception is preserved for generic pipeline setup failures.
- [x] Existing AOP unit-of-work and cache behavior remains covered.
- [x] Raw SQL guard rejects tenant pseudo-predicates bound to the wrong alias.
- [x] Raw SQL guard rejects data-scope pseudo-predicates bound to the wrong alias.
- [x] Raw SQL guard rejects unqualified soft-delete predicates for aliased guarded tables.
- [x] No public API, OpenAPI, database schema, permission, menu, or frontend generated contract change.
- [x] No AI runtime code introduced.
- [x] Full backend gate blocker recorded: local `WECMS_TEST_MYSQL_CONNECTION_STRING` is unavailable.
