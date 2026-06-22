# WeCMS Next Foundation Stable Final Acceptance

Date: 2026-06-23

Status: `APPROVE_WITH_RELEASE_APPROVAL`

## Conclusion

WeCMS Next is accepted as a foundation-stable baseline for the current repository state.

This acceptance consolidates the completed Phase 1 foundation, Phase 1 hardening, production-hardening documentation, and S1-S14 system-foundation upgrade evidence. The active baseline is:

- `.NET 10` + ASP.NET Core Minimal APIs.
- JIT publish/runtime.
- SqlSugar ORM + MySQL.
- `WeCms.Data.SqlSugar` data platform plus `WeCms.Modules.*.SqlSugar` module persistence adapters.
- SoybeanAdmin / Vue 3 foundation admin frontend.
- Backend contract first through OpenAPI and generated frontend contract artifacts.

The foundation-stable baseline does not accept CMS content APIs, legacy ThinkPHP runtime compatibility, old data migration, plugin runtime, or AI runtime.

## Accepted Scope

Accepted foundation scope:

- M0-BE backend skeleton and JIT publish baseline.
- M1-BE system-management APIs.
- M2-FE SoybeanAdmin foundation admin frontend.
- Phase 1 hardening baseline.
- Production hardening documentation and gate wiring.
- S1-S14 destructive system-foundation upgrade.
- Final module split from old System/Persistence structure into responsibility-based modules.

Active system-foundation modules:

- `WeCms.Modules.Identity`
- `WeCms.Modules.AccessControl`
- `WeCms.Modules.Organization`
- `WeCms.Modules.Configuration`
- `WeCms.Modules.Audit`
- `WeCms.Modules.Security`
- `WeCms.Modules.FileCenter`
- `WeCms.Modules.Platform`

Active infrastructure boundaries:

- `WeCms.Data.SqlSugar`
- `WeCms.Modules.*.SqlSugar`
- `WeCms.Caching`
- `WeCms.EventBus`
- `WeCms.Aop`
- `WeCms.Infrastructure`
- `WeCms.Shared`

`WeCms.Modules.System` and `WeCms.Persistence` are no longer active source and must not be reintroduced. `WeCms.Modules.Cms` remains a placeholder only and is not part of current foundation API, OpenAPI functional coverage, or quality-gate feature coverage.

## Gate Baseline

Current frozen quality gate entrypoints:

- Backend: `bash scripts/quality-gate-backend.sh`
- Frontend: `bash scripts/quality-gate-frontend.sh`
- Production readiness: `bash scripts/quality-gate-production.sh`

Current backend gate contains 37 checks, including restore, warn-as-error build, unit tests, architecture tests, integration tests, JIT publish, OpenAPI export, permission/audit coverage, security-event coverage, cookie-origin and CSRF checks, ThinkPHP delta checks, foundation freeze checks, production baseline checks, database/layer/DI boundary checks, code-review checks, affected-row checks, and migration/seed smoke tests.

Frontend gate covers install, lint, typecheck, build, Vite proxy config validation, frontend production env, no CMS frontend, no `v-html`, generated API contract, route permission coverage, and smoke fixtures.

Production gate composes backend gate, frontend gate, production config docs, production template no-secrets, runtime wiring, release runbooks, and frontend production env checks.

## Evidence

Primary acceptance evidence:

- `docs/reports/wecms-phase1-hardening-final-acceptance.md`
- `docs/reports/wecms-production-hardening-final-acceptance.md`
- `docs/reports/system-foundation-upgrade-acceptance.md`
- `docs/specs/s14-final-cleanup-acceptance/full-verification.md`
- `docs/specs/s14-final-cleanup-acceptance/final-audit.md`
- `docs/releases/WeCMS-next H3 基础系统冻结基线.md`
- `docs/dirs/system-foundation-development-guide.md`

Most recent S14 foundation verification evidence records:

- Warn-as-error backend build: passed, 0 warnings, 0 errors.
- Unit tests: 575 passed.
- Architecture tests: 185 passed.
- Integration tests: 73 passed with MySQL `127.0.0.1`.
- Migration/seed smoke: 8 passed.
- Full backend quality gate: `quality-gate-backend: ok`.

Phase 1 hardening and production hardening reports record backend gate, frontend gate, and production readiness gate as passed for their respective accepted scopes.

## Frozen Governance Surface

The following files are the current governance and review baseline:

- `AGENTS.md`
- `code_review.md`
- `.trae/rules/wecms-engineering-principles.md`
- `docs/context/01-thinkphp-system.md`
- `docs/context/02-next-migration-plan.md`
- `docs/context/03-engineering-delivery.md`
- `docs/context/04-m0-skeleton-validation.md`
- `docs/dirs/system-foundation-development-guide.md`
- `scripts/quality-gate-backend.sh`
- `scripts/quality-gate-frontend.sh`
- `scripts/quality-gate-production.sh`

Audit result for this closeout: the listed governance and gate surfaces match the current foundation baseline: Minimal API only, JIT runtime, SqlSugar data-platform boundary, no active System/Persistence source, no CMS phase-one scope expansion, no AI runtime, backend contract first, and task-by-task gate closure.

## Release Tag

Project tags are in use. Existing local tags include `phase1-accepted`, `v1-phase1-hardening-stable`, and `v1-system-admin-production`.

Foundation stable tag selected for this closeout:

```text
v0.2.0-foundation
```

Local tag status:

```text
v0.2.0-foundation -> e19fcbea
```

Important Git boundary: the tag points to a committed Git object. This report, README update, CHANGELOG, and release note were still uncommitted when the local tag was created, so the tag does not contain those document files unless a later release owner retags after committing them.

## Residual Risks

- Human release approval is still required before pushing tags or treating this as a production deployment.
- Real production deployment, DNS/proxy changes, secret-manager provisioning, backup execution, migration execution, and admin smoke checks remain release-time operations recorded through runbooks.
- Browser-level Swagger/Scalar smoke should be rerun in a normal developer shell before an external release if required by release owner.
- CMS phase two must start from a separate spec and may not reopen the foundation baseline implicitly.

## Final Decision

The foundation baseline is stable enough to freeze locally and use as the starting point for either production deployment preparation or CMS phase-two planning, subject to human release approval and the required release-time runbooks.
