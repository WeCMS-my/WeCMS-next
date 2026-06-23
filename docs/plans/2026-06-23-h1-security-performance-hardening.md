# H1 Security / Performance Hardening Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Close every item in the 2026-06-23 secondary security/performance audit with strict serial implementation, targeted tests, backend gate proof, and final audit.

**Architecture:** Keep this as a backend/infrastructure hardening sprint over the existing foundation modules. Prefer narrow changes in the owning service/middleware/infrastructure layer, using existing interfaces and DI patterns. Do not reopen CMS, AI, legacy compatibility, or broad frontend scope.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, SqlSugar, MySQL, System.Text.Json source generation, existing WeCMS module boundaries.

---

## Baseline Evidence

- Worktree: `/Users/ali/mydev/Git/Github/WeCMS-next/.worktrees/h1-security-performance-hardening`
- Branch: `codex/h1-security-performance-hardening`
- Initial `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --no-restore` failed because `WeCms.Api.csproj` triggered the SoybeanAdmin build and `vue-tsc` was missing from worktree `node_modules`.
- Backend-only baseline passed with:

```bash
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true --no-restore
```

Result: 604 passed, 0 failed.

## Task 0: Spec And Execution Plan

**Files:**
- Create: `docs/specs/h1-security-performance-hardening/spec.md`
- Create: `docs/specs/h1-security-performance-hardening/tasks.md`
- Create: `docs/specs/h1-security-performance-hardening/checklist.md`
- Create: `docs/plans/2026-06-23-h1-security-performance-hardening.md`

**Steps:**
1. Read the audit report and mandatory project docs.
2. Map every audit finding to an explicit task or final-audit item.
3. Add the spec三件套 and execution plan.
4. Run docs/audit checks:

```bash
git diff --check
rg -n "WeCms.Persistence|AI runtime|Controller" docs/specs/h1-security-performance-hardening docs/plans/2026-06-23-h1-security-performance-hardening.md
```

Expected: no whitespace errors; any boundary terms appear only in forbidden/non-scope wording.

## Task 1: Login Dummy Hash

**Files:**
- Modify: `backend/src/WeCms.Modules.Identity/Services/AuthService.cs`
- Test: `backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.cs`
- Test helper if needed: `backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.Fakes.cs`

**Red test:** Add a fake password hasher or counter assertion proving `LoginAsync` calls `Verify` when the user is missing and when the user is disabled.

**Implementation:** Add a fixed PBKDF2 dummy hash constant in `AuthService`, compute `passwordHash = user?.PasswordHash ?? DummyPasswordHash`, always call `_passwordHasher.Verify(password, passwordHash)`, and make invalid result logic depend on `user is null`, status, or password result.

**Validation:**

```bash
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true --no-restore --filter "FullyQualifiedName~AuthServiceTests"
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true --no-restore
```

**Closure evidence (2026-06-23):**

- Added fixed dummy password-hash verification in `AuthService.LoginAsync` so missing, disabled, and wrong-password users all execute password verification before returning invalid credentials.
- Added timing-focused unit coverage in `AuthServiceTimingTests` and updated auth fakes to count password verification calls.
- Targeted tests passed after final-audit refresh: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~AuthServiceTimingTests|FullyQualifiedName~AccessProfileServiceTests"`; 29 passed.
- Full backend gate passed during task closure with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: source inspection confirmed no legacy password-hash compatibility branch or AI/CMS scope drift was introduced.

## Task 2: RateLimit/IP Deny Event Buffer

**Files:**
- Modify: `backend/src/WeCms.Modules.Security/RateLimitRecords.cs`
- Modify/Create: `backend/src/WeCms.Modules.Security/*SecurityEventBuffer*.cs`
- Modify: `backend/src/WeCms.Modules.Security/SecurityServiceCollectionExtensions.cs`
- Modify: `backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs`
- Modify: `backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs`
- Modify: `backend/src/WeCms.Api/Program.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Security/RateLimitingTests.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Api/IpAccessControlMiddlewareTests.cs`

**Red tests:** Prove rate-limit rejection and IP deny enqueue into buffer and return without calling repository/alert synchronously.

**Implementation:** Introduce bounded buffer plus flush hosted service. Group by event kind, policy/rule, IP, path, method, actor, and time bucket. Flush through existing repository/alert services in background scope. Flush failures must not affect request path.

**Validation:**

```bash
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true --no-restore --filter "FullyQualifiedName~RateLimitingTests|FullyQualifiedName~IpAccessControlMiddlewareTests"
```

**Closure evidence (2026-06-23):**

