# S14 Repository Cleanup Audit

## Purpose

This audit records the Sprint 14 naming and forbidden-surface cleanup scan results. It distinguishes active source residuals from guardrail tests, tracked deletions, ignored build outputs, and empty local directories in the long-lived upgrade worktree.

## Commands And Results

### Transitional Module References

```bash
rg -n "WeCms\.Modules\.System" backend/src backend/tests
rg -n "WeCms\.Persistence" backend/src backend/tests
```

Result: no matches.

Classification: no active source or test references remain.

### Transitional Module Paths

```bash
find backend/src backend/tests -path '*/WeCms.Modules.System*' -o -path '*/WeCms.Persistence*'
git status --short -- backend/src/WeCms.Modules.System backend/src/WeCms.Persistence
find backend/src/WeCms.Modules.System backend/src/WeCms.Persistence -type f ! -path '*/bin/*' ! -path '*/obj/*' -print
git check-ignore -v backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll backend/src/WeCms.Modules.System/obj/project.assets.json backend/src/WeCms.Persistence/obj/project.assets.json
```

Result:

- `git status` shows tracked `D` entries for the removed `WeCms.Modules.System` and `WeCms.Persistence` source files and project files.
- Non-`bin`/`obj` files under those folders are only the deleted tracked files.
- Local `bin` and `obj` files are ignored by `.gitignore`.
- Remaining empty local directories are filesystem residue and are not source-controlled deliverables.

Classification: the old modules are removed from active source. The visible paths are deletion records plus ignored/local build residue, not active production code.

### Controller And MVC Surface

```bash
rg -n "ControllerBase|AddControllers|MapControllers|ApiController" backend/src backend/tests
rg -n "PackageReference Include=\"Microsoft\.AspNetCore\.Mvc|AddRazorPages|AddMvc|AddControllers|MapControllers|MapRazorPages" backend/src backend/tests --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
rg -n "Razor|AddRazorPages|MapRazorPages|Microsoft\.AspNetCore\.Mvc" backend/src backend/tests --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
```

Result:

- `ControllerBase`, `AddControllers`, `MapControllers`, and `ApiController` appear only in guardrail tests asserting their absence or in accepted architecture-rule text checks.
- No production `AddControllers`, `MapControllers`, `ControllerBase`, `ApiController`, `AddMvc`, Razor Pages, or MVC package registration was found.
- `using Microsoft.AspNetCore.Mvc;` appears in Minimal API endpoint files for endpoint binding attributes such as `[FromForm]` and `[FromServices]`.

Classification: no Controller, MVC Controller, Razor, or Razor Pages endpoint surface was introduced.

### System Position Legacy Naming

```bash
rg -n "sys_post|UserPost|PostService|IPostRepository|PostPermissions" backend/src backend/tests database
```

Result: no matches.

Classification: no old system-position naming residuals remain in the scanned source, tests, or database scripts.

### EF Core, Dynamic, AI Runtime, And Diagnostics Package Boundaries

```bash
rg -n "Microsoft\.EntityFrameworkCore|\bdynamic\b|WeCms\.Modules\.Ai|\bOpenAI\b|\bVector\b|\bPrompt\b" backend/src backend/tests --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
rg -n "Swashbuckle\.AspNetCore|Scalar\.AspNetCore|MiniProfiler" backend/src/WeCms.Modules.* backend/src/WeCms.Data.SqlSugar --glob '*.cs' --glob '*.csproj' --glob '!**/bin/**' --glob '!**/obj/**'
```

Result: no matches.

Classification:

- No EF Core, `dynamic` query/return type, AI runtime surface, prompt/vector runtime, or OpenAI integration was found in backend source/tests.
- Swagger, Scalar, and MiniProfiler package references do not leak into business modules or `WeCms.Data.SqlSugar`.

## Follow-Up Gates

The S14-T01 cleanup audit is source-scan driven. The related guardrails were validated by:

```bash
bash scripts/checks/check-no-controller.sh
bash scripts/checks/check-layer-dependency.sh
bash scripts/checks/check-di-boundary.sh
bash scripts/checks/check-sqlsugar-boundary.sh
bash scripts/checks/check-code-review.sh
```

Results:

- S14-focused architecture tests passed: 20/20.
- `check-no-controller`: ok.
- `check-layer-dependency`: ok.
- `check-di-boundary`: ok.
- `check-sqlsugar-boundary`: ok.
- `check-code-review`: ok.

The full backend quality gate was also run as S14-T01 gate evidence with MySQL `127.0.0.1`:

```bash
NUGET_PACKAGES=/private/tmp/wecms-nuget-packages \
NUGET_HTTP_CACHE_PATH=/private/tmp/wecms-nuget-http-cache \
WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend \
WECMS_TEST_MYSQL_CONNECTION_STRING='server=127.0.0.1;port=3306;database=wecms_dev;uid=root;charset=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;' \
WECMS_TEST_MYSQL_ALLOWED_HOSTS='127.0.0.1' \
bash scripts/quality-gate-backend.sh
```

Result: `quality-gate-backend: ok`.

Note: S14-T02 still owns the dedicated full-test checklist closure. This S14-T01 run is recorded as the task-level gate evidence for cleanup scanning.
