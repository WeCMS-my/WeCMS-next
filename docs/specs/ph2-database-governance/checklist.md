# PH-2 Database Production Governance Checklist

- [x] No new business module or CMS phase 2 feature.
- [x] No real database password, host credential, dump, or production secret committed.
- [x] `docs/ops/database-production.md` exists.
- [x] `docs/runbooks/database-backup-restore.md` exists.
- [x] Runtime, migration, and backup account permissions are documented.
- [x] Production startup does not auto-run migrations on startup by default.
- [x] Independent migration command is documented and tested.
- [x] Command timeout is configurable and validates invalid values.
- [x] `scripts/checks/check-database-governance.sh` passes.
- [x] Backend gate passes with `127.0.0.1` MySQL.
- [x] Frontend gate passes.

Validation evidence: `scripts/checks/check-database-governance.sh`, backend gate, frontend gate, and production readiness gate.
