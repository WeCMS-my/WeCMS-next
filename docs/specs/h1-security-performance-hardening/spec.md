# H1 Security / Performance Hardening Spec

## Goal

Close the follow-up security and performance audit findings from the secondary foundation audit dated 2026-06-23 without reopening CMS, AI runtime, legacy compatibility, or broad frontend scope.

## Audit Items In Scope

This spec covers the following findings from the audit report:

- S-01 login timing side-channel mitigation with a fixed dummy password hash.
- S-02 / S-03 rate-limit and IP-deny rejection event buffering so hostile traffic cannot synchronously amplify database writes or alerts.
- S-04 active security-ban caching, including negative cache and cache invalidation on ban create/revoke.
- S-05 production CSP enforcement.
- S-06 production virus scanning baseline, with fail-fast production validation rather than a silent noop scanner.
- S-07 stricter original filename validation.
- P-01 access-profile cache keyed by user id and permission version.
- P-02 user menu all-visible-menu query pressure, mitigated by access-profile caching in this sprint.
- P-03 cache prefix invalidation risk, handled as a focused infrastructure hardening task.
- P-04 outbox dispatcher idle/failure backoff and observable backlog signals.
- P-05 streaming image structure validation that avoids whole-file `MemoryStream.ToArray()`.
- A-01 reject methods that combine `[Cacheable]` and `[CacheEvict]`.
- A-02 response-started exception behavior in error/rejection middleware.
- A-03 local file storage must use `FileMode.CreateNew` to avoid overwrites.

Raw SQL Guard remains in audit scope as a final review item. This sprint does not introduce an SQL AST parser unless final audit finds a concrete current bypass.

## Hard Boundaries

- Keep ASP.NET Core Minimal APIs, .NET 10, JIT publish/runtime, and `WebApplication.CreateSlimBuilder(args)`.
- Do not introduce MVC Controller, Razor, EF Core, runtime endpoint scanning, runtime business code generation, CMS content APIs, or AI runtime code.
- Keep database, ORM, MySQL connector, and SQL text inside `WeCms.Data.SqlSugar`, `WeCms.Modules.*.SqlSugar`, or existing approved infrastructure boundaries.
- New side-effecting services must have `I*` interfaces and constructor injection.
- Do not hand-edit frontend generated API types.
- Do not modify `frontend/**` unless a later explicitly scoped frontend task requires it; this audit sprint is backend/infrastructure focused.
- Do not add silent fallback, legacy compatibility branches, broad catch-and-ignore behavior, or production security downgrades.

## Design Summary

### Authentication

`AuthService.LoginAsync` must always invoke `IPasswordHasher.Verify` after username lookup for non-empty credentials. If the user is missing or disabled, the service uses a fixed dummy hash produced by the same PBKDF2 format and cost as normal hashes. Login result, audit, security event, and failure limiter behavior remain unchanged.

### Rejection Event Buffering

Rate-limit and IP-deny paths must enqueue bounded records into an in-memory buffer/channel and return `429`/`403` without waiting for database writes or alerts. A hosted service flushes grouped records by policy/rule, IP, path, method, and actor over a short window. Flush failures must be logged and dropped or retried according to bounded policy, never affecting the rejected request response.

### Security Ban Cache

Security ban lookups should be cached by `security:ban:{type}:{target}`. Negative lookups get a short TTL. Positive entries expire at `expires_at` when present, or a bounded default TTL for non-expiring bans. Ban create/revoke/batch-unban paths must invalidate affected cache keys.

### Access Profile Cache

`AccessProfileService` should read `permission_version` first, then cache the composed `AccessProfileDto` by `access-profile:{userId}:{permissionVersion}`. Permission changes already bump versions; new keys naturally invalidate old cached profiles.

### File Hardening

Image structure validation must read only the required head/tail bytes:

- PNG: header plus IEND tail marker.
- JPEG: SOI header plus EOI tail marker.
- WebP: RIFF/WEBP header.

Original file name validation must use normalized basename semantics, reject path separators and Windows-reserved characters, reject `.`/`..`, leading dots, and trailing dot/space.

### Production Security Baseline

Production validation must require enforce CSP (`Security:SecureHeaders:CspEnabled=true`) and a valid enforce CSP value. Production file scanning must require `FileStorage:VirusScanEnabled=true` with `clamav-tcp` and non-placeholder scanner host.

### Infrastructure Hardening

`ApplicationServiceAopInterceptor` must fail fast when a method has both cacheable and cache-evict metadata. `LocalFileStorage` must open new files with `FileMode.CreateNew`. Error/rejection middleware must avoid throwing secondary write exceptions after the response starts; it should log and abort or return according to HTTP pipeline safety.

Cache prefix and outbox hardening are limited to focused, non-contract infrastructure improvements and must not alter public API shape.

## Validation Requirements

Each task must close before the next begins:

- TDD red test first for behavior changes.
- Targeted unit/integration/architecture tests for the changed surface.
- `dotnet test ... -p:SkipFrontendBuild=true` targeted proof.
- `bash scripts/quality-gate-backend.sh` or explicitly documented environment blocker plus equivalent targeted checks.
- Rule audit against `AGENTS.md`, `code_review.md`, `.trae/rules/wecms-engineering-principles.md`.
- Final audit covers all audit report items, including items determined stale or deferred by explicit evidence.
