# S8 System Residual Migration Tasks

Scope summary: migrate remaining system-foundation capabilities into Audit, Security, FileCenter, and Platform module boundaries while preserving current API and security behavior.

## S8-T00 Spec Trio

Add the S8 residual system migration spec trio before production code changes.

Required proof includes:

- `docs/specs/s8-system-residual-migration/spec.md`
- `docs/specs/s8-system-residual-migration/tasks.md`
- `docs/specs/s8-system-residual-migration/checklist.md`
- technical-book Sprint 6 to development-plan S8 numbering drift documented
- route and permission strategy documented as preserved public contract
- docs/rules audit

## S8-T01 Audit Migration

Move login log and audit log DTOs, records, permissions, services, repository interfaces, and endpoint definitions into `WeCms.Modules.Audit`.

Move `LogRepository` or equivalent audit/login log repository implementation into `WeCms.Modules.Audit.SqlSugar`, preserve current audit and login log routes, query semantics, permission codes, OpenAPI coverage, and read-only behavior.

Required proof includes:

- audit/login log service tests
- audit repository integration tests
- Audit API scan or endpoint metadata tests
- OpenAPI, permission, audit coverage, DB boundary, layer, and DI gates

## S8-T02 Security Migration

Move security event, security ban, security alerting, rate-limit security event writer abstractions, permissions, services, repository interfaces, and endpoint definitions into `WeCms.Modules.Security`.

Move security repository implementations into `WeCms.Modules.Security.SqlSugar`, preserve current security event and security ban routes, security event write behavior, rate-limit event behavior, alerting behavior, permission codes, OpenAPI coverage, and middleware integrations.

Required proof includes:

- security service tests
- security repository integration tests
- Security API scan or endpoint metadata tests
- security event coverage, rate-limit coverage, OpenAPI, permission, audit coverage, DB boundary, layer, and DI gates

## S8-T03 FileCenter Migration

Move file DTOs, records, permissions, service, upload policy, object key generator abstractions, repository interface, and endpoint definitions into `WeCms.Modules.FileCenter`.

Move file repository implementation into `WeCms.Modules.FileCenter.SqlSugar`, preserve current file routes, upload/download behavior, permission codes, audit metadata, security checks, OpenAPI coverage, and file storage integration through `WeCms.Infrastructure`.

Required proof includes:

- file service tests
- file repository integration tests
- FileCenter API scan or endpoint metadata tests
- file storage production checks, OpenAPI, permission, audit coverage, DB boundary, layer, and DI gates

## S8-T04 Platform Migration

Move platform/system ping, health, database probe contracts, migration probe contracts, records, and endpoint definitions into `WeCms.Modules.Platform`.

Preserve current health-check routes and anonymous/internal access policies as currently defined. Repository or probe implementations that directly touch database infrastructure must remain in an allowed infrastructure/data boundary or move to an approved Platform adapter boundary only if tests and dependency rules allow it.

Required proof includes:

- platform endpoint definition tests
- database/migration probe tests or explicit boundary tests
- OpenAPI endpoint coverage, DB boundary, layer, DI, no-controller, and minimal-api metadata gates

## S8-T05 Final Residual Migration Audit

Ensure S8 moved only Audit, Security, FileCenter, and Platform ownership and did not prematurely execute S9, S11, or S12.

Required proof includes:

- residual old namespace/path scan for S8-owned System/Persistence boundaries
- target module dependency and SQL boundary tests
- CMS/cache/AOP/EventBus/Outbox non-migration tests
- final S8 checklist and total audit
