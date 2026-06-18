# P2-003 AuthService Responsibility Split

## Goal

Reduce `AuthService` responsibility density without changing public HTTP contracts, auth storage semantics, or existing refresh/logout behavior.

## Context

Current repo-truth already extracted two major auth concerns:

- `AuthSessionIssuer` owns successful session issuance.
- `AuthTwoFactorChallengeService` owns two-factor challenge creation and verification.

`AuthService` still remains near the project file-size ceiling and currently mixes:

1. login orchestration;
2. refresh-token rotation and replay handling;
3. logout revocation behavior;
4. auth audit-log persistence details;
5. auth security-event persistence details.

This concentration weakens responsibility boundaries and makes further auth hardening more expensive.

## Decision

Keep `IAuthService` and all external endpoint contracts unchanged, but split internal responsibilities into smaller services:

- `AuthAuditWriter`
- `AuthSecurityEventWriter`
- `RefreshTokenRotationService`
- `LogoutTokenRevoker`

After the split:

- `AuthService` remains the public orchestration entrypoint for login, refresh, logout, me, and two-factor delegation.
- refresh rotation and replay handling move behind `RefreshTokenRotationService`.
- logout revocation behavior moves behind `LogoutTokenRevoker`.
- auth-specific audit and security-event persistence details move behind dedicated writers.

## Required Changes

1. Add internal auth writer abstractions for audit-log and security-event persistence.
2. Add a dedicated refresh rotation service that preserves all current replay-window semantics.
3. Add a dedicated logout revoker that preserves current unknown-token / revoked-token / success behavior.
4. Refactor `AuthService` to delegate refresh/logout and reuse the writers for login failure / password-rotation paths.
5. Keep unit and integration behavior unchanged.

## Non-Goals

- No endpoint contract change.
- No OpenAPI change.
- No refresh-cookie or token-storage redesign.
- No login-failure policy redesign.
- No two-factor workflow redesign.

