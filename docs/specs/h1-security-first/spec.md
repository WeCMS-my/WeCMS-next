# H1 Security First

## Scope

Implement the H1 security-first hardening tasks from `docs/context/WeCMS_Next_一期后建议补齐清单详细开发修复计划书_v1.1_任务说明增强版.md`.

This spec covers exactly these tasks:

- H1-001 Cookie auth Origin / CSRF protection.
- H1-002 `IIpRuleMatcher` and `IpAccessControlMiddleware`.
- H1-003 `SecurityBan` table, service, and middleware.
- H1-004 Security center bans/status/unban.
- H1-005 Login failure limit and ban linkage.
- H1-006 Write endpoint method / permission / audit gate.
- H1-007 2FA database and backend foundation services.
- H1-008 2FA login challenge flow.
- H1-009 2FA account endpoints.
- H1-010 2FA frontend pages.
- H1-011 Admin reset 2FA.
- H1-012 Account profile/password/avatar.

## Requirements

- Do not copy the old ThinkPHP AdminGate. Split the useful controls into middleware, services, permissions, audit, rate limiting, and security events.
- Do not add CMS phase-two runtime capability, AI runtime, legacy ThinkPHP runtime compatibility, old password-hash compatibility, or old data migration.
- Keep ASP.NET Core Minimal APIs, .NET 10, JIT runtime, `WebApplication.CreateSlimBuilder(args)`, SqlSugar ORM, and SoybeanAdmin.
- Keep all database access in `WeCms.Persistence`.
- Keep business modules depending on interfaces and `WeCms.Shared` abstractions only.
- Add `I*` interfaces before adding side-effecting services.
- Add all request/response DTOs to `WeCmsJsonSerializerContext`.
- Register all endpoints explicitly and keep `OpenApiExtensions` in sync.
- Generate frontend API types from OpenAPI; do not hand-edit `frontend/soybean-admin/src/api/types/generated.ts`.
- Keep refresh token storage in the existing `HttpOnly; Secure; SameSite=Strict` cookie model and never return refresh tokens to frontend JSON.
- Protect cookie-authenticated auth endpoints with strict Origin/Referer validation before relying on SameSite alone.
- Ensure all write endpoints use explicit non-GET methods, permission metadata or explicit anonymous/internal policy, DTO validation, and audit logs.
- Record security events for high-risk operations and security rejections.
- Require current password, 2FA, or short-lived challenge for sensitive account operations where the task requires it.
- Implement every H1 subtask serially. Do not begin the next subtask until the current subtask has targeted tests, quality gate evidence, and audit evidence.

## Backend Contract Requirements

- `POST /api/v1/auth/refresh` and `POST /api/v1/auth/logout` remain bodyless cookie-auth endpoints and must be covered by Origin / CSRF protection.
- `POST /api/v1/auth/2fa/verify` and `POST /api/v1/auth/2fa/recovery-code` must be covered by the same cookie-auth Origin / CSRF protection when introduced.
- `GET /api/v1/system/security/status`, `GET /api/v1/system/security/bans`, `GET /api/v1/system/security/bans/{id}`, `POST /api/v1/system/security/bans/{id}/unban`, and `POST /api/v1/system/security/bans/batch-unban` must be permission-protected system security endpoints.
- Account self-service 2FA endpoints require an authenticated user but no system permission code.
- Admin reset 2FA requires `sys:user:reset-2fa`.
- Profile, password, avatar, and account security endpoints require an authenticated user and must not allow editing fields outside the explicit account self-service contract.

## Database Requirements

- Add migrations for new security tables, including `sys_security_ban`, `sys_user_two_factor`, and auth challenge storage.
- `sys_security_ban` must support ban type, target, reason, severity, source, expiration, revoked state, revoker, revoke reason, timestamps, and indexes for active lookups.
- 2FA secrets must be encrypted or otherwise protected at rest.
- Recovery codes must be stored as hashes only and must be one-time use.
- Auth challenges must be short-lived, one-time use, and must track failure limits.

## Frontend Requirements

- Add 2FA login challenge routing without storing refresh tokens or 2FA secrets in browser storage.
- Add account profile and account security pages.
- Add security center pages for bans/status/unban.
- Add reset 2FA action to user management with permission-controlled visibility and explicit confirmation.
- Avoid `v-html` for untrusted data.
- Use generated OpenAPI types and existing request interceptors without reshaping backend business `data`.

## Non-Goals

- No MVC Controller, Razor, Razor Pages, EF Core, dynamic query/return, runtime endpoint scanning, dynamic proxy AOP, or runtime code generation.
- No CMS content API.
- No AI module, AI provider, prompt/RAG/vector/agent runtime code, or AI key.
- No ThinkPHP runtime compatibility, old AdminGate clone, old session runtime, or old data migration.
- No new frontend generated type edits by hand.
- No broad frontend redesign beyond the H1 account/security surfaces.

## Validation

Each H1 subtask must provide task-specific evidence:

- Red -> Green -> Refactor test evidence, or explicit N/A for pure docs.
- Targeted unit, architecture, endpoint, or integration tests matching the changed behavior.
- Backend quality gate evidence after backend changes.
- Frontend typecheck/lint/build/gate evidence after frontend changes.
- OpenAPI export and generated type evidence after contract changes.
- Rule audit covering no AI runtime, no MVC/EF, no SQL boundary break, no generated hand edits, permission metadata, audit logs, and security events.

The final H1 completion audit must verify every checklist item in `checklist.md` against current files and fresh command output.