- Introduced bounded `SecurityRejectionEventBuffer` and `SecurityRejectionEventFlushHostedService`; rate-limit and IP-deny rejection paths now enqueue events and do not synchronously persist security events or publish alerts.
- Buffered flush groups repeated rejection events by kind/policy/path/method/actor/IP; IP-deny messages and rate-limit messages preserve aggregated rejected counts.
- Final-audit P2 blocker fixed: `RateLimitHitRecord` now carries `RejectedCount`, and flush writes the group count through to `IRateLimitSecurityEventService`.
- Red tests observed during final-audit fix: rate-limit aggregation tests failed before `RejectedCount` existed on `RateLimitHitRecord`.
- Targeted tests passed after final-audit fix: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~RateLimitingTests|FullyQualifiedName~SecurityRejectionEventFlushHostedServiceTests"`; 8 passed.
- Related security rejection tests passed after final-audit fix: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~SecurityBanServiceTests|FullyQualifiedName~SecurityBanMiddlewareTests|FullyQualifiedName~RateLimitingTests|FullyQualifiedName~RateLimitingSourceTests|FullyQualifiedName~SecurityRejectionEventFlushHostedServiceTests"`; 31 passed.
- Full backend gate passed during task closure with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: source checks confirm `OnRejected` and IP deny paths depend on `ISecurityRejectionEventBuffer`, not direct repository/alert services.

## Task 3: SecurityBan Cache

**Files:**
- Modify/Create: `backend/src/WeCms.Modules.Security/SecurityBanRecords.cs`
- Modify/Create: `backend/src/WeCms.Modules.Security/SecurityBanCache*.cs`
- Modify: `backend/src/WeCms.Modules.Security/SecurityBanService.cs`
- Modify: `backend/src/WeCms.Modules.Security/SecurityServiceCollectionExtensions.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Security/SecurityBanServiceTests.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Api/SecurityBanMiddlewareTests.cs`

**Red tests:** Prove repeated negative lookup hits repository only once within TTL; positive lookup is cached and invalidated by revoke/create.

**Implementation:** Add `ISecurityBanCache`, cache active ban lookups by type/target, short negative TTL, positive TTL by expires_at or bounded default, invalidation on create/revoke/batch unban.

**Closure evidence (2026-06-23):**

- Added `ISecurityBanLookupCache` and API-side cache implementation with bounded positive TTL and short negative TTL.
- `SecurityBanService.FindActiveAsync` now uses cached positive and negative lookup results and invalidates active-ban lookup keys on create, revoke, and batch unban.
- Final-audit P1 blocker fixed: negative misses are now cached through `SetMissAsync`, not repeatedly reloaded from repository.
- Red test evidence: `FindActiveAsync_CachesNegativeMisses` failed before `SetMissAsync` and `NegativeCacheTtl` existed; it passes after the fix and asserts one repository lookup for repeated misses.
- Targeted SecurityBan tests passed after final-audit fix: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~SecurityBanServiceTests|FullyQualifiedName~SecurityBanMiddlewareTests"`; 19 passed.
- Related security rejection tests passed after final-audit fix: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~SecurityBanServiceTests|FullyQualifiedName~SecurityBanMiddlewareTests|FullyQualifiedName~RateLimitingTests|FullyQualifiedName~RateLimitingSourceTests|FullyQualifiedName~SecurityRejectionEventFlushHostedServiceTests"`; 31 passed.
- Full backend gate passed during task closure with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: cache interface lives in the Security module abstraction layer; implementation is registered from API composition root and no database dependency was added to middleware.

## Task 4: AccessProfile Cache

**Files:**
- Modify/Create: `backend/src/WeCms.Modules.AccessControl/AccessProfiles/AccessProfileService.cs`
- Modify: `backend/src/WeCms.Modules.AccessControl/AccessControlServiceCollectionExtensions.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/AccessControl/AccessProfileServiceTests.cs`

**Red tests:** Prove two calls for same user/version reuse roles/permissions/menus; changing permission version composes a new profile.

**Implementation:** Cache `AccessProfileDto` by `access-profile:{userId}:{permissionVersion}` using existing caching abstractions.

**Closure evidence (2026-06-23):**

- Added `IAccessProfileCache` and API-side cache implementation; `AccessProfileService` caches composed `AccessProfileDto` by user id and permission version.
- Permission-version changes compose and cache a new profile instead of returning stale roles, permissions, menus, or button permissions.
- Targeted tests passed after final-audit refresh: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~AuthServiceTimingTests|FullyQualifiedName~AccessProfileServiceTests"`; 29 passed.
- Full backend gate passed during task closure with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: caching is isolated behind the access-control abstraction and no frontend generated type or OpenAPI contract was changed.

