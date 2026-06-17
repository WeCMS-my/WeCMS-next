# M2-FE-002 Auth Loop Spec

## Scope

Implement the M2-FE authentication loop:

- login page
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- refresh queue for concurrent 401 responses
- route authentication guard
- auth/user/permission/menu state initialization
- session restore after page refresh

## Non-Goals

- No system CRUD pages.
- No dynamic backend menu route generation yet.
- No role/menu/permission assignment UI.
- No file/log/settings UI.
- No CMS routes or `/api/v1/cms` calls.
- No AI runtime.

## Contract

OpenAPI source: `artifacts/openapi/wecms-api-v1.json`.

- `LoginRequest`: `username`, `password`
- `LoginResponse`: `accessToken`, `expiresAt`, `user`, `roles`, `permissions`, `menus`
- `POST /api/v1/auth/refresh`: no JSON request body; refresh token is sent by `HttpOnly; Secure; SameSite=Strict` cookie.
- `POST /api/v1/auth/logout`: no JSON request body; refresh token is sent by `HttpOnly; Secure; SameSite=Strict` cookie.
- `AuthMeResponse`: `user`, `roles`, `permissions`, `menus`

`AuthUserDto` currently exposes `id`, `username`, `displayName`, and `isSuperAdmin`.

## Required Behavior

- Login form rejects blank username/password before API submission.
- Login submit shows loading state.
- Login success stores access token and expiration in memory only; the backend sets the refresh token cookie.
- Login success redirects to `redirect` query value or `/dashboard`.
- Login failure displays backend `msg` or a generic failure message.
- App startup calls `/auth/refresh` when no in-memory access token exists, then calls protected APIs with the restored access token.
- Protected routes redirect unauthenticated users to `/login?redirect=<path>`.
- Logout calls backend with credentials, clears local state even if backend rejects an expired session, and redirects to `/login`.
- Request client retries a single 401 after one shared refresh promise.
- Multiple concurrent 401 responses must wait for the same refresh promise.
- Refresh failure clears session and redirects to `/login`.
- Request interceptor must not reshape backend `data`.

## Validation

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

Backend gates are not required unless backend files change.

## Audit Rules

- No backend production code changes.
- No CMS route/API call.
- No AI runtime or key.
- No sensitive token/password logging.
- Password field must not be persisted.
- Token storage must not persist access or refresh tokens in `localStorage` or `sessionStorage`.
- Button/route permission semantics must remain backend-authority only.
