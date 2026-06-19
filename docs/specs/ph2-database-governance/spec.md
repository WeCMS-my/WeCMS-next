# PH-2 Database Production Governance Spec

## Goal

Define and enforce the minimum production database governance baseline for WeCMS Next.

## Scope

- Production database least-privilege account documentation.
- Migration execution strategy and independent `--migrate` command.
- Backup / restore runbook.
- Database connection timeout configuration.
- Backend gate checks for PH-2 artifacts.

## Non-Goals

- No CMS phase 2 database tables.
- No legacy ThinkPHP data migration.
- No destructive migration redesign.
- No production credential values.
- No external backup service integration.

## Decisions

- Runtime API uses `ConnectionStrings:Default`.
- Production deployments should split runtime, migration, and backup accounts.
- The API host does not auto-run migrations by default in Production.
- `dotnet run --project backend/src/WeCms.Api -- --migrate` is the independent migration/seed entry.
- `Database:RunMigrationsOnStartup` controls startup migration. Development defaults to true; Production defaults to false and should use `--migrate`.
- `Database:CommandTimeoutSeconds` configures SqlSugar command timeout and fails fast on invalid values.

## Acceptance

- Database production docs and backup/restore runbook exist.
- Production template includes migration and command timeout settings.
- `--migrate` can execute migration and seed runners.
- Invalid command timeout fails fast.
- Backend gate includes PH-2 database governance check.
- Backend and frontend quality gates pass using `127.0.0.1` MySQL for database operations.
