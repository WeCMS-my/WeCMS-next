# M2-FE-000 Frontend Foundation Spec

## Scope

Prepare M2-FE execution before frontend implementation.

M2-FE is the first frontend phase for WeCMS Next. It consumes the accepted M1-BE system management APIs and must deliver a usable admin console:

```text
login -> auth state restore -> dynamic menu/routes -> permission controls -> system management CRUD -> file management -> log viewing
```

This spec does not implement UI. It defines the execution boundary, task split, contract risks, and validation rules that every following M2-FE task must follow.

## Source Documents

- `AGENTS.md`
- `code_review.md`
- `.trae/rules/wecms-engineering-principles.md`
- `docs/context/01-thinkphp-system.md`
- `docs/context/02-next-migration-plan.md`
- `docs/context/03-engineering-delivery.md`
- `docs/context/04-m0-skeleton-validation.md`
- `docs/context/一期范围调整说明.md`
- `docs/context/WeCMS-next M2-FE 基础系统前端开发计划书.md`
- `artifacts/openapi/wecms-api-v1.json`

## Hard Boundaries

- Use SoybeanAdmin/Vue 3 frontend under `frontend/soybean-admin`.
- Do not add CMS content features, CMS routes, CMS permissions, CMS menus, CMS pages, or `/api/v1/cms` calls.
- Do not add runtime AI functionality, AI pages, AI provider code, prompt/RAG/vector/agent code, or AI keys.
- Frontend data contracts must follow backend DTO/OpenAPI.
- Do not hand-edit generated service output if a generated directory is introduced.
- Request interceptors may handle token, `code`, `msg`, 401, and 403 only; they must not reshape business `data`.
- Dynamic routes must use a component whitelist; backend `component` strings must never become arbitrary imports.
- Frontend permission checks are only UI controls; backend remains the authorization boundary.
- Dangerous writes require confirmation and success/failure feedback.
- Every page must handle loading, error, and empty states.

## Current Repository Facts

- `frontend/soybean-admin` does not exist yet.
- There is no frontend `package.json`, lockfile, Vite config, or TypeScript config.
- `pnpm` is not currently available on PATH; `node`, `npm`, and `corepack` are available.
- Existing quality gate is backend-only and includes `check-no-frontend-change.sh`.
- OpenAPI path coverage for M2-FE required APIs is complete.

## OpenAPI Contract Risks

- `AuthMenuDto` is too small for full dynamic route generation. It contains basic menu fields, while the frontend route contract needs component, icon, hidden, keepAlive, externalUrl, permissionCode, status, builtin, and children metadata. Until the backend contract is expanded, dynamic routing must use `/api/v1/system/menus/tree` as the planned transition path.
- File download/preview paths exist and use `sys:file:download`, but OpenAPI describes JSON `ApiResult<Object>` rather than blob or streamed media. File UI implementation must verify actual backend behavior before finalizing preview/download client code.
- User list/detail expose `deptId`, not department name. User UI must map department names from the department tree/list or explicitly display IDs until backend provides display names.

## Serial Task Plan

M2-FE must be executed one task at a time. A task is complete only after its tests, frontend gate or documented equivalent, and task audit pass.

1. `M2-FE-000`: frontend execution spec, task split, contract risks, and readiness checklist.
2. `M2-FE-001`: frontend foundation under `frontend/soybean-admin`: package manager, Vite/Vue/TypeScript/SoybeanAdmin baseline, env, request client shell, token store shell, router guard shell, base layout, and frontend quality gate script.
3. `M2-FE-002`: Auth loop: login, logout, refresh, `/auth/me`, refresh queue, session restore, route auth guard, and user state store.
4. `M2-FE-003`: permissions and navigation: permission store, `hasPermission` helpers, dynamic menu, dynamic route whitelist, `PermissionButton`, and menu rendering.
5. `M2-FE-004`: users page: list/search/filter, create, edit, delete, enable/disable, reset password, assign roles, assign posts, detail, and no password hash exposure.
6. `M2-FE-005`: roles and permissions pages: role CRUD, permission CRUD/tree, role permission/menu assignment, locked-role UI protection, builtin permission UI protection, and forced-call error display.
7. `M2-FE-006`: menus and departments pages: menu tree CRUD, department tree CRUD, enable/disable, builtin menu protection, and parent cycle prevention.
8. `M2-FE-007`: posts and dicts pages: post CRUD/status, dict type CRUD, dict value CRUD, default/status display, and uniqueness/error handling.
9. `M2-FE-008`: settings and logs pages: setting list/edit, sensitive value masking, login logs, audit logs, security events, filters, details, and read-only log behavior.
10. `M2-FE-009`: files page: list, upload, browser SHA-256, size/MIME capture, file type/size limits, preview, download, delete, and permission-gated controls.
11. `M2-FE-010`: M2-FE quality gate and acceptance: lint, typecheck, build, API contract check, route permission coverage, no-CMS scan, optional smoke, and M2-FE acceptance report.
12. `M2-FE-011`: final read-only audit for all M2-FE changes.

## Required Validation Pattern

For frontend changes:

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
```

For backend support changes, also run:

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
bash scripts/quality-gate-backend.sh
```

For documentation-only governance tasks, build/test/publish/typecheck/lint are not required, but document consistency checks and rule audit are required.

## Frontend Gate Requirements

`scripts/quality-gate-frontend.sh` must eventually cover:

- install with frozen lockfile
- lint
- typecheck
- build
- no `/api/v1/cms` or CMS frontend route/page scan
- API contract generated/type alignment check
- route permission coverage check

## Acceptance Rules

- Every M2-FE included capability from the plan is represented in a task.
- No M2-FE excluded capability is represented as an implementation task.
- Every implementation task has an explicit validation and audit boundary.
- Known OpenAPI risks are recorded before coding begins.
- Next implementation task is `M2-FE-001`; no login/page work starts before the frontend foundation and gate exist.
