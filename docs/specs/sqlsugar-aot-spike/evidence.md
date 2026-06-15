# SqlSugar Native AOT Spike Evidence

> Archived historical evidence. This file documents the previous AOT research path only and is no longer an active runtime-baseline gate.

## Snapshot

Date: 2026-06-15.

Scope: isolated SqlSugar Native AOT probe. No production WeCMS project was modified and no real database connection was opened.

## Repository Baseline

Current production package state:

- `backend/src/WeCms.Persistence/WeCms.Persistence.csproj` has no SqlSugar package reference.
- `backend/src` has no `SqlSugarClient`, `ISqlSugarClient`, `StaticConfig.EnableAot`, or `SqlSugarCoreNoDrive.Aot` usage.
- `dotnet list backend/WeCms.sln package` showed only SDK auto-referenced AOT/linker packages in production projects. Test projects only had xUnit, test SDK, and coverlet references.

Current AOT baseline:

- `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true` succeeded for the current repository without SqlSugar.
- This proves only the current empty persistence baseline, not SqlSugar production compatibility.

## Phase 0 Baseline Recheck

Date: 2026-06-15.

Working tree:

- `git status --short` showed only the new untracked `docs/specs/` Spike files from this task.

Source search:

```bash
rg -n "SqlSugar|SqlSugarCore|SqlSugarClient|ISqlSugarClient|StaticConfig\\.EnableAot|MySqlConnector|DbConnection|DbTransaction|Ado\\.|\\bSELECT\\s+\\*\\b" backend/src
```

Result:

- No matches.
- Current production source has no SqlSugar usage, MySQL provider usage, raw database connection usage, or `SELECT *` hit under `backend/src`.

Project-file package search:

```bash
rg -n "<PackageReference|SqlSugar|SqlSugarCore|SqlSugarCoreNoDrive|MySqlConnector|Dapper|Newtonsoft.Json" backend/src backend/tests
```

Result:

- `backend/src` has no explicit package references.
- Test projects only declare `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`.
- No `SqlSugar`, `MySqlConnector`, or `Dapper` package reference appears in production or test project files.

Assets-file package search:

```bash
rg -n "SqlSugar|SqlSugarCore|SqlSugarCoreNoDrive|MySqlConnector|Dapper|Newtonsoft.Json" backend/src/*/obj/project.assets.json backend/tests/*/obj/project.assets.json
```

Result:

- No production `backend/src/*/obj/project.assets.json` match for SqlSugar, MySqlConnector, Dapper, or Newtonsoft.Json.
- Test assets contain `Newtonsoft.Json 13.0.3` through the test toolchain only.

Live `dotnet` command status:

- `dotnet list backend/WeCms.sln package` was attempted but hung without output and was cancelled.
- `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true` was attempted but hung after `Determining projects to restore...` and was cancelled.
- These runs are classified as local SDK/NuGet tooling noise for this turn, not as a new code or SqlSugar compatibility signal.

## Isolated Probe

Temporary probe path used during the initial check:

- `/private/tmp/sqlsugar-aot-probe`

Package:

- `SqlSugarCoreNoDrive.Aot` `5.1.4.186`

Runtime:

- `net10.0`
- `osx-arm64`

Probe configuration:

- `StaticConfig.EnableAot = true`
- `rd.xml` included with `SqlSugar` and probe assembly dynamic directives
- MySQL `SqlSugarClient` constructed with placeholder local credentials
- No database operation was executed

Observed dependency graph included:

- `SqlSugarCoreNoDrive.Aot` `5.1.4.186`
- `MySqlConnector` `2.2.5`
- `Newtonsoft.Json` `13.0.2`
- `Microsoft.Data.SqlClient` `5.2.2`
- `Npgsql` `8.0.7`
- `Microsoft.Data.Sqlite` `9.0.0`

## Publish Result

Command:

