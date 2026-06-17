# P2-SEC-001 Refresh Token Storage ADR

## Scope

Document the M2-FE refresh-token storage risk and the target migration path.

## Requirements

- Record that M2-FE temporarily stores access and refresh tokens in `localStorage` because the current backend refresh/logout contract requires body tokens.
- Record the security risk: XSS can read long-lived refresh tokens and continue a session.
- Define the preferred follow-up: move refresh token transport/storage to `HttpOnly; Secure; SameSite` cookies.
- Confirm minimum current mitigations: no trusted bypass via `v-html`, logout clears local token state, backend refresh token rotation remains the server-side mitigation.

## Non-Goals

- Do not change backend auth endpoints in this task.
- Do not implement cookie-based refresh tokens in this task.
- Do not add a runtime AI/security product feature.

## Validation

- Static check for `v-html` usage in `frontend/soybean-admin/src`.
- Static check that logout clears token state.
- Backend test evidence for refresh-token rotation/replay behavior.
