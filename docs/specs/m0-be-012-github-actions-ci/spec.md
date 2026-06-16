# M0-BE-012 GitHub Actions CI Spec

## Scope

Add `.github/workflows/backend-quality-gate.yml` for backend-only M0-BE validation.

## Triggers

- push to `main`
- pull request targeting `main`
- manual `workflow_dispatch`

## Required Flow

- checkout repository
- setup .NET 10
- install ripgrep for shell quality-gate scanners
- start MySQL 8
- create a test connection string in `WECMS_TEST_MYSQL_CONNECTION_STRING`
- run `bash scripts/quality-gate-backend.sh`

## Constraints

- CI must not run frontend `pnpm` commands.
- CI must not run AOT publish, `/p:PublishAot=true`, Dapper baseline, Dapper.AOT, IL2026, or IL3050 gates.
- CI must fetch enough history for backend-only frontend diff checks.