```bash
dotnet publish /private/tmp/sqlsugar-aot-probe/sqlsugar-aot-probe.csproj -c Release -r osx-arm64 /p:PublishAot=true
```

Result:

- Native AOT publish generated a binary.
- The published binary started successfully.
- Startup output: `SqlSugarClient initialized: SqlSugar.SqlSugarClient`.

## Warning Inventory

Publish emitted AOT, trim, and single-file warnings. Representative warning codes:

| Code | Assembly / Area | Meaning for WeCMS |
|---|---|---|
| `IL2104` | `SqlSugar.dll` | Third-party trim warnings were produced. |
| `IL3053` | `SqlSugar.dll` | Third-party AOT analysis warnings were produced. |
| `IL3000` | `SqlSugar.EntityMaintenance`, `SqlSugar.MySqlDbMaintenance`, `SqlSugar.InstanceFactory` | Uses assembly file path APIs that are unsafe under single-file/native publish assumptions. |
| `IL3002` | `SqlSugar.UtilMethods.GetStackTrace` | Uses API requiring assembly files; can behave differently in single-file output. |
| `IL2104` / `IL3053` | `Newtonsoft.Json` | Dependency introduces trim/AOT warning surface; must not become WeCMS business JSON path. |
| `IL2104` | `MySqlConnector` | MySQL provider produced trim warnings in the probe graph. |
| `IL2104` / `IL3053` | `Microsoft.Data.SqlClient` | SqlSugar AOT package pulled SQL Server provider warning surface even though WeCMS targets MySQL. |

## Decision

Current Spike decision: `BLOCKED` for production admission.

Reason:

- The probe can generate and run a Native AOT binary.
- The probe does not satisfy the WeCMS hard gate of 0 warnings and 0 errors.
- Production adoption must not proceed unless either:
  - a package/configuration/version combination reaches 0 AOT/trim/single-file warnings on local host RID and CI `linux-x64`; or
  - ADR-0006 is updated with a narrow, human-approved third-party exception, and the remaining warnings are proven unreachable or acceptable.

## Reproduction

Use the checked-in probe script:

```bash
bash docs/specs/sqlsugar-aot-spike/probe/run-probe.sh
```

Script validation in this workspace:

- `bash -n docs/specs/sqlsugar-aot-spike/probe/run-probe.sh` passed.
- A full script run on 2026-06-15 created `/private/tmp/wecms-sqlsugar-aot-probe.wLcpMs`.
- That run restored packages and emitted the managed DLL, then stayed in Native AOT publish without warning output until it was interrupted.
- The resulting log contained only restore and managed-output lines, so it is classified as an environment/tooling run failure, not as a new SqlSugar compatibility result.
- The compatibility decision above still relies on the earlier completed manual probe that generated a binary and emitted AOT/trim warnings.
- After adding script timeout protection, `PUBLISH_TIMEOUT_SECONDS=5 bash docs/specs/sqlsugar-aot-spike/probe/run-probe.sh` exited `124` and reported `Native AOT publish exceeded 5s and was stopped`, proving the script no longer hangs indefinitely in this environment.

Expected behavior under the current package version:

- The script creates a disposable project under `${TMPDIR:-/tmp}`.
- It installs `SqlSugarCoreNoDrive.Aot` `5.1.4.186`.
- It publishes with Native AOT for the host RID unless `RID` is supplied.
- It stops publish after `PUBLISH_TIMEOUT_SECONDS` seconds, default `600`, and exits `124` for environment/tooling timeout.
- It scans the publish log for warnings/errors.
- If warnings are found, it exits non-zero and reports that SqlSugar remains blocked for WeCMS production admission.

## Follow-Up

- Re-run the probe when SqlSugar or provider versions change.
- Add a repository-integrated proof only after deciding whether third-party warning exceptions are acceptable.
- Do not add SqlSugar to `WeCms.Persistence` production code while this decision remains `BLOCKED`.
