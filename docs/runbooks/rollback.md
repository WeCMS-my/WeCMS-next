# Rollback Runbook

Rollback is a human-approved operational action. Do not run destructive database or file operations without explicit release owner approval.

## Preconditions

- Release owner is identified.
- Rollback target commit/tag is known.
- Current production commit/tag is recorded.
- Latest backup artifact is known and verified.
- Incident or release failure timeline is started.

## Application Rollback

1. Stop new deployment rollout.
2. Re-deploy the previous known-good backend artifact.
3. Re-deploy the previous known-good frontend artifact.
4. Restore previous runtime environment variables if they changed.
5. Verify `/health/live`, `/health/ready`, and admin login.

## Database Rollback

Database rollback is not automatic. Prefer forward fixes for compatible schema changes.

- Do not restore over production without explicit human approval.
- Do not run reverse migrations unless they were reviewed before release.
- If restore is required, follow `docs/runbooks/database-backup-restore.md`.
- If migration failed before application deploy, stop deploy, keep old app running, and investigate migration state.
- If migration failed after partial writes, freeze writes if possible and decide between forward fix and restore.

## Configuration Rollback

1. Restore previous environment variables and secret references.
2. Confirm no secret value is copied into git, chat, issue comments, or release notes.
3. Restart the application if configuration is process-loaded.
4. Verify CORS, cookie, CSP, forwarded headers, and health behavior.

## File Storage Rollback

1. Confirm whether upload writes occurred after release.
2. Do not delete files blindly; compare database metadata and object keys.
3. If local storage path changed, restore previous `FileStorage:Local:BasePath`.
4. If storage data restore is required, coordinate with database restore to avoid metadata/object mismatch.

## DNS / Proxy Rollback

1. Restore previous upstream target or load balancer version.
2. Restore previous TLS, HSTS, forwarded headers, and CORS proxy rules if changed.
3. Lower TTL changes only according to the release owner decision.
4. Verify external and internal health checks.

## Verification

- [ ] `/health/live` passed.
- [ ] `/health/ready` passed.
- [ ] Authorized `/health/dependencies` passed or known degraded dependency recorded.
- [ ] Smoke admin login passed.
- [ ] Audit log records rollback action.
- [ ] Security event stream is still writable.

## Rollback Prohibited Without Approval

- Restoring a database backup over production.
- Deleting uploaded files or storage directories.
- Rotating secrets.
- Changing DNS ownership or TLS certificates.
- Running unreviewed reverse migrations.
