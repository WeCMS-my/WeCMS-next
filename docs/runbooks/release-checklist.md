# Release Checklist

Use one copy of this checklist per release. Do not record secrets, passwords, tokens, or private backup locations in this file.

## Release Record

| Item | Value |
| --- | --- |
| Current commit SHA | |
| Current tag | |
| Release owner | |
| Release timestamp | |
| Rollback target commit/tag | |
| Migration plan link | |
| Backup artifact reference | |
| Backend gate result | |
| Frontend gate result | |
| Appsettings / env review result | |
| Secrets verification result | |
| `/health/live` result | |
| `/health/ready` result | |
| `/health/dependencies` result | |
| Smoke admin login result | |
| Final go / no-go decision | |
| Known residual risks | |

## Pre-Release Gate

- [ ] `bash scripts/quality-gate-backend.sh` passed.
- [ ] `bash scripts/quality-gate-frontend.sh` passed.
- [ ] Database backup completed using `docs/runbooks/database-backup-restore.md`.
- [ ] Migration plan reviewed, including `--migrate` command and expected migration versions.
- [ ] `appsettings` and environment variables reviewed against `docs/ops/production-configuration.md`.
- [ ] Secrets verified in the production secret store, without copying values into the release record.
- [ ] Reverse proxy, TLS, CORS, CSP, and secure headers reviewed against `docs/ops/security-baseline.md`.
- [ ] File storage base path or provider configuration reviewed against `docs/ops/file-storage-production.md`.
- [ ] Rollback target confirmed using `docs/runbooks/rollback.md`.
- [ ] Production deployment record prepared using `docs/runbooks/production-deployment-record.md`.

## Release Steps

1. Confirm the current commit SHA and tag.
2. Confirm database backup is complete and verified.
3. Apply reviewed database migrations with the migration account.
4. Deploy the application artifact.
5. Deploy or invalidate frontend static assets.
6. Verify `/health/live`.
7. Verify `/health/ready`.
8. Verify protected `/health/dependencies` from an authorized internal operator session.
9. Perform smoke admin login.
10. Confirm audit and security event writes are healthy.

## Go / No-Go

- [ ] Go: all checks passed, no P0/P1 blockers, rollback target confirmed.
- [ ] No-Go: any gate failed, backup missing, migration uncertain, health check failed, admin login failed, or rollback target missing.

## Post-Release

- [ ] Record final deployed commit SHA and tag.
- [ ] Record gate results and smoke test outcome.
- [ ] Record any residual risk with owner and follow-up date.
- [ ] Complete `docs/runbooks/production-deployment-record.md` for the release archive.
