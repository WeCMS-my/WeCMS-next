# M0-BE-007 Refresh Token Rotation Spec

## Scope

Implement refresh token rotation for `POST /api/v1/auth/refresh`.

## Requirements

- The request accepts only a refresh token string.
- Refresh token plaintext is never stored.
- The repository looks up tokens by SHA-256 hash.
- A successful refresh runs in one transaction:
  - validate token exists,
  - validate token is not expired,
  - validate token is not revoked,
  - validate user is enabled,
  - revoke the old token,
  - insert the new refresh token hash in the same family,
  - issue a new access token.
- Reuse of an already revoked token revokes the whole token family and writes a security event.
- Expired token returns 401 and writes a security event.
- Disabled user returns 401 and writes a security event.
- Concurrent refresh attempts against the same token produce exactly one successful refresh.

## Non-Goals

- Logout endpoint implementation.
- Permission metadata enforcement.
- Frontend generated type updates.
