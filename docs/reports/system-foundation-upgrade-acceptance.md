# WeCMS Next System Foundation Upgrade Acceptance

Date: 2026-06-22

Scope: destructive system-foundation upgrade from S1 through S14.

## Acceptance Conclusion

The system-foundation upgrade is accepted for the completed S1-S14 scope.

The active backend source has moved from the former `WeCms.Modules.System` and `WeCms.Persistence` structure to the final module split and SqlSugar data-platform boundary. The current verification evidence shows Minimal API, JIT publish/runtime, SqlSugar/MySQL, OpenAPI, permission/audit coverage, migration/seed smoke, and architecture guardrails passing with MySQL `127.0.0.1`.

This report does not accept CMS content APIs, legacy ThinkPHP runtime compatibility, legacy data migration, plugin runtime, or AI runtime. Those remain out of the system-foundation upgrade scope.

## Completed Sprint List

| Sprint | Scope | Evidence |
| --- | --- | --- |
| S1 | Project skeleton and dependency matrix | `docs/reports/system-foundation-s1-skeleton-validation.md` |
| S2 | Minimal API endpoint platform | `docs/specs/s2-endpoint-platform/` |
| S3 | SqlSugar data-platform foundation | `docs/specs/s3-data-sqlsugar-platform/` |
| S4 | Identity migration | `docs/specs/s4-identity-migration/` |
| S5 | AccessControl migration | `docs/specs/s5-accesscontrol-migration/` |
| S6 | Organization migration and Posts -> Positions rename | `docs/specs/s6-organization-migration/` |
| S7 | Configuration migration | `docs/specs/s7-configuration-migration/` |
| S8 | Audit, Security, FileCenter, Platform residual migration | `docs/specs/s8-system-residual-migration/` |
| S9 | Remove old System/Persistence and reset baseline | `docs/specs/s9-system-persistence-removal/checklist.md` |
| S10 | Data platform, CodeFirst, QueryFilter, SQL audit | `docs/specs/s10-data-platform-upgrade/checklist.md` |
| S11 | Cache and AOP infrastructure | `docs/specs/s11-cache-aop-upgrade/` |
| S12 | EventBus and outbox | `docs/specs/s12-eventbus-outbox-upgrade/` |
| S13 | Swagger, Scalar, MiniProfiler, OpenAPI diagnostics | `docs/specs/s13-openapi-diagnostics-upgrade/` |
| S14 | Final cleanup, full verification, documentation, acceptance | `docs/specs/s14-final-cleanup-acceptance/` |

## Module Split Results

Active system-foundation modules:

- `WeCms.Modules.Identity`: auth, account profile, users, 2FA, refresh-token flow.
- `WeCms.Modules.AccessControl`: roles, menus, permissions, access profiles, permission versioning.
- `WeCms.Modules.Organization`: departments and positions.
- `WeCms.Modules.Configuration`: settings, dictionaries, i18n messages.
- `WeCms.Modules.Audit`: audit logs and login logs.
- `WeCms.Modules.Security`: security events, rate-limit events, security bans.
- `WeCms.Modules.FileCenter`: file metadata and file operations.
- `WeCms.Modules.Platform`: platform health, version, database probes.

Active infrastructure boundaries:

- `WeCms.Data.SqlSugar`: SqlSugar platform, migration, seed, UnitOfWork, CodeFirst registry, QueryFilter, SQL audit.
- `WeCms.Modules.*.SqlSugar`: module entities, repository implementations, module CodeFirst providers.
- `WeCms.Caching`: cache abstraction and default provider.
- `WeCms.EventBus`: in-process event bus, outbox contracts, dispatcher primitives.
- `WeCms.Aop`: application-service AOP infrastructure.

`WeCms.Modules.Cms` remains a content-module placeholder only. It is not part of system-foundation API, OpenAPI functional coverage, or quality-gate feature coverage.

## Old Module Deletion Results

S9 and S14 cleanup evidence shows:

