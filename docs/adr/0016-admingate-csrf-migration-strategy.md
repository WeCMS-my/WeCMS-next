# ADR 0016: AdminGate / CSRF Migration Strategy

## Status

Accepted.

## Context

The legacy ThinkPHP system used `AdminGate` as a centralized backend gate. It combined responsibilities that are separate concerns in WeCMS Next:

- WAF-like request feature checks.
- Configuration reads.
- Session and database token checks.
- Two-factor pending state checks.
- Permission checks.
- IP allow/deny checks.
- Security bans.
- Operation logs.
- CSRF protection for server-rendered write requests.

WeCMS Next is an ASP.NET Core Minimal API + Vue/SoybeanAdmin application. Its current auth baseline uses bearer access tokens for business API calls and a `HttpOnly; Secure; SameSite=Strict` refresh-token cookie for refresh/logout. Copying the old `AdminGate` shape would create a large cross-cutting middleware with mixed responsibilities, weak testability, and unclear ownership.

## Decision

We will not create an `AdminGateMiddleware` or equivalent all-in-one runtime compatibility layer.

Legacy AdminGate / CSRF responsibilities must be decomposed into explicit WeCMS Next components:

| Legacy responsibility | WeCMS Next owner |
| --- | --- |
| Login/session check | ASP.NET Core Authentication |
| Database token validation | Refresh Token Repository + token family revocation |
| Permission check | `RequirePermission` + `PermissionEndpointFilter` |
| 2FA pending state | Auth challenge + TwoFactorService |
| WAF feature detection | SecurityEventClassifier |
| IP allow/deny rules | `IIpRuleMatcher` + IpAccessControlMiddleware |
| Security ban | SecurityBanService + SecurityBanMiddleware |
| Operation log | Audit middleware / AuditLogService |
| Settings lookup | SettingService + SettingCache |
| Login failure limit | Rate limiting + SecurityBanService |
| Security headers | SecureHeadersMiddleware |
| Cookie-auth CSRF protection | Origin / Referer / SameSite checks, with double-submit token only when needed |

CSRF protection will not be copied as a global legacy rule. WeCMS Next uses these rules instead:

- Business APIs using bearer access tokens rely on bearer auth, CORS, permission codes, DTO validation, audit logs, and security events for high-risk operations.
- Cookie-based authentication endpoints such as refresh/logout must use strict cookie attributes and Origin / Referer validation before production hardening is closed.
- High-risk writes may additionally require current password, 2FA, or short-lived challenge verification.

## Consequences

- H1/H2 hardening tasks must implement small, testable components rather than a broad gate.
- All write endpoints still need explicit HTTP method, permission code or `AllowAnonymous` policy, DTO validation, and audit log coverage.
- High-risk operations need security events and, where required, current password, 2FA, or challenge verification.
- WeCMS Next does not provide old ThinkPHP runtime compatibility for sessions, tokens, password hashes, routes, or middleware behavior.
- CMS and AI runtime capabilities remain outside the phase-one hardening scope.

## Verification

For H0, verification is document-level:

- README, ADR-0014, and status documents must point to this ADR for AdminGate / CSRF migration.
- Searches must not find a new `AdminGateMiddleware` implementation.
- Future implementation tasks must add tests for each concrete component they introduce.
