# M0-BE-006 Auth Spec

## Scope

Implement the backend-only minimal authentication surface for M0-BE:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`

This task establishes login, current-user lookup, refresh-token storage, login auditing, and explicit endpoint registration. Refresh token rotation semantics, reuse detection, family revocation, and concurrency guarantees are handled by M0-BE-007.

## Requirements

- ASP.NET Core Minimal APIs only.
- Endpoints are explicitly registered from `WeCms.Modules.System.Auth`.
- DTOs are `record` types and included in `WeCmsJsonSerializerContext`.
- Username and password are required.
- Login failure returns a generic unauthorized response and does not reveal whether the user exists.
- Login failure writes `sys_login_log` and `sys_security_event`.
- Login success:
  - verifies a stored password hash,
  - generates an access token,
  - generates a refresh token,
  - stores only the refresh token hash,
  - updates `last_login_at` and `last_login_ip`,
  - returns roles, permissions, and an empty menus array.
- `GET /api/v1/auth/me` requires a valid access token and returns user, roles, permissions, and an empty menus array.
- `POST /api/v1/auth/refresh` and `POST /api/v1/auth/logout` expose request DTOs and fail explicitly until M0-BE-007 implements rotation/logout semantics.

## Non-Goals

- Refresh token rotation.
- Concurrent refresh handling.
- Token family reuse detection.
- Role/menu management APIs.
- Frontend generated type updates.
