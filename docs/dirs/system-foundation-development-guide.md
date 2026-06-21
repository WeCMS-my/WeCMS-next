# System Foundation Development Guide

This guide is the current operating guide for system-foundation backend work after the S1-S14 destructive upgrade.

## Current Module Structure

Production system-foundation modules are split by responsibility:

- `WeCms.Modules.Identity`: auth, account profile, users, 2FA, refresh-token flow.
- `WeCms.Modules.AccessControl`: roles, menus, permissions, access profiles, permission versioning.
- `WeCms.Modules.Organization`: departments and positions.
- `WeCms.Modules.Configuration`: settings, dictionaries, and i18n messages.
- `WeCms.Modules.Audit`: audit logs and login logs.
- `WeCms.Modules.Security`: security events, rate-limit events, security bans.
- `WeCms.Modules.FileCenter`: file metadata and file operations.
- `WeCms.Modules.Platform`: platform health, version, and database probes.

Data access is split into:

- `WeCms.Data.SqlSugar`: SqlSugar platform, connection registration, UnitOfWork, migration, seed, CodeFirst registry, query filters, SQL audit.
- `WeCms.Modules.*.SqlSugar`: module-specific SqlSugar entities, repositories, and model providers.

`WeCms.Modules.System` and `WeCms.Persistence` are no longer active source projects. Historical documentation may mention them only as migration history.

## Add An Endpoint

1. Start with `docs/specs/<change-id>/{spec.md,tasks.md,checklist.md}` when the change adds or changes a public API, OpenAPI contract, permission, menu, security policy, database shape, or reaches the spec threshold in `AGENTS.md`.
2. Add DTOs in the owning module `Contracts` folder and register them in `backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs`.
3. Add an explicit Minimal API endpoint definition in the owning module. Do not add Controller, ControllerBase, AddControllers, MapControllers, Razor, or endpoint runtime scanning.
4. Register the endpoint through the module endpoint route builder extension and the API host composition path.
5. Attach permission metadata or an explicit internal/anonymous policy. Write operations must attach audit metadata.
6. Add unit tests for endpoint metadata and service behavior, integration tests for HTTP behavior when needed, and OpenAPI contract tests when the contract changes.
7. Run the relevant targeted tests, then `bash scripts/quality-gate-backend.sh`.

## Add A Permission

1. Define the permission code in the owning module permission definition file.
2. Bind endpoint metadata to the same permission code.
3. Update seed SQL in `database/seeds/000002_seed_system_permissions.sql` and ensure `super_admin` receives the permission through the seed path.
4. Update frontend route or button permission usage only when the task scope includes frontend.
5. Add or update permission coverage tests and OpenAPI permission tests.
6. Run permission coverage checks through the backend quality gate.

## Add A Repository

1. Define the repository interface in the owning `WeCms.Modules.*` project or `WeCms.Shared` only when it is truly shared.
2. Implement the repository in the matching `WeCms.Modules.*.SqlSugar` project.
3. Keep SQL and data mapping inside the repository implementation. Keep business rules, authorization, audit orchestration, and transactions in service/use-case code.
4. Support `CancellationToken` on all repository methods.
5. Avoid `SELECT *`, dynamic query/return types, user-input SQL concatenation, and non-whitelisted sort fields.
6. Register the implementation from the module `.SqlSugar` service collection extension.
7. Add integration tests for SQL behavior and affected-row checks for writes.

## Add A CodeFirst Entity

1. Place the entity in `WeCms.Data.SqlSugar` only for platform entities, or in the owning `WeCms.Modules.*.SqlSugar` project for module entities.
2. Register the entity through the module CodeFirst model provider or data platform registry.
3. Keep CodeFirst as the model and validation path; do not treat it as automatic production DDL.
4. Add schema validation or architecture tests when the entity affects shared invariants.
5. Update migration baseline SQL when the database shape changes.

## Update Migration Baseline

1. Review the database shape change in the spec before editing migration SQL.
2. Update `database/migrations/*.sql` with explicit DDL and explicit column lists.
3. Update `database/seeds/*.sql` only for required seed data. Seeds must be idempotent.
4. Keep scripts compatible with the migration runner: standard SQL statements split by line-ending semicolons; no `DELIMITER`, stored procedures, functions, triggers, or function bodies that rely on internal semicolons.
5. Run integration tests with MySQL `127.0.0.1`, then run `bash scripts/quality-gate-backend.sh`.
6. Production must use the reviewed migration command and migration account; startup automatic DDL remains forbidden in production.

## Test And Quality Gates

Use MySQL `127.0.0.1` for local full verification:

```bash
export WECMS_TEST_MYSQL_ALLOWED_HOSTS="127.0.0.1"
export WECMS_TEST_MYSQL_CONNECTION_STRING="server=127.0.0.1;port=3306;database=wecms_dev;uid=wecms_dev;pwd=<local-dev-password>;charset=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;"
```

Backend verification:

```bash
dotnet restore backend/WeCms.slnx
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings
bash scripts/quality-gate-backend.sh
```

Frontend verification, when frontend files are in scope:

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```