## Task 5: File Upload Streaming Validation And Filename Hardening

**Files:**
- Modify: `backend/src/WeCms.Modules.FileCenter/Files/FileUploadPolicies.cs`
- Modify: `backend/src/WeCms.Modules.FileCenter/Files/FileService.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Files/FileServiceTests.cs`
- Tests: add/update file upload policy tests if current file exists.

**Red tests:** Prove validation accepts valid PNG/JPEG/WebP with only head/tail reads and rejects truncated files; prove unsafe original filenames are rejected.

**Implementation:** Replace `MemoryStream.ToArray()` with bounded head/tail reads. Normalize filename to Form C, reject non-basename and reserved forms.

**Closure evidence (2026-06-23):**

- Added `FileUploadContent` as the single upload buffering boundary; `FileService` and `AccountAvatarFileService` no longer directly reopen `IFormFile` streams.
- Replaced image signature checks with bounded head/tail reads and removed whole-file `ToArray()` from upload policy validation.
- Added `FileNameSafety` and applied fail-fast filename/extension validation to system upload/download and avatar upload/download paths.
- Targeted tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~WeCms.Tests.Unit.Files.FileServiceTests|FullyQualifiedName~WeCms.Tests.Unit.Files.FileUploadStreamContractTests|FullyQualifiedName~WeCms.Tests.Unit.Auth.AccountProfileServiceTests"`; 46 passed.
- Full unit tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj`; 632 passed.
- Backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Sub agent review: Halley initially found avatar download extension, filename fail-fast, image `ToArray()`, and avatar upload regression-test gaps; after fixes Halley re-reviewed and reported no blocking issues.

## Task 6: Production CSP And Virus Scan Baseline

**Files:**
- Modify: `backend/src/WeCms.Api/Configuration/ProductionConfigurationValidator.cs`
- Modify: `backend/src/WeCms.Api/appsettings.Production.example.json`
- Tests: `backend/tests/WeCms.Tests.Unit/Configuration/ProductionConfigurationValidatorTests.cs`

**Red tests:** Production rejects `CspEnabled=false` even with report-only enabled; Production rejects `FileStorage:VirusScanEnabled=false`.

**Implementation:** Require enforce CSP and enabled ClamAV TCP scanner in Production. Keep Development report-only/noop behavior.

**Closure evidence (2026-06-23):**

- Added fail-fast Production validation for `Security:SecureHeaders:CspEnabled=true` and required enforce `Security:SecureHeaders:Csp`.
- Added fail-fast Production validation for `FileStorage:VirusScanEnabled=true`, `FileStorage:VirusScan:Provider=clamav-tcp`, and non-empty/non-placeholder scanner host.
- Updated the production example and ops docs so Production requires enforce CSP and enabled ClamAV TCP scanning; report-only/noop remain non-production or supplemental behavior only.
- Hardened `check-security-baseline.sh`, `check-production-config-baseline.sh`, and `check-file-storage-production.sh` so production template regressions are caught.
- Red tests observed before implementation: `Validate_ProductionRejectsReportOnlyCspWithoutEnforceCsp` and `Validate_ProductionRejectsDisabledVirusScan` initially failed because no exception was thrown.
- Targeted tests passed after fix: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~ProductionConfigurationValidatorTests|FullyQualifiedName~FileStorageRuntimeProcessTests"`; 31 passed.
- Related integration tests passed: `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --filter "FullyQualifiedName~FileStorageConfigurationIntegrationTests"`; 2 passed.
- Full backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: static search found only intentional negative tests, development defaults, and generic runtime defaults for disabled CSP/scanning. Sub agent Russell found stale docs/script guard gaps; after fixes those gaps were covered by targeted scripts and the full backend gate.

## Task 7: AOP Cache Metadata Conflict

**Files:**
- Modify: `backend/src/WeCms.Aop/ApplicationServiceAopInterceptor.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Aop/ApplicationServiceAopInterceptorTests.cs`

**Red test:** Interface method with both `[Cacheable]` and `[CacheEvict]` throws clear `NotSupportedException`.

**Implementation:** Validate invocation plan once before composing pipeline.

**Closure evidence (2026-06-23):**

