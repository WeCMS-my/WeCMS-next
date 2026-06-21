# S14 Final Cleanup And Acceptance Tasks

## S14-T00 Spec Trio

Create Sprint 14 spec, tasks, and checklist before S14 production code changes.

Required proof:

- `docs/specs/s14-final-cleanup-acceptance/spec.md`
- `docs/specs/s14-final-cleanup-acceptance/tasks.md`
- `docs/specs/s14-final-cleanup-acceptance/checklist.md`
- documentation consistency audit

## S14-T01 Repository Naming Cleanup

Run repository-wide cleanup scans and remediate only confirmed system-foundation residuals.

Required scans include:

```bash
rg "WeCms.Modules.System" backend/src backend/tests
rg "WeCms.Persistence" backend/src backend/tests
rg "ControllerBase|AddControllers|MapControllers|ApiController" backend/src backend/tests
rg "sys_post|UserPost|PostService|IPostRepository|PostPermissions" backend/src backend/tests database
```

Additional S14 safety scans:

```bash
rg "Microsoft\.EntityFrameworkCore|\bdynamic\b|WeCms\.Modules\.Ai|\bOpenAI\b|\bVector\b|\bPrompt\b" backend/src backend/tests --glob '*.cs' --glob '*.csproj'
rg "Swashbuckle\.AspNetCore|Scalar\.AspNetCore|MiniProfiler" backend/src/WeCms.Modules.* backend/src/WeCms.Data.SqlSugar --glob '*.cs' --glob '*.csproj'
```

Required proof:

- cleanup scans documented with result classification
- any required residual fix has failing test or scan before fix
- cleanup tests/gates pass after fix

## S14-T02 Full Test And Quality Gate

Run full backend verification with MySQL `127.0.0.1`.

Required commands:

```bash
dotnet restore backend/WeCms.slnx
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings
bash scripts/quality-gate-backend.sh
```

Required proof:

- restore/build/unit/architecture/integration results
- full backend quality gate result
- OpenAPI export result
- migration/seed smoke result

## S14-T03 Documentation Update

Update final project governance, architecture, and operations documentation.

Required documentation surfaces:

- `README.md`
- `AGENTS.md`
- `code_review.md`
- `docs/adr/*`
- `docs/context/*`
- database governance or production database docs
- runbooks under `docs/runbooks/*`

Required content:

- final module structure
- Minimal API-only decision
- how to add an Endpoint
- how to add a permission
- how to add a Repository
- how to add a CodeFirst entity
- how to generate or update migration baseline
- how to run tests and quality gates

Required proof:

- documentation consistency audit
- no contradiction with S1-S13 accepted constraints

## S14-T04 Final Acceptance Report

Create final acceptance report.

Required artifact:

- `docs/reports/system-foundation-upgrade-acceptance.md`

Required contents:

- completed sprint list
- module split results
- old module deletion results
- database baseline results
- test command and result matrix
- remaining risks
- CMS content-module next-step recommendation

Required proof:

- report exists
- report references actual verification evidence
- final S14 audit passes

## S14-T05 Final Sprint 14 Audit

Run a total audit after S14-T01 through S14-T04 complete.

Required proof:

- checklist complete
- full backend gate green
- naming cleanup scans green or documented as historical documentation-only matches
- documentation and acceptance report complete
- no Controller/MVC/Razor/EF/dynamic/AI/CMS scope drift
- sub agent audit approval
