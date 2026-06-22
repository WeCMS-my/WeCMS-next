# P2 AOP and Raw SQL Guard Hardening Tasks

- [x] Add failing AOP test for synchronous generic pipeline exception unwrapping.
- [x] Add failing Raw SQL guard tests for tenant/data-scope wrong-alias predicates.
- [x] Add failing Raw SQL guard test for unqualified soft-delete predicate on an aliased table.
- [x] Unwrap `TargetInvocationException` at the generic `Task<T>` AOP reflection boundary.
- [x] Require alias-qualified guard predicates when raw SQL table references use aliases.
- [x] Run targeted tests and full unit tests.
- [x] Attempt full backend gate; blocked before validation because `WECMS_TEST_MYSQL_CONNECTION_STRING` is unavailable locally.
