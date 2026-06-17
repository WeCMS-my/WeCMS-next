# P2-FE-003 Auth Visible Menu Tree

## Goal

Fix the ordinary-authorized-user navigation gap by making `/api/v1/auth/login` and `/api/v1/auth/me` return the current user's visible system menu tree.

## Scope

This task includes:

- backend auth contract changes for `LoginResponse` and `AuthMeResponse`
- repository support for loading menus visible to a user
- permission-filtered menu tree generation for auth responses
- frontend fallback changes so sidebar and dynamic route registration can use auth-returned menus when `sys:menu:tree` is absent
- OpenAPI and generated frontend type alignment
- task-specific tests for the broken ordinary-user scenario

This task does not include:

- changing `/api/v1/system/menus/tree` permission requirements
- granting `sys:menu:tree` to ordinary users
- menu CRUD page changes
- CMS routes or APIs
- AI runtime

## Problem Statement

Current frontend behavior only fetches `/api/v1/system/menus/tree` when the user has `sys:menu:tree`.

Current backend behavior returns `menus: []` from both auth endpoints.

As a result, a user who has page permissions such as `sys:user:list` but does not have `sys:menu:tree` can reach authorized static routes yet still sees an empty sidebar because the auth fallback menu source is empty.

## Contract Changes

OpenAPI source: `artifacts/openapi/wecms-api-v1.json`.

- `LoginResponse.menus` changes from a flat `AuthMenuDto[]` placeholder to `MenuTreeDto[]`
- `AuthMeResponse.menus` changes from a flat `AuthMenuDto[]` placeholder to `MenuTreeDto[]`

The auth menu tree must reuse the same structural fields already exposed by `MenuTreeDto`, including:

- `id`
- `parentId`
- `type`
- `code`
- `path`
- `component`
- `title`
- `i18nKey`
- `icon`
- `sort`
- `hidden`
- `keepAlive`
- `externalUrl`
- `permissionCode`
- `status`
- `isBuiltin`
- `children`

## Required Behavior

- `/auth/login` returns the caller's roles, permissions, and visible menu tree after successful login.
- `/auth/me` returns the caller's roles, permissions, and visible menu tree.
- Visible auth menus are filtered by the caller's effective permissions.
- Disabled menus must not appear in auth menu trees.
- Button-only menus must not become sidebar items or dynamic routes, but may remain in the tree if needed for hierarchy consistency only when downstream filtering already excludes them from rendering.
- The frontend must use auth-returned menu trees as the fallback navigation and dynamic-route source when `sys:menu:tree` is unavailable.
- Users who do have `sys:menu:tree` may still load the full backend menu tree through the existing endpoint.

## Data Rules

- Menu data must be read only through `WeCms.Persistence`.
- Repository methods must accept `CancellationToken`.
- SQL must explicitly list fields and must not use `SELECT *`.
- Menu visibility must be derived from the user's enabled roles and effective permission codes.
- Soft-deleted menus must be excluded.

## Validation

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-backend.sh
git diff --check
```

## Audit Rules

- No MVC, Razor, EF Core, `dynamic`, or runtime route scanning
- No `SELECT *`
- No SQL text outside `WeCms.Persistence`
- No silent compatibility fallback that hides authorization state
- No frontend hand edits to unrelated generated contracts