- Added fail-fast validation in `ApplicationServiceAopInterceptor.BuildInvocationPlan` before target execution and before cache, transaction, or audit pipeline composition.
- Added `ApplicationServiceAopInterceptor_RejectsCombinedCacheableAndCacheEvictMetadata`; red test first failed because no exception was thrown, then passed after implementation and asserted the target method was not executed.
- Targeted tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~ApplicationServiceAopInterceptorTests"`; 13 passed.
- Full unit tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj`; 635 passed.
- Backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: multi-line search found no production combined `[Cacheable]` + `[CacheEvict]` usage; the only combined metadata is the intentional negative unit-test method.

## Task 8: LocalFileStorage CreateNew

**Files:**
- Modify: `backend/src/WeCms.Infrastructure/Files/LocalFileStorage.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Files/LocalFileStorageTests.cs`

**Red test:** Storing an object key that already exists does not overwrite prior content and throws a clear domain/system exception.

**Implementation:** Replace `File.Create` with `FileStream(... FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan)`.

**Closure evidence:**
- Added `StoreAsync_RejectsExistingObjectKeyWithoutOverwritingContent`; red test first failed because no exception was thrown.
- Implemented `FileMode.CreateNew` and fail-fast `DomainException(ApiCodes.Conflict, ...)` for an existing local object key.
- Targeted tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~LocalFileStorageTests"`; 4 passed.
- Related file service/upload stream tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~WeCms.Tests.Unit.Files.FileServiceTests|FullyQualifiedName~WeCms.Tests.Unit.Files.FileUploadStreamContractTests"`; 36 passed.
- Full unit tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj`; 636 passed.
- File storage gate passed: `bash scripts/checks/check-file-storage-production.sh`.
- Backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: `rg` confirmed `FileMode.CreateNew`, no `File.Create(fullPath)`, and gate tokens cover the implementation/test; sub agent review found no blockers.

## Task 9: Response Started Behavior

**Files:**
- Modify: `backend/src/WeCms.Api/Middleware/ExceptionMiddleware.cs`
- Modify: `backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs`
- Modify: `backend/src/WeCms.Api/Middleware/SecurityBanMiddleware.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Api/ResponseAndExceptionTests.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Api/IpAccessControlMiddlewareTests.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Api/SecurityBanMiddlewareTests.cs`

**Red tests:** When response has started, middleware logs/aborts/returns without throwing a secondary `InvalidOperationException`.

**Implementation:** Avoid response writes after `HasStarted`. Exception middleware logs and aborts. Rejection middleware returns if started after recording/logging.

**Closure evidence:**
- Red tests were observed before implementation: `ExceptionMiddleware_DoesNotThrowSecondaryExceptionAfterResponseStarted`, `InvokeAsync_DoesNotThrowSecondaryExceptionWhenDenyResponseAlreadyStarted`, and `InvokeAsync_DoesNotThrowSecondaryExceptionWhenBlockedResponseAlreadyStarted` failed with the existing secondary `InvalidOperationException`.
- Exception middleware now logs/aborts when an exception arrives after `Response.HasStarted`; IP deny and security-ban rejection paths return after event/hit recording when response has already started.
- Targeted middleware tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~ResponseAndExceptionTests|FullyQualifiedName~IpAccessControlMiddlewareTests|FullyQualifiedName~SecurityBanMiddlewareTests"`; 25 passed.
- Full unit tests passed inside backend gate: 643 passed.
- Security baseline scripts passed: `bash scripts/checks/check-security-baseline.sh`; `bash scripts/checks/check-security-event-coverage.sh`.
- Backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: `rg` confirmed all three middleware files guard `Response.HasStarted`; no old secondary write exception text remains in exception/rejection middleware; `StartedResponseFeature` and `AbortedRequestLifetimeFeature` tests are covered by `check-security-baseline.sh`; sub agent review found no blockers after the abort/log assertion was added.

## Task 10: MemoryCache And Outbox Infrastructure

**Files:**
- Modify: `backend/src/WeCms.Caching/MemoryCacheProvider.cs`
- Modify: `backend/src/WeCms.EventBus/OutboxDispatcherOptions.cs`
- Modify: `backend/src/WeCms.EventBus.SqlSugar/OutboxDispatcherHostedService.cs`
- Modify: `backend/src/WeCms.EventBus/OutboxDispatcher.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/Caching/MemoryCacheProviderTests.cs`
- Tests: `backend/tests/WeCms.Tests.Unit/EventBus/EventAbstractionTests.cs`

**Red tests:** Prefix invalidation avoids scanning unrelated keys or uses namespace versioning where compatible; outbox idle cycles back off and failure cycles slow down without stopping dispatcher.

**Implementation:** Keep API-compatible behavior unless a small namespace-version path is already supported. Add bounded idle/failure backoff and backlog/oldest-message logging or metrics hooks.

