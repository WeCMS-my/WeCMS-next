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

## Pending Migrations

Current PH-2 scope provides an independent migration entry and documents startup policy. PH-3 readiness health will own dependency health semantics for pending migrations.
