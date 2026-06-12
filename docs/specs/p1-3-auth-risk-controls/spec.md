# P1-3 Auth Risk Controls

## Background

The authentication path records login logs and security events, but the current behavior does not actively stop repeated attempts. For a CMS admin backend, brute-force protection must be enforced before production use.

## Decision

Add a minimal server-side auth risk layer for M1 readiness:

- Evaluate recent failed login attempts before password verification.
- Require captcha verification after repeated username + IP failures.
- Enforce rate limits for username + IP, username, and IP dimensions.
- Return `ApiCodes.TooManyRequests` when the risk policy blocks a login attempt.
- Return a two-factor login challenge when password verification succeeds for a 2FA-enabled account.
- Issue login tokens from `POST /api/v1/auth/verify-2fa` after a valid backend 2FA challenge verification.
- Record high-severity security events when rate limits or token-reuse thresholds are reached.
- Keep SQL in `WeCms.Persistence`; modules depend on repository abstractions only.

## Initial Policy

- username + IP: require captcha after 3 failed attempts in 15 minutes.
- username + IP: block after 5 failed attempts in 15 minutes.
- username: 10 failed attempts in 15 minutes.
- IP: 20 failed attempts in 15 minutes.
- refresh token reuse: severity escalates after repeated reuse events in the same window.

## Non-Goals

- No frontend flow in this step.
- No permanent account lock table in this step.
- No TOTP secret binding or recovery-code management in this step.
- No user-visible distinction between unknown user, wrong password, locked account, or rate limit details beyond captcha/429 requirements.