- `WeCms.Modules.System` was removed from active backend source and tests.
- `WeCms.Persistence` was removed from active backend source and tests.
- `rg "WeCms.Modules.System" backend/src backend/tests` has no active-source matches.
- `rg "WeCms.Persistence" backend/src backend/tests` has no active-source matches.
- Remaining local paths are tracked deletions, ignored build outputs, or empty local directories, not source deliverables.
- Final guardrails reject Controller/MVC/Razor, old System/Persistence references, invalid SqlSugar boundary usage, and cross-layer dependency drift.

Evidence:

- `docs/specs/s9-system-persistence-removal/checklist.md`
- `docs/specs/s14-final-cleanup-acceptance/cleanup-audit.md`

## Database Baseline Results

Current database baseline artifacts:

- `database/migrations/000001_baseline_system_schema.sql`
- `database/seeds/000002_seed_system_permissions.sql`
- `database/seeds/000003_seed_super_admin.sql`

S9 reset the system-foundation baseline after deleting old System/Persistence. S10 added CodeFirst model providers, schema validation, QueryFilter, multi-connection roles, and SQL audit guardrails while keeping production automatic DDL disabled.

S14-T02 migration/seed smoke result:

- 8 passed, 0 failed, 0 skipped.
- MySQL host: `127.0.0.1`.

## Test And Gate Result Matrix

S14-T02 full verification evidence is recorded in `docs/specs/s14-final-cleanup-acceptance/full-verification.md`.

| Check | Result |
| --- | --- |
| `dotnet restore backend/WeCms.slnx -p:NuGetAudit=false` | Passed |
| `dotnet build backend/WeCms.slnx -warnaserror -p:NuGetAudit=false` | Passed; 0 warnings, 0 errors |
| Unit tests | Passed; 575/575 |
| Architecture tests | Passed; 185/185 |
| Integration tests | Passed; 73/73, MySQL `127.0.0.1` |
| JIT publish | Passed in backend quality gate |
| OpenAPI export and OpenAPI checks | Passed |
| Write endpoint permission/audit coverage | Passed |
| Migration/seed smoke | Passed; 8/8 |
| Full backend quality gate | Passed; `quality-gate-backend: ok` |

S14-T03 documentation gates:

- `git diff --check`: passed.
- `check-code-review`: passed.
- `check-release-runbooks`: passed.
- `check-database-governance`: passed.
- `check-no-controller`: passed.
- `check-sqlsugar-boundary`: passed.
- `check-no-system-god-module`: passed.
- `check-db-boundary`: passed.
- `check-layer-dependency`: passed.
- `check-di-boundary`: passed.

## Remaining Risks

- Frontend production build emits Vite warning `INEFFECTIVE_DYNAMIC_IMPORT`; this is non-blocking in current gates but should be reviewed during future frontend optimization work.
- Live local web-host smoke for S13 Swagger/Scalar was blocked by the Codex shell/.NET web host environment, while code-level tests and backend gate passed. Re-run browser-level smoke in a normal developer shell before release.
- Historical context documents still mention old `WeCms.Persistence` or `WeCms.Modules.System` where they describe the old structure being split, removed, or replaced. Current active rules classify those mentions as historical only.
- Production database migration remains an operator-reviewed release activity. Startup automatic DDL remains forbidden outside development.

## CMS Next-Step Recommendation

Do not reopen CMS content APIs inside the system-foundation upgrade.

Recommended next step:

1. Create a separate CMS phase spec before adding CMS tables, permissions, menus, endpoints, frontend pages, or OpenAPI coverage.
2. Reuse the established module pattern: module contracts and services in `WeCms.Modules.Cms`, data access in `WeCms.Modules.Cms.SqlSugar` if and when CMS is activated.
3. Keep CMS out of system-foundation quality-gate functional coverage until the CMS phase explicitly owns that scope.
4. Preserve the no-legacy-compatibility rule: old ThinkPHP data and runtime behavior remain reference material, not runtime compatibility requirements.

## Source Evidence

- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
- `docs/specs/s14-final-cleanup-acceptance/cleanup-audit.md`
- `docs/specs/s14-final-cleanup-acceptance/full-verification.md`
- `docs/specs/s14-final-cleanup-acceptance/documentation-audit.md`
- `docs/dirs/system-foundation-development-guide.md`
