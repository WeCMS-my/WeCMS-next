# M2-FE-001 Frontend Baseline Spec

## Scope

Create the minimum frontend foundation for M2-FE under `frontend/soybean-admin`.

This task must not implement login, CRUD pages, file management, or logs. It only establishes the frontend project, shells, and quality gate required before feature work.

## Technical Baseline

- Vue 3
- Vite
- TypeScript
- Pinia
- Vue Router
- UnoCSS
- Naive UI compatible dependency set
- pnpm package manager

SoybeanAdmin upstream currently documents Vue 3, Vite, TypeScript, Pinia, UnoCSS, and pnpm usage. This repository will keep a small local baseline instead of copying the full upstream template in this task, because M2-FE must integrate with the existing backend contract and gate one task at a time.

## Files To Add

- `frontend/soybean-admin/package.json`
- `frontend/soybean-admin/pnpm-lock.yaml`
- `frontend/soybean-admin/.npmrc`
- `frontend/soybean-admin/index.html`
- `frontend/soybean-admin/vite.config.ts`
- `frontend/soybean-admin/tsconfig.json`
- `frontend/soybean-admin/tsconfig.node.json`
- `frontend/soybean-admin/eslint.config.js`
- `frontend/soybean-admin/env.d.ts`
- `frontend/soybean-admin/.env.development`
- `frontend/soybean-admin/.env.production`
- `frontend/soybean-admin/src/main.ts`
- `frontend/soybean-admin/src/App.vue`
- `frontend/soybean-admin/src/router/index.ts`
- `frontend/soybean-admin/src/router/guards.ts`
- `frontend/soybean-admin/src/router/static-routes.ts`
- `frontend/soybean-admin/src/api/request.ts`
- `frontend/soybean-admin/src/api/types/generated.ts`
- `frontend/soybean-admin/src/stores/auth.ts`
- `frontend/soybean-admin/src/stores/permission.ts`
- `frontend/soybean-admin/src/stores/menu.ts`
- `frontend/soybean-admin/src/utils/token.ts`
- `scripts/quality-gate-frontend.sh`
- `scripts/checks/check-no-cms-frontend.sh`
- `scripts/checks/check-route-permission-coverage.sh`
- `scripts/checks/check-api-contract-generated.sh`

## Required Behavior

- App renders a basic authenticated admin shell placeholder and static dashboard route.
- Router guard redirects unauthenticated protected routes to `/login`.
- Token utility stores only access token, refresh token, and expiration timestamp in browser storage.
- Request client attaches bearer token and handles `ApiResult` shape without reshaping business `data`.
- `src/api/types/generated.ts` is explicitly marked as OpenAPI-aligned placeholder and must not claim generated freshness beyond current artifact.
- Static system route metadata includes permission arrays.
- No CMS routes, CMS pages, CMS API calls, or AI runtime code are introduced.
- Frontend quality gate runs install, lint, typecheck, build, no-CMS scan, generated contract check, and route permission coverage.

## TDD / Verification Strategy

This is a foundation/configuration task. The red step is represented by gate scripts failing before the files exist:

1. `pnpm --dir frontend/soybean-admin typecheck` fails before project creation.
2. `bash scripts/quality-gate-frontend.sh` fails before the gate exists.
3. After implementation, both must pass.

## Validation Commands

```bash
corepack prepare pnpm@10.5.0 --activate
pnpm --dir frontend/soybean-admin install --frozen-lockfile
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

Backend build/test/publish are not required for this task unless backend files change.

## Audit Rules

- No backend production code changes.
- No `/api/v1/cms`.
- No `cms/article`, `cms/channel`, `cms/page`, or `cms/tag` frontend route.
- No AI runtime or AI key.
- No secret in env files.
- New hand-written files are each <= 600 lines.
- Frontend request interceptor must not reshape business `data`.
