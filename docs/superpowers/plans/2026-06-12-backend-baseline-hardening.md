# WeCMS Backend Baseline Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore a truthful, fully runnable, high-quality backend baseline for `WeCMS-next` without compromise: stable quality gate, valid runtime permission metadata checks, correct committed OpenAPI artifact, and explicit `/auth/me` contract behavior.

**Architecture:** Keep the existing modular backend structure and Native AOT path, but harden the validation chain so repository state, runtime behavior, and committed contract artifacts cannot drift apart. Fix the smallest set of root causes in gate orchestration, auth endpoint registration, contract generation, and auth service semantics, then rerun full backend verification as the release baseline.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, Native AOT, Dapper/Dapper.AOT, xUnit, shell gate scripts, OpenAPI artifact export

---

## File Map

- Modify: `scripts/quality-gate-backend.sh`
  - Ensure every `dotnet` invocation in the backend gate uses the same writable NuGet HTTP cache path.
- Modify: `backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs`
  - Remove the current Minimal API parameter inference hazard and make auth route registration test-host-safe.
- Modify: `backend/tests/WeCms.Tests.Architecture/PermissionMetadataScanTests.cs`
  - Keep runtime permission scanning aligned with the corrected route registration shape.
- Modify: `backend/tests/WeCms.Architecture/OpenApiArtifactCompletenessTests.cs`
  - No logic change expected; use as the artifact red-light test and update only if route truth changes.
- Modify: `backend/src/WeCms.Modules.System/Auth/AuthDtos.cs`
  - Replace the weak `object` menu payload with an explicit DTO.
- Modify: `backend/src/WeCms.Modules.System/Auth/AuthService.cs`
  - Enforce logout affected-row checks and return typed menu data for `/auth/me`.
- Modify: `backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.cs`
  - Add red tests for logout row-count handling and `/auth/me` response contract.
- Modify: `artifacts/openapi/wecms-api-v1.json`
  - Re-export the committed backend contract artifact after code fixes.
- Verify only: `backend/tests/WeCms.Tests.Architecture/OpenApi/*.cs`, `backend/tests/WeCms.Tests.Integration/Auth/*.cs`, `backend/tests/WeCms.Tests.Integration/OpenApi/*.cs`
  - Use these as regression proof points; edit only if the implementation legitimately changes the expected contract.

---

### Task 1: Rebuild The Backend Gate Into A Truthful Green Baseline

**Files:**
- Modify: `scripts/quality-gate-backend.sh`
- Test: `scripts/checks/test-quality-gate-backend.sh`

- [ ] **Step 1: Write the failing shell regression check**

Add a focused assertion to `scripts/checks/test-quality-gate-backend.sh` that proves the gate passes the same cache env var to `dotnet test` and `dotnet run`, not only `dotnet publish`.

```bash
assert_contains 'env NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj'
assert_contains 'env NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet run --project backend/src/WeCms.Api -- --export-openapi'
```

- [ ] **Step 2: Run the shell check to verify it fails**

Run: `bash scripts/checks/test-quality-gate-backend.sh`
Expected: FAIL because the current script only injects `NUGET_HTTP_CACHE_PATH` for `dotnet publish`.

- [ ] **Step 3: Apply the minimal gate fix**

Update `scripts/quality-gate-backend.sh` so every backend gate `dotnet` command that can trigger restore or vulnerability-cache writes uses the same env wrapper.

```bash
run_with_dir "$REPO_ROOT" env NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --verbosity normal

run_with_dir "$REPO_ROOT" env NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet run --project backend/src/WeCms.Api -- --export-openapi "$REPO_ROOT/artifacts/openapi/wecms-api-v1.json" --nologo

run_with_dir "$REPO_ROOT" env NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --verbosity normal
```

- [ ] **Step 4: Run the shell check to verify it passes**

Run: `bash scripts/checks/test-quality-gate-backend.sh`
Expected: PASS.

- [ ] **Step 5: Run the first backend red/green proof**

Run: `bash scripts/quality-gate-backend.sh`
Expected: progress moves past Unit/OpenAPI/Architecture steps; if later steps fail, they must now be real code or artifact failures instead of `NU1900` cache noise.

- [ ] **Step 6: Commit**

```bash
git add scripts/quality-gate-backend.sh scripts/checks/test-quality-gate-backend.sh
git commit -m "fix: stabilize backend quality gate cache handling"
```

---

### Task 2: Rebuild Auth Route Registration So Runtime Metadata Tests Are Real

**Files:**
- Modify: `backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs`
- Modify: `backend/tests/WeCms.Tests.Architecture/PermissionMetadataScanTests.cs`
- Test: `backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj`

