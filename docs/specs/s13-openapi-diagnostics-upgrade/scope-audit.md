# S13 Scope Audit

## Purpose

This audit records the Sprint 13 scope boundary for the current long-lived upgrade worktree. The worktree contains earlier sprint changes, so a plain `git status -- frontend` cannot prove whether Sprint 13 itself added frontend behavior.

## Sprint 13 Owned Surface

Sprint 13 owns OpenAPI documentation, OpenAPI metadata projection, and local diagnostics only:

- `backend/src/WeCms.Api/WeCms.Api.csproj`
- `backend/src/WeCms.Api/Program.cs`
- `backend/src/WeCms.Api/Extensions/WeCmsOpenApiDocumentationExtensions.cs`
- `backend/src/WeCms.Api/Extensions/OpenApiExtensions*.cs`
- `backend/src/WeCms.Api/Extensions/WeCmsDiagnosticsExtensions.cs`
- `backend/src/WeCms.Data.SqlSugar/SqlAudit/ISqlTimingRecorder.cs`
- `backend/src/WeCms.Data.SqlSugar/SqlAudit/SqlSugarSqlAuditRegistrar.cs`
- `backend/tests/WeCms.Tests.Unit/OpenApi/*`
- `backend/tests/WeCms.Tests.Unit/Diagnostics/MiniProfilerDiagnosticsTests.cs`
- `backend/tests/WeCms.Tests.Architecture/S13OpenApiDiagnosticsTests.cs`
- `artifacts/openapi/wecms-api-v1.json`
- `docs/specs/s13-openapi-diagnostics-upgrade/*`

## Frontend Diff Attribution

The current worktree has frontend changes, but they are not Sprint 13 OpenAPI/diagnostics changes. The visible frontend delta is the system job position rename from legacy `posts` to `positions`, for example:

- `frontend/soybean-admin/src/router/dynamic-routes.ts`
- `frontend/soybean-admin/src/router/static-routes.ts`
- `frontend/soybean-admin/src/api/system/posts.ts`
- `frontend/soybean-admin/src/api/system/positions.ts`
- `frontend/soybean-admin/src/views/system/posts/PostsView.vue`
- `frontend/soybean-admin/src/views/system/positions/`

That rename is owned by Sprint 6:

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` S6-T01 requires `Posts` to become `Positions`, route `/posts` to become `/positions`, and old `Post` naming residuals to be removed.
- `docs/specs/s6-organization-migration/spec.md` states that S6 owns system job positions and must rename `Post*` to `Position*`, `/api/v1/system/posts` to `/api/v1/system/positions`, `sys_post` to `sys_position`, and `sys_user_post` to `sys_user_position`.
- `docs/specs/s6-organization-migration/checklist.md` records those S6 rename items as completed.

Therefore S13's no-frontend assertion is scoped to the Sprint 13 owned surface above, not to the accumulated long-lived worktree.

## Audit Commands

The following commands were used for the Sprint 13 final audit:

```bash
git diff --check
rg -n "AddControllers|MapControllers|ControllerBase|Microsoft\.EntityFrameworkCore|\bdynamic\b|WeCms\.Modules\.Ai|\bOpenAI\b|\bVector\b|\bPrompt\b" backend/src/WeCms.Api backend/src/WeCms.Data.SqlSugar backend/src/WeCms.Modules.Identity backend/src/WeCms.Modules.AccessControl backend/src/WeCms.Modules.Configuration backend/src/WeCms.Modules.Security backend/src/WeCms.Shared --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
rg -n "Swashbuckle\.AspNetCore|Scalar\.AspNetCore|MiniProfiler" frontend backend/src/WeCms.Modules.Cms --glob '!**/dist/**' --glob '!**/node_modules/**' --glob '!**/bin/**' --glob '!**/obj/**'
rg -n "Swashbuckle\.AspNetCore|Scalar\.AspNetCore|MiniProfiler" backend/src/WeCms.Modules.* backend/src/WeCms.Data.SqlSugar --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
jq -r '[.. | objects | keys[]? | select(startswith("x-wecms-"))] | unique | sort | .[]' artifacts/openapi/wecms-api-v1.json
bash scripts/checks/check-no-controller.sh
bash scripts/checks/check-layer-dependency.sh
bash scripts/checks/check-di-boundary.sh
bash scripts/checks/check-sqlsugar-boundary.sh
```

Results:

- `git diff --check`: passed.
- Forbidden source scan: no source matches.
- Swagger/Scalar/MiniProfiler frontend/CMS/module/Data.SqlSugar package scan: no matches.
- OpenAPI artifact includes `x-wecms-audit`, `x-wecms-module`, `x-wecms-permission`, and `x-wecms-rate-limit`.
- `check-no-controller`, `check-layer-dependency`, `check-di-boundary`, and `check-sqlsugar-boundary`: passed.
- Full backend quality gate with MySQL `127.0.0.1`: passed with `quality-gate-backend: ok`.

## Runtime Smoke Limitation

External localhost HTTP smoke for `/swagger` and `/scalar` was attempted in this Codex shell. A fresh `/private/tmp` ASP.NET Core Empty template also failed to listen on localhost in the same shell, so this is recorded as an environment limitation instead of a WeCMS-specific pass or failure.

Sprint 13 development usability is covered by source route mapping tests, OpenAPI documentation configuration tests, OpenAPI export, and the full backend quality gate.
