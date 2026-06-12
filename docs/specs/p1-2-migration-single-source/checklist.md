# Checklist

- [x] Schema SQL has one source of truth under `database/migrations`.
- [x] Seed SQL has one source of truth under `database/seeds`.
- [x] `DbMigrationRunner` fails fast on checksum drift.
- [x] Seeds are not recorded in `sys_schema_migration`.
- [x] Runtime password hash values are not stored as SQL placeholders.
- [x] Manual DB scripts do not bypass `DbMigrationRunner`.
- [x] No old-system data migration compatibility path is introduced.