**Closure evidence:**
- Red tests were observed against a temporary `HEAD` baseline with only H1-SP-010 tests applied: outbox tests failed to compile because `DispatchAsync` still returned `Task`/void and `OutboxDispatchResult` did not exist. After sub agent review, an additional behavior red test proved `Failed` hosted-service cycles incorrectly selected the normal `PollInterval` instead of `FailurePollInterval`.
- Memory cache now records prefix invalidation versions instead of enumerating `keys.Keys` in `RemoveByPrefixAsync`; stale entries miss on read, unrelated keys remain readable, and new writes under the same prefix are readable.
- Outbox dispatcher now returns `OutboxDispatchResult(LockedCount, ProcessedCount, FailedCount)`; hosted service uses `IdlePollInterval` when no messages are locked and `FailurePollInterval` when a cycle has `FailedCount > 0` or throws.
- Targeted cache/outbox tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~MemoryCacheProviderTests|FullyQualifiedName~OutboxDispatcherTests|FullyQualifiedName~EventAbstractionTests.OutboxDispatcherContract_ReturnsDispatchResultForBackoff"`; 16 passed.
- Full unit tests passed: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj`; 644 passed.
- Architecture tests passed: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj`; 188 passed.
- Observability baseline script passed: `bash scripts/checks/check-observability-baseline.sh`.
- Backend gate passed with local MySQL `wecms_dev`: `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`.
- Current-task audit: `rg` confirmed prefix versioning tokens, no `foreach (var key in keys.Keys)` in `MemoryCacheProvider`, `FailedCount > 0` failure-backoff selection, outbox dispatch result/backoff tokens, and observability gate coverage. `IOutboxDispatcher.DispatchAsync` return type changed from `Task` to `Task<OutboxDispatchResult>` intentionally as an internal contract change for dispatcher observability/backoff.

## Final Audit

Run or document blockers for:

```bash
dotnet build backend/WeCms.slnx -warnaserror -p:SkipFrontendBuild=true --no-restore
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true --no-restore
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:SkipFrontendBuild=true --no-restore
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false -p:SkipFrontendBuild=true --no-restore
bash scripts/quality-gate-backend.sh
git diff --check
bash scripts/checks/check-code-review.sh
```

If `bash scripts/quality-gate-backend.sh` is blocked by local MySQL or frontend dependency bootstrap, record exact blocker output and run the closest equivalent targeted commands with `SkipFrontendBuild=true`.

**Closure evidence (2026-06-23):**

- Final-audit blockers were fixed and reverified:
  - SecurityBan negative misses are cached through `SetMissAsync`, and unit/integration test fakes implement the updated `ISecurityBanLookupCache` contract.
  - Rate-limit buffered flush writes grouped rejected count into `RateLimitHitRecord.RejectedCount`, and persisted event messages include `Rejected count` when aggregated.
  - H1-SP-001 through H1-SP-004 now have closure evidence in this plan.
- Targeted final-audit tests passed:
  - `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~SecurityBanServiceTests|FullyQualifiedName~SecurityBanMiddlewareTests|FullyQualifiedName~RateLimitingTests|FullyQualifiedName~RateLimitingSourceTests|FullyQualifiedName~SecurityRejectionEventFlushHostedServiceTests"`; 31 passed.
  - `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~AuthServiceTimingTests|FullyQualifiedName~AccessProfileServiceTests"`; 29 passed.
  - `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --filter "FullyQualifiedName~AuthIntegrationTests"`; 16 passed.
- Full verification passed:
  - `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj`; 646 passed.
  - `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj`; 188 passed.
  - `scripts/quality-gate-backend.sh`; 37/37 steps passed with `quality-gate-backend: ok`, including integration 73 passed and migration/seed smoke 8 passed.
- Static/audit scripts passed: `git diff --check`, `check-code-review`, `check-generated-test-artifacts`, `check-no-controller`, `check-no-sql-in-modules`, `check-db-boundary`, `check-sqlsugar-boundary`, `check-security-baseline`, `check-security-event-coverage`, `check-rate-limit-policy-coverage`, `check-observability-baseline`, `check-file-storage-production`, and `check-production-config-baseline`.
- Raw SQL guard review: final boundary scans and `check-no-sql-in-modules`/`check-db-boundary`/`check-sqlsugar-boundary` passed; no new SQL-boundary bypass was introduced by this hardening branch.
- Final sub agent review reported no P0/P1/P2 findings and confirmed the SecurityBan negative cache, RateLimit/IP-deny buffer aggregation, and H1-SP-001 through H1-SP-004 documentation evidence are closed.
