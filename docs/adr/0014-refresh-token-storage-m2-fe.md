# ADR 0014: Auth Token Storage Final State

## Status

Accepted.

## Context

M2-FE originally accepted a temporary frontend token-storage model while the backend refresh/logout contract still used refresh tokens in JSON request bodies.

That temporary model is no longer the current baseline.

The current backend auth contract stores the refresh token in the `__Host-wecms_refresh` cookie:

- `HttpOnly = true`
- `Secure = true`
- `SameSite = Strict`
- `Path = /`
- expiration and max-age aligned with the refresh-token lifetime

The current frontend token state contains only:

- `accessToken`
- `expiresAt`

The frontend removes the previous localStorage token key and keeps access-token state in memory. The frontend does not store refresh tokens in localStorage.

## Decision

Refresh tokens must be stored in the `HttpOnly; Secure; SameSite=Strict` cookie issued by the backend. The frontend must not persist refresh tokens in localStorage, sessionStorage, IndexedDB, or other script-readable storage.

Access tokens remain short-lived bearer tokens and are held only in frontend memory. They are sent in the `Authorization: Bearer` header for protected business APIs.

Refresh and logout use the refresh cookie:

- `POST /api/v1/auth/refresh` reads the refresh cookie, rotates the server-side token, returns a new access token, and sets a new refresh cookie.
- `POST /api/v1/auth/logout` reads the refresh cookie, revokes the server-side refresh-token family, clears the refresh cookie, and clears frontend access-token state.
- replayed, revoked, expired, or unknown refresh tokens are rejected and classified through the existing auth/security event path.

Cookie-based auth endpoints must be protected as cookie-auth endpoints:

- maintain strict cookie attributes;
- add Origin / Referer validation as part of H1 hardening;
- add double-submit CSRF token only if Origin / Referer / SameSite coverage is insufficient for a specific flow.

## Rejected Alternatives

- **Refresh token in localStorage**: rejected because XSS can read and exfiltrate it.
- **Refresh token in request body from frontend state**: rejected for the same reason if the frontend can read the token.
- **Legacy ThinkPHP Session / token compatibility**: rejected; WeCMS Next does not implement old runtime compatibility.
- **Global legacy CSRF copy**: rejected; CSRF controls must be scoped to Cookie-based auth surfaces and high-risk writes.

## Consequences

- `localStorage` is a historical temporary implementation detail, not an accepted baseline.
- Frontend code must not add `refreshToken` back to generated state, Pinia state, localStorage, sessionStorage, or request DTOs.
- Backend OpenAPI for login/refresh responses must not expose refresh tokens.
- Cookie auth endpoints remain a hardening priority until Origin / Referer coverage is implemented and tested.
- This decision aligns with [ADR-0016](0016-admingate-csrf-migration-strategy.md): old CSRF/AdminGate behavior is decomposed instead of copied.
