# ADR-0013: M1-BE System Management API Scope

Status: Accepted

## Context

M0-BE established the backend-only foundation: .NET 10 Minimal APIs, JIT publish/runtime, SqlSugar isolated in `WeCms.Persistence`, Auth, permission metadata, OpenAPI export, and backend quality gate.

`docs/context/WeCMS Next M1-BE 后端-only 开发计划书 v1.0.md` defines the next phase as backend-only system management API development. The plan requested an ADR named `0011-m1-system-management-api-scope.md`, but ADR-0011 already exists for the JIT + SqlSugar Persistence decision. This ADR therefore uses the next available number to avoid an ADR collision.

## Decision

M1-BE is limited to backend system management APIs.

M1-BE must deliver:

- User, role, menu, permission, department, post, dictionary, setting, login log, audit log, security event, current-user security, and basic file metadata APIs.
- System management permission seeds and menu seeds.
- Permission metadata coverage for every M1 business endpoint.
- OpenAPI coverage for every M1 API path, request body, query contract, and response schema.
- M1 backend quality gate and CI coverage.

M1-BE must not deliver:

- `frontend/**` changes.
- SoybeanAdmin pages, generated frontend types, or `pnpm` commands.
- CMS content APIs such as channel, article, page, media, tag, site, link, workflow, or publishing.
- Runtime AI capability, `WeCms.Modules.Ai`, AI providers, prompts, RAG, vector stores, or model API calls.
- Old ThinkPHP data migration or compatibility behavior.
- Multi-tenant or plugin-system behavior.

## Constraints

- ASP.NET Core Minimal APIs only; no MVC Controller or Razor.
- `.NET 10 JIT publish/runtime` remains the runtime baseline.
- `WebApplication.CreateSlimBuilder(args)` remains the host baseline.
- All database access remains isolated in `WeCms.Persistence`.
- `WeCms.Modules.*` may define endpoint, DTO, service/use-case, validation, permission constants, and repository interfaces only.
- Repository implementations, SQL, SqlSugar entities, migrations, seeds, and transaction adapters remain in `WeCms.Persistence` or `database/**`.
- Every business endpoint must require JWT authorization and a permission code unless explicitly documented as `AllowAnonymous`.
- Every write path must record audit evidence.
- New public API, permission code, menu, database table, migration, authentication/security change, or OpenAPI contract change must have `docs/specs/<change-id>/{spec.md,tasks.md,checklist.md}` before implementation.
- Each M1 task must finish tests, quality gate, and task audit before the next M1 task starts.

## Consequences

- Frontend work remains blocked until backend OpenAPI contracts are stable.
- M1 development must be split into small, serial task closures.
- M1 quality gate must expand from M0 coverage to include system permission coverage, system OpenAPI coverage, DB boundary, layer dependency, DI boundary, no frontend change, and code review checks.
- ADR-0011 remains the M0 JIT + SqlSugar Persistence ADR; M1 scope is governed by ADR-0013.
