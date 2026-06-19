# Database Backup And Restore Runbook

## Backup Policy

- Frequency: daily full backup.
- Optional: binlog or incremental backup for tighter recovery point objectives.
- Retention: 7 daily, 4 weekly, 3 monthly.
- Encryption: backups must be encrypted at rest.
- Secret storage: backup credentials and encryption keys must be stored outside git.

## Release Backup

Before every production release:

1. Confirm release commit SHA and tag.
2. Run a full backup with the backup account.
3. Verify backup artifact exists and has non-zero size.
4. Store checksum and timestamp in the release record.
5. Do not proceed to migration until backup verification is recorded.

## Restore Drill

Run restore drills in staging:

1. Restore latest backup to a staging database.
2. Verify schema migration table exists.
3. Compare row-count checksums for critical tables.
4. Run API db-check.
5. Smoke test admin login.
6. Record drill date, operator, source backup, result, and issues.

## Restore Procedure

1. Stop API writes or put the site into maintenance mode.
2. Create a final emergency backup if the database is reachable.
3. Restore the selected backup to a new database.
4. Validate checksum and table counts.
5. Point staging or production configuration to the restored database only after validation.
6. Run health checks and admin login smoke test.
7. Record audit notes and incident timeline.

## Restrictions

- Do not restore over production without explicit human approval.
- Do not use the API runtime account for backup or restore.
- Do not commit dumps, backup artifacts, passwords, or encryption keys.
