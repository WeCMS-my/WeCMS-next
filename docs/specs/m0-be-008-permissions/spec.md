# M0-BE-008 Permissions Spec

## Scope

Implement endpoint permission metadata and a secure system probe endpoint:

- `GET /api/v1/system/secure-ping`
- permission code: `sys:system:secure-ping`

## Requirements

- Endpoint uses Minimal APIs and explicit registration.
- Endpoint has permission metadata and authorization metadata.
- Missing authentication returns 401.
- Disabled user returns 401.
- Authenticated user without permission returns 403.
- Authenticated user with permission is allowed.
- Permission check reads current user status and permission assignments through Persistence.

## Non-Goals

- Full System API live/ready/db-check endpoints.
- Role/menu management.
- Frontend integration.
