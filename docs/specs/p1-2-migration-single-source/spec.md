# P1-2 Migration Single Source

## Background

The new WeCMS system starts from a clean database. It does not need runtime compatibility with old ThinkPHP data or legacy migration state.

The current database initialization path has two sources for the same schema and seed intent:

- `DbMigrationRunner` embeds schema and seed SQL as C# constants.
- `database/migrations/*.sql` and `database/seeds/*.sql` are executable by shell scripts.

This allows drift between runtime startup and manual database scripts. It also stores seed entries in the schema migration sequence and only checks whether a version exists, not whether the stored checksum still matches the current SQL.

## Decision

`database/migrations/*.sql` and `database/seeds/*.sql` are the only application schema and seed SQL source.

`DbMigrationRunner` must:

- read schema migrations from `database/migrations`;
- read seed scripts from `database/seeds`;
- track schema migrations in `sys_schema_migration`;
- track seed scripts separately in `sys_seed_migration`;
- compute SHA-256 checksums from the file SQL text;
- fail fast if an already-applied version has a different checksum;
- inject runtime-only values, such as the admin password hash, through Dapper parameters rather than file placeholders.

The runner may create its own metadata tables before applying files. Application schema files must not define runner metadata tables.

## Non-Goals

- No compatibility path for old `sys_schema_migration.version` values such as `001`.
- No ThinkPHP data migration.
- No direct shell execution of individual SQL files.
- No production demo seed import behavior.
