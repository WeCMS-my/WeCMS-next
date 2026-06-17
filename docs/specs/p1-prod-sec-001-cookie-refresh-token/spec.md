# P1-PROD-SEC-001 Cookie Refresh Token Migration

## Scope

Move browser refresh-token transport from JSON body and frontend storage to an `HttpOnly; Secure; SameSite=Strict` cookie.

## Requirements

- Login and refresh responses must not expose refresh tokens in JSON.
- Login and refresh endpoints must set the refresh token as a secure cookie.
- Refresh and logout endpoints must read the refresh token from the cookie and must not require a request body.
- Logout must revoke the refresh token family and clear the cookie.
- Frontend access tokens must stay in memory only.
- Frontend refresh and logout requests must include credentials and must not send refresh tokens in JSON.
- OpenAPI and generated frontend types must match the new contract.
- The cookie must use the `__Host-` prefix, `Path=/`, no `Domain`, `HttpOnly`, `Secure`, and `SameSite=Strict`.

## Non-Goals

- Do not add BFF or server-side session.
- Do not add new database tables or migrations.
- Do not change access-token signing or permission semantics.
- Do not implement AI runtime features.

## Security Notes

- `SameSite=Strict` is the first CSRF control for this task.
- Refresh and logout remain POST-only.
- Cross-site SPA/API deployment is not supported by this cookie profile without a separate CORS and CSRF design.
- Local HTTP development may need HTTPS to persist the Secure cookie.

## Validation

- Unit tests prove public auth response schemas do not expose refresh tokens.
- Unit tests prove refresh/logout OpenAPI operations are bodyless.
- Endpoint source tests prove refresh/logout read from cookies and set/delete the refresh cookie.
- Frontend typecheck proves no refresh-token JSON contract remains in app code.
- Backend and frontend quality gates run for the changed surface.