- [ ] **Step 1: Lock the current failure as the red test**

Run the focused architecture test first.

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --filter "FullyQualifiedName~PermissionMetadataScanTests" --nologo --verbosity minimal`
Expected: FAIL with `Body was inferred but the method does not allow inferred body parameters` pointing at auth endpoint discovery.

- [ ] **Step 2: Replace the hazardous lambda parameter shape**

Update `backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs` so handler resolution is explicit service injection rather than ambiguous inferred parameters.

```csharp
var login = (RouteHandlerBuilder)group.MapPost(
    "/login",
    static async (
        LoginRequest request,
        HttpContext context,
        [FromServices] AuthEndpointHandlers handlers,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handlers.LoginAsync(
            request,
            context.GetClientIp(),
            context.Request.Headers.UserAgent.ToString(),
            cancellationToken)));
```

Apply the same explicit `[FromServices]` treatment to `refresh`, `logout`, and `me`.

- [ ] **Step 3: Keep the architecture test host aligned**

If the metadata test host still needs clarity, make the auth endpoint dependency shape explicit there too, but do not weaken the assertions. The allowed result is still: authenticated endpoints are discoverable and `secure-ping` carries both authorization and permission metadata.

```csharp
builder.Services.AddScoped<AuthEndpointHandlers>();
builder.Services.AddSingleton<System.Text.Json.Serialization.JsonSerializerContext>(WeCms.Api.Json.WeCmsJsonContext.Default);
```

- [ ] **Step 4: Re-run the focused architecture test**

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --filter "FullyQualifiedName~PermissionMetadataScanTests" --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 5: Run the wider auth/openapi architecture slice**

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --filter "FullyQualifiedName~PermissionMetadataScanTests|FullyQualifiedName~AuthOpenApiGenerationTests|FullyQualifiedName~OpenApiEndpointCompletenessGenerationTests" --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs backend/tests/WeCms.Tests.Architecture/PermissionMetadataScanTests.cs
git commit -m "fix: make auth endpoint registration runtime-safe"
```

---

### Task 3: Rebuild The OpenAPI Artifact Chain So Committed Contract Matches Reality

**Files:**
- Modify: `artifacts/openapi/wecms-api-v1.json`
- Verify only: `backend/tests/WeCms.Tests.Architecture/OpenApiArtifactCompletenessTests.cs`
- Verify only: `backend/tests/WeCms.Tests.Integration/OpenApi/OpenApiDocumentEndpointTests.cs`

