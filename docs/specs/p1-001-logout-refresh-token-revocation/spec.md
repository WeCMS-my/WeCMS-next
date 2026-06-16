# P1-001 Logout Refresh-Token Revocation Spec

## Scope

Repair the `POST /api/v1/auth/logout` endpoint so the HTTP contract matches the existing refresh-token revocation service semantics.

This change covers:

- endpoint authorization metadata,
- OpenAPI export metadata,
- regression tests for endpoint source and exported contract,
- documentation of the selected logout security model.

## Decision

`POST /api/v1/auth/logout` is a refresh-token revocation endpoint, not an access-token-authenticated session API.

Therefore:

- the endpoint must be `AllowAnonymous()`,
- the request body remains required and must include `refreshToken`,
- the service continues to hash the provided refresh token, revoke the matching token family when present, and always return a uniform success response,
- security/audit logging remains in the service layer.

## Requirements

- Minimal API only; no controller introduction.
- `POST /api/v1/auth/logout` must not require a valid access token.
- `POST /api/v1/auth/logout` must still require the JSON request body declared by `LogoutRequest`.
- Exported OpenAPI for `/api/v1/auth/logout` must keep `requestBody` and must not publish `bearerAuth` security metadata.
- Source and contract regression tests must fail if logout is switched back to `RequireAuthorization()`.
- `/api/v1/auth/me` remains access-token protected.

## Non-Goals

- Changing `AuthService.LogoutAsync` token-family revocation behavior.
- Adding rate limiting in this task.
- Changing `/api/v1/auth/refresh` or `/api/v1/auth/me` semantics.
- Reworking the static OpenAPI endpoint registration strategy in this task.
