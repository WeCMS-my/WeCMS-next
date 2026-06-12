# P1-4 JWT Token Version Validation

## Background

Access tokens currently carry `securityStamp` and `permissionVersion`, but endpoints that only use `RequireAuthorization()` can complete authentication without proving those claims still match the user row. Permission-protected endpoints query the database for active state and permissions, but `/api/v1/auth/me` and `/api/v1/auth/logout` need the same token invalidation boundary.

## Decision

WeCMS keeps short-lived access tokens and also validates token version claims on every authenticated request:

- `securityStamp` in the JWT must match `sys_user.security_stamp`.
- `permissionVersion` in the JWT must match `sys_user.permission_version`.
- `sys_user.status` must remain active and `deleted_at` must be null.
- A mismatch fails authentication and returns HTTP 401 through the normal bearer pipeline.

This makes password reset, forced logout, user disable, and role/permission version changes invalidate existing access tokens before endpoint handlers run.

## Scope

In scope:

- Add a token version validation abstraction.
- Implement the abstraction in Persistence using explicit SQL.
- Wire JWT Bearer token validated events through DI.
- Add regression tests for disabled users and stale token claims.

Out of scope:

- Changing refresh token rotation semantics.
- Adding new public API endpoints.
- Changing OpenAPI response schemas.
- Implementing AI runtime capability.