- [ ] **Step 1: Confirm the artifact red light**

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --filter "FullyQualifiedName~OpenApiArtifactCompletenessTests" --nologo --verbosity minimal`
Expected: FAIL because the committed artifact is missing `/health/*`, `/api/v1/system/*`, and `/api/v1/auth/me`.

- [ ] **Step 2: Re-export the backend contract artifact from the fixed code**

Run:

```bash
env NUGET_HTTP_CACHE_PATH="${TMPDIR:-/tmp}/nuget-http-cache" \
dotnet run --project backend/src/WeCms.Api --no-build -- --export-openapi /Users/ali/mydev/Git/Github/WeCMS-next/artifacts/openapi/wecms-api-v1.json
```

Expected: the file is rewritten with stable `servers[0].url == "http://localhost:5000/"` and all mapped backend paths present.

- [ ] **Step 3: Verify the artifact test turns green**

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --filter "FullyQualifiedName~OpenApiArtifactCompletenessTests|FullyQualifiedName~AuthOpenApiContractTests" --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 4: Verify runtime document and committed artifact agree**

Run: `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --no-build --no-restore --filter "FullyQualifiedName~OpenApiDocumentEndpointTests" --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add artifacts/openapi/wecms-api-v1.json
git commit -m "chore: refresh committed openapi artifact"
```

---

### Task 4: Rebuild `/auth/me` And Logout Semantics Into Explicit Contracts

**Files:**
- Modify: `backend/src/WeCms.Modules.System/Auth/AuthDtos.cs`
- Modify: `backend/src/WeCms.Modules.System/Auth/AuthService.cs`
- Modify: `backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.cs`
- Verify only: `backend/tests/WeCms.Tests.Integration/Auth/AuthLogoutTests.cs`

- [ ] **Step 1: Write the missing red tests**

Add unit tests for:

```csharp
[Fact]
public async Task LogoutAsync_WhenTokenExistsAndRevokeRowsIsNotOne_ShouldThrowSystemError()

[Fact]
public async Task GetCurrentUserAsync_ShouldReturnTypedEmptyMenus_WhenNoMenusExist()
```

For the typed menu proof, replace weak assertions on `object` with assertions against an explicit DTO shape such as:

```csharp
public sealed record CurrentUserMenuDto(
    long Id,
    string Code,
    string Name,
    string Component,
    string RoutePath,
    IReadOnlyList<CurrentUserMenuDto> Children);
```

- [ ] **Step 2: Run the focused unit tests to verify they fail**

Run: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --no-build --no-restore --filter "FullyQualifiedName~AuthServiceTests" --nologo --verbosity minimal`
Expected: FAIL because logout does not currently validate affected rows and `/auth/me` still uses `IReadOnlyList<object>`.

- [ ] **Step 3: Introduce an explicit menu DTO**

Replace the weak current-user menu contract in `AuthDtos.cs`.

```csharp
public sealed record CurrentUserMenuDto(
    long Id,
    string Code,
    string Name,
    string Component,
    string RoutePath,
    IReadOnlyList<CurrentUserMenuDto> Children);

public sealed record CurrentUserResponse(
    MeUserInfo User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<CurrentUserMenuDto> Menus);
```

- [ ] **Step 4: Enforce logout affected-row semantics**

Harden `AuthService.LogoutAsync`.

```csharp
var revokedRows = await _repository.RevokeRefreshTokenAsync(
    null,
    storedToken.Id,
    _clock.UtcNow,
    null,
    cancellationToken);

if (revokedRows != 1)
{
    throw new DomainException(ApiCodes.SystemError, "登出令牌吊销失败");
}
```

- [ ] **Step 5: Return a typed menu payload from `/auth/me`**

Until real menu loading is implemented, keep the payload explicit and empty rather than weakly typed.

```csharp
return new CurrentUserResponse(
    new MeUserInfo(user.Id, user.Username, user.DisplayName),
    roles,
    permissions,
    Array.Empty<CurrentUserMenuDto>());
```

- [ ] **Step 6: Re-run unit and integration auth checks**

Run:

```bash
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --no-build --no-restore --filter "FullyQualifiedName~AuthServiceTests" --nologo --verbosity minimal
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --no-build --no-restore --filter "FullyQualifiedName~AuthLogoutTests|FullyQualifiedName~AuthRefreshConcurrencyTests" --nologo --verbosity minimal
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/WeCms.Modules.System/Auth/AuthDtos.cs backend/src/WeCms.Modules.System/Auth/AuthService.cs backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.cs
git commit -m "fix: harden auth service contract semantics"
```

---

### Task 5: Re-run The Entire Backend Release Baseline Without Waivers

**Files:**
- Verify only: `scripts/quality-gate-backend.sh`
- Verify only: `backend/tests/WeCms.Tests.Architecture/*`
- Verify only: `backend/tests/WeCms.Tests.Integration/*`
- Verify only: `backend/tests/WeCms.Tests.Unit/*`

- [ ] **Step 1: Run the complete backend gate**

Run: `bash scripts/quality-gate-backend.sh`
Expected: PASS end-to-end with no manual bypasses and no host-cache `NU1900` failure.

- [ ] **Step 2: Run the architecture suite again as an isolated proof**

Run: `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --no-build --no-restore --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 3: Run the integration suite again as an isolated proof**

Run: `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --no-build --no-restore --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 4: Run the unit suite again as an isolated proof**

Run: `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --no-build --no-restore --nologo --verbosity minimal`
Expected: PASS.

- [ ] **Step 5: Run final hygiene checks**

Run:

```bash
git diff --check
git status --short
```

Expected: no whitespace errors; only intended tracked changes remain.

- [ ] **Step 6: Commit the final verified baseline**

```bash
git add scripts/quality-gate-backend.sh scripts/checks/test-quality-gate-backend.sh backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs backend/src/WeCms.Modules.System/Auth/AuthDtos.cs backend/src/WeCms.Modules.System/Auth/AuthService.cs backend/tests/WeCms.Tests.Architecture/PermissionMetadataScanTests.cs backend/tests/WeCms.Tests.Unit/Auth/AuthServiceTests.cs artifacts/openapi/wecms-api-v1.json
git commit -m "fix: restore truthful backend baseline"
```

---

## Self-Review

- Spec coverage: this plan covers the four audit red lights directly tied to baseline truthfulness: gate instability, runtime permission scan failure, stale committed OpenAPI artifact, and weak `/auth/me` plus logout semantics.
- Placeholder scan: no `TODO` or deferred placeholders remain; every task has exact files, commands, and concrete code snippets.
- Type consistency: the plan standardizes the current-user menu payload on `CurrentUserMenuDto` and preserves existing `LoginRequest`, `RefreshRequest`, `LogoutRequest`, `CurrentUserResponse`, `AuthEndpointHandlers`, and `PermissionEndpointFilter` naming.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-12-backend-baseline-hardening.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
