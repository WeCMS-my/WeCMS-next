# M0-BE-013 Persistence Access Strategy Spec

## Scope

Define the boundary and execution mode for all persistence access in WeCMS Next.

## Requirements

- `WeCms.Persistence` is the only production project that may reference `SqlSugar`, MySQL connection types, SQL text, or direct DB client objects.
- `WeCms.Modules.*` may only define:
  - Repository ports (`I*Repository`)
  - domain services / handlers
  - endpoint registration and DTOs
- `WeCms.Persistence` repository implementations must:
  - perform data access through async APIs
  - pass `CancellationToken` through all repository entry points
  - use explicit field selection (no `SELECT *`)
  - avoid `dynamic`
- No module project (`WeCms.Modules.System`, `WeCms.Modules.Cms`) may contain SQL string literals for data access.

## Strategy

- Keep persistence implementation SQL-first in SqlSugar by default.
- ORM-first migration is only allowed when:
  - an ADR explicitly approves it
  - equivalent SQL semantics and test coverage are added in the same change set
- Security-sensitive queries must remain explicit and auditable SQL until ADR approval exists for ORM abstraction.

## Compliance Artifacts

- Repository async conversion in `WeCms.Persistence/Modules/System/Permissions/PermissionRepository.cs`
- Architecture tests that validate System API permission metadata
- OpenAPI and endpoint tests covering auth/logout refresh behaviors
