# PH-2 Database Production Governance Checklist

- [ ] No new business module or CMS phase 2 feature.
- [ ] No real database password, host credential, dump, or production secret committed.
- [ ] `docs/ops/database-production.md` exists.
- [ ] `docs/runbooks/database-backup-restore.md` exists.
- [ ] Runtime, migration, and backup account permissions are documented.
- [ ] Production startup does not auto-run migrations by default.
- [ ] Independent migration command is documented and tested.
- [ ] Command timeout is configurable and validates invalid values.
- [ ] `scripts/checks/check-database-governance.sh` passes.
- [ ] Backend gate passes with `127.0.0.1` MySQL.
- [ ] Frontend gate passes.
