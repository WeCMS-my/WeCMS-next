# Checklist

## Scope Control

- [x] No CMS content module/API work was introduced.
- [x] No AI runtime, provider, prompt, RAG, vector, agent, or model API code was introduced.
- [x] No MVC Controller, Razor, EF Core, runtime endpoint scanning, or business runtime code generation was introduced.
- [x] No hand-edited frontend generated API type changes were introduced.
- [x] No legacy ThinkPHP runtime compatibility or old password-hash compatibility was introduced.

## Security Findings

- [x] S-01 login path always performs password hash verification for missing, disabled, and wrong-password users.
- [x] S-02 RateLimit rejection path does not synchronously write security events or publish alerts.
- [x] S-03 IP deny path does not synchronously write security events or publish alerts.
- [x] S-04 SecurityBan middleware avoids per-request database hot-path lookups through active-ban cache.
- [x] S-05 Production requires enforce CSP.
- [x] S-06 Production requires enabled virus scanning with valid `clamav-tcp` configuration.
- [x] S-07 Original filename validation rejects path separators, reserved characters, leading dots, `.`/`..`, and trailing dot/space after normalization.

## Performance Findings

- [x] P-01 `/auth/me` access profile composition is cached by user id and permission version.
- [x] P-02 user-visible menu all-read cost is mitigated by access-profile caching.
- [x] P-03 cache prefix invalidation risk is either fixed or explicitly deferred with evidence.
- [x] P-04 outbox polling risk is either fixed or explicitly deferred with evidence.
- [x] P-05 image validation avoids whole-file `ToArray()` and validates supported image signatures.

## Architecture Findings

- [x] A-01 methods cannot combine `[Cacheable]` and `[CacheEvict]`.
- [x] A-02 exception/rejection middleware does not throw secondary response-write exceptions after `Response.HasStarted`.
- [x] A-03 local file storage uses create-new semantics and does not overwrite an existing object key.
- [x] Raw SQL guard limitations are reviewed; no new SQL-boundary bypass was introduced.

## Verification

- [x] Targeted unit tests pass.
- [x] Relevant integration or architecture tests pass.
- [x] `dotnet build backend/WeCms.slnx -warnaserror -p:SkipFrontendBuild=true` passes.
- [x] `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false -p:SkipFrontendBuild=true` passes.
- [x] `bash scripts/quality-gate-backend.sh` passes, or any environment blocker is documented with equivalent targeted proof.
- [x] Final audit passes before the branch is considered complete.
