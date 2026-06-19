# PH-2 Database Production Governance Tasks

- [x] Add `docs/ops/database-production.md`.
- [x] Add `docs/runbooks/database-backup-restore.md`.
- [x] Add migration execution documentation.
- [x] Add `Database:RunMigrationsOnStartup` and `Database:CommandTimeoutSeconds` to templates.
- [x] Add `--migrate` command entry.
- [x] Apply SqlSugar command timeout configuration.
- [x] Add unit/source tests.
- [x] Add `scripts/checks/check-database-governance.sh`.
- [x] Add database governance check to backend gate.
- [x] Run backend gate with `127.0.0.1` MySQL.
- [x] Run frontend gate.
- [x] Run audit and review.

Validation evidence: `scripts/checks/check-database-governance.sh`, backend gate, frontend gate, and production readiness gate.
