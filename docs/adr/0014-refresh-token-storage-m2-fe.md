# ADR 0014: M2-FE Refresh Token Storage

## Status

Accepted for M2-FE, with follow-up required before production hardening.

## Context

The current backend auth contract accepts refresh tokens in JSON request bodies:

- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`

The M2-FE frontend therefore needs client-side access to the refresh token to refresh a session and revoke it during logout. The current implementation stores `accessToken`, `refreshToken`, and `expiresAt` in `localStorage`.

This is not a production-grade refresh-token storage model. If the frontend has an XSS vulnerability, injected script can read `localStorage` and exfiltrate both the access token and refresh token. The refresh token is the higher-risk value because it can extend a session.

## Decision

M2-FE may keep `localStorage` token storage only as a temporary compatibility decision with the current body-token backend contract.

The preferred future design is:

- store refresh tokens in `HttpOnly; Secure; SameSite` cookies;
- keep access tokens short-lived;
- rotate refresh tokens on every refresh;
- make logout revoke the server-side refresh-token family and clear frontend access-token state.

## Current Mitigations

- Frontend logout calls the backend logout endpoint when a refresh token exists and clears local token state in `finally`.
- Backend refresh-token rotation and replay handling remain the server-side protection for stolen or reused refresh tokens.
- Frontend source must not render untrusted HTML with `v-html`.
- CSP should be added as a separate production-hardening task.

## Consequences

This ADR does not remove the XSS token theft risk. It makes the risk explicit and prevents treating the current `localStorage` implementation as final M2-FE acceptance for production.
