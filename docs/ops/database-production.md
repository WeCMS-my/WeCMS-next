# Database Production Governance

## Accounts

Use separate MySQL accounts in production.

| Account | Used by | Minimum privileges |
| --- | --- | --- |
| `wecms_app` | API runtime | `SELECT`, `INSERT`, `UPDATE`, `DELETE` on the WeCMS schema |
| `wecms_migration` | release migration command | DDL/DML required for migrations and seeds on the WeCMS schema |
| `wecms_backup` | backup job | backup-only read privileges required by the backup tool |

The runtime account must not have `DROP DATABASE`, global admin privileges, user management privileges, or backup credentials. The backup account must not be used by the API.

## Connection Strings

- `ConnectionStrings:Default`: runtime API connection string.
- `ConnectionStrings:Migration`: recommended production migration connection string. If absent, the migration command uses `Default`.

Production connection strings must be injected by environment variables or a secret manager.

Recommended MySQL connection-string parameters:

```text
Pooling=true;MinimumPoolSize=0;MaximumPoolSize=100;ConnectionTimeout=15;DefaultCommandTimeout=30;SslMode=Required;
```

Adjust pool size for the deployment's CPU, MySQL capacity, and expected concurrency.

## Command Timeout

`Database:CommandTimeoutSeconds` configures SqlSugar command timeout.

- Development default: `30`
- Staging default: `30`
- Production default: `30`
- Valid range: `1` to `300`

Invalid values fail fast during persistence registration.

`Database:LatestRequiredMigration` must match the latest reviewed migration version in `database/migrations`. Readiness checks require that version to exist in `sys_schema_migration`.

## Migration Strategy

Production startup must not run migrations automatically.

Configuration:

- `Database:RunMigrationsOnStartup=false` in Production.
- Development defaults to `true` for local convenience when a valid local database connection is configured.

Release flow:

1. Confirm current commit SHA and release tag.
2. Take mandatory database backup.
3. Review migration SQL and checksum drift risk.
4. Run migration command with the migration account.
5. Deploy the app with the runtime account.
6. Verify health checks and admin login.

Command:

```bash
dotnet run --project backend/src/WeCms.Api -- --migrate
```

The command runs `database/migrations` and `database/seeds`. Outside Development, `Database:SeedAdminPassword` must be configured and strong.

## CodeFirst And Baseline Updates

CodeFirst metadata is allowed only in `WeCms.Data.SqlSugar` and `WeCms.Modules.*.SqlSugar`. It is a modeling and validation path, not a production automatic-DDL policy.

When a database shape changes:

1. Create or update the required `docs/specs/<change-id>/` spec trio before changing schema files.
2. Update the owning module SqlSugar entity and CodeFirst model provider.
3. Update reviewed SQL under `database/migrations`.
4. Update idempotent seed SQL under `database/seeds` only when required.
5. Run MySQL integration tests against `127.0.0.1`.
6. Run `bash scripts/quality-gate-backend.sh`.

Migration scripts must remain compatible with the repository migration runner: standard SQL statements split by line-ending semicolons, with no `DELIMITER`, stored procedures, functions, triggers, or function bodies that rely on internal semicolons.

## Pending Migrations

PH-3 readiness health reports migration dependency unavailable when `Database:LatestRequiredMigration` is missing from `sys_schema_migration`. This prevents a database with only the migration table or only early migrations from reporting ready.
