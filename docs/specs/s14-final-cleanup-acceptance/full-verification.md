# S14-T02 Full Verification Evidence

Date: 2026-06-22

Scope: Sprint 14 full backend verification with MySQL bound to `127.0.0.1`.

Environment:

- `NUGET_PACKAGES=/private/tmp/wecms-nuget-packages`
- `NUGET_HTTP_CACHE_PATH=/private/tmp/wecms-nuget-http-cache`
- `WECMS_TEST_MYSQL_CONNECTION_STRING=server=127.0.0.1;port=3306;database=wecms_dev;uid=root;charset=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;`
- `WECMS_TEST_MYSQL_ALLOWED_HOSTS=127.0.0.1`
- `WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend` for the full backend gate because this worktree contains accepted frontend changes from earlier sprint tasks.

## Command Results

| Check | Command | Result |
| --- | --- | --- |
| Restore | `dotnet restore backend/WeCms.slnx -p:NuGetAudit=false` | Passed; 28 projects evaluated, no restore errors. |
| Warn-as-error build | `dotnet build backend/WeCms.slnx -warnaserror -p:NuGetAudit=false` | Passed; `Build succeeded`, 0 warnings, 0 errors. The transitive SoybeanAdmin production build also passed. |
| Unit tests | `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:NuGetAudit=false` | Passed; 575 passed, 0 failed, 0 skipped. |
| Architecture tests | `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:NuGetAudit=false` | Passed; 185 passed, 0 failed, 0 skipped. |
| Integration tests | `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings -p:NuGetAudit=false` | Passed; 73 passed, 0 failed, 0 skipped, MySQL `127.0.0.1`. |
| Full backend gate | `bash scripts/quality-gate-backend.sh` with the environment above | Passed; `quality-gate-backend: ok`. |

## Gate Coverage

The full backend quality gate completed all 37 steps:

- restore, warn-as-error build, unit tests, architecture tests, integration tests
- JIT publish for `backend/src/WeCms.Api/WeCms.Api.csproj`
- OpenAPI export and OpenAPI contract checks
- write endpoint permission and audit coverage checks
- system permission, locked-role seed, rate-limit, security-event, cookie-origin, CSRF migration, ThinkPHP delta, freeze baseline, production config, security, database governance, observability, file storage, and release runbook checks
- SQL-in-module, Controller/MVC, Minimal API metadata, System god module, SqlSugar boundary, DB boundary, layer dependency, DI boundary, generated artifacts, code review, replace affected rows checks
- migration/seed smoke tests: 8 passed, 0 failed, 0 skipped

## Notes

- The frontend build warning `INEFFECTIVE_DYNAMIC_IMPORT` is emitted by Vite during successful production build. It did not fail build, tests, or gate.
- No source changes were required during S14-T02 verification.
