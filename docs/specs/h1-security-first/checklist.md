# Checklist

## Scope Control

- [x] No CMS content API is added.
- [x] No AI runtime, `WeCms.Modules.Ai`, AI provider, prompt/RAG/vector/agent runtime code, or AI key is added.
- [x] No ThinkPHP runtime compatibility or old AdminGate clone is added.
- [x] No MVC Controller, Razor, Razor Pages, EF Core, dynamic query/return, runtime endpoint scanning, dynamic proxy AOP, or runtime code generation is added.
- [x] SqlSugar/database access remains only in `WeCms.Persistence`.
- [x] Business modules depend on interfaces and `WeCms.Shared` abstractions, not persistence implementations.

## H1-001 Cookie Origin / CSRF

- [x] Cookie-auth refresh and logout reject illegal Origin.
- [x] Cookie-auth refresh and logout handle missing Origin according to strict configuration.
- [x] Referer fallback is explicit and tested.
- [x] Wildcard allowed origins are rejected.
- [x] Development/Production behavior is tested.
- [x] Rejections write security events without leaking sensitive details.
- [x] Future 2FA cookie-auth endpoints are covered by the same protection.

## H1-002 IP Access Control

- [x] `IIpRuleMatcher` exists behind an interface.
- [x] IPv4 exact rules are tested.
- [x] IPv4 CIDR rules are tested.
- [x] IPv6 exact rules are tested.
- [x] IPv6 CIDR rules are tested.
- [x] Comma, newline, and whitespace-separated rules are tested.
- [x] Invalid rules fail fast.
- [x] Rejected requests return 403 and write security events.

## H1-003 Security Ban

- [x] `sys_security_ban` migration exists.
- [x] `ISecurityBanRepository` exists in the module/shared boundary and implementation exists only in persistence.
- [x] `SecurityBanService` exists behind an interface.
- [x] `SecurityBanMiddleware` is wired in the documented order.
- [x] Active IP bans block requests.
- [x] Active user bans block requests.
- [x] Expired bans are ignored.
- [x] Revoked bans are ignored.
- [x] Ban hits write security events.

## H1-004 Security Center

- [x] Security status endpoint exists.
- [x] Ban list endpoint exists.
- [x] Ban detail endpoint exists.
- [x] Single unban endpoint exists and requires reason.
- [x] Batch unban endpoint exists and enforces a maximum batch size.
- [x] Security center permissions are seeded and granted to `super_admin`.
- [x] Unban writes audit log and security event.
- [x] Frontend security center route/page/API exists.
- [x] Frontend unban actions are permission controlled.

## H1-005 Login Failure Linkage

- [x] Login failures write login log.
- [x] Login failures increment configured username/IP counters.
- [x] Thresholds are configurable.
- [x] Threshold hits write security events.
- [x] Ban threshold creates temporary bans.
- [x] Successful login handles counters according to policy.
- [x] Error responses do not disclose whether the username exists.

## H1-006 Write Endpoint Gate

- [x] Backend gate rejects write endpoints without permission metadata unless explicitly allowlisted.
- [x] Backend gate rejects GET endpoints with write side effects.
- [x] Backend gate rejects write endpoints missing audit coverage.
- [x] Cookie-auth anonymous exceptions require Origin / CSRF protection evidence.
- [x] Allowlist entries include reason, owner module, and risk compensation.
- [x] Gate is wired into `scripts/quality-gate-backend.sh`.

## H1-007 2FA Foundation

- [x] `sys_user_two_factor` migration exists.
- [x] TOTP service tests cover valid, invalid, clock window, and replay behavior.
- [x] Secret protection does not hardcode keys.
- [x] Recovery code service stores hashes only.
- [x] Recovery codes are one-time use.
- [x] Setup returns secrets only during setup and not after confirmation.
- [x] Disable/reset clears sensitive 2FA data and writes audit evidence where applicable.

## H1-008 2FA Login Challenge

- [x] Auth challenge storage exists.
- [x] Users without enabled 2FA still receive normal login tokens.
- [x] Users with enabled 2FA receive `requiresTwoFactor` and a challenge instead of tokens.
- [x] TOTP verify signs in and sets refresh cookie.
- [x] Recovery code verify signs in and sets refresh cookie.
- [x] Expired, reused, over-limit, and invalid challenges are rejected.
- [x] 2FA failures write login logs and security events.
- [x] 2FA auth endpoints are Origin / CSRF protected.

## H1-009 Account 2FA

- [x] Account 2FA status endpoint exists.
- [x] Account 2FA setup endpoint exists.
- [x] Account 2FA confirm endpoint exists.
- [x] Account 2FA disable endpoint requires current password or TOTP.
- [x] Recovery-code regeneration invalidates old codes.
- [x] Sensitive 2FA account actions write audit logs and security events.

## H1-010 2FA Frontend

- [x] Login redirects to `/auth/two-factor` when backend requires 2FA.
- [x] TOTP verification can complete login.
- [x] Recovery-code verification can complete login.
- [x] Challenge loss on refresh sends the user back to login.
- [x] Account security page supports status, setup, confirm, disable, and recovery-code regeneration.
- [x] Recovery codes are displayed only once in the UI flow.
- [x] No refresh token or 2FA secret is stored in browser storage.
- [x] No untrusted `v-html` is introduced.

## H1-011 Admin Reset 2FA

- [x] `POST /api/v1/system/users/{id}/reset-2fa` exists.
- [x] `sys:user:reset-2fa` permission exists.
- [x] Reset clears target 2FA secret, recovery codes, last TOTP step, and enabled state.
- [x] Reset writes high-risk audit log.
- [x] Reset writes security event.
- [x] Unauthorized or out-of-policy reset attempts fail.
- [x] Frontend user management exposes a permission-controlled reset 2FA action with confirmation.

## H1-012 Account Profile / Password / Avatar

- [x] Account profile get endpoint exists.
- [x] Account profile update endpoint limits editable fields.
- [x] Password change requires old password.
- [x] Password policy failures are rejected.
- [x] Successful password change revokes refresh token family or requires re-login by documented policy.
- [x] Avatar upload uses explicit MIME, extension, and size policy.
- [x] Invalid and oversized avatars are rejected.
- [x] Account security endpoint exists.
- [x] Frontend account profile and account security pages exist.
- [x] Profile/password/avatar changes write audit logs and required security events.

## Contracts And Gates

- [x] All new DTOs are in `WeCmsJsonSerializerContext`.
- [x] All new endpoints are in `OpenApiExtensions`.
- [x] OpenAPI artifact is regenerated after contract changes.
- [x] Frontend generated types are regenerated, not hand-edited.
- [x] Backend quality gate passes with `WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend` for explicit H1 frontend scope.
- [x] Frontend quality gate passes after frontend changes.
- [x] Final H1 audit passes.
