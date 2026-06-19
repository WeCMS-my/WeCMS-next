# Incident Response Runbook

Use this runbook to structure response. Record timeline, owner, scope, customer/user impact, and final decision. Do not paste secrets, tokens, passwords, production dumps, or private file contents into incident notes.

## Login Brute Force

- Triage: review login failure logs, `sys_security_event`, source IPs, affected usernames, and rate limit counters.
- Containment: keep login rate limits enabled, block abusive IPs at proxy if needed, and avoid disabling 2FA.
- Recovery: confirm normal login path, unban only verified false positives, and verify audit writes.
- Postmortem: tune `Security:LoginFailure` and `AuthLogin` rate limits if needed.

## Refresh Token Reuse

- Triage: identify `refresh token reuse` security events, user id, session lineage, IP, and user agent.
- Containment: revoke affected user's refresh tokens and require re-login.
- Recovery: confirm `/auth/refresh` and `/auth/me` behavior, then notify affected operator if required.
- Postmortem: review cookie settings, HTTPS, proxy headers, and suspicious access patterns.

## Permission Anomaly

- Triage: compare denied permission code, role assignments, visible menus, and backend permission metadata.
- Containment: avoid granting broad roles; temporarily disable affected account or role only when necessary.
- Recovery: fix role/permission assignment or deploy a reviewed permission seed correction.
- Postmortem: add or update permission metadata tests if drift caused the incident.

## DB Connection Exhaustion

- Triage: inspect `/health/ready`, DB latency, MySQL process list, app logs, and connection pool settings.
- Containment: reduce traffic if needed, stop runaway jobs, and avoid increasing pool limits without diagnosis.
- Recovery: restart app only after identifying whether leaked connections or DB saturation caused exhaustion.
- Postmortem: review `Database:CommandTimeoutSeconds`, pool parameters, slow queries, and health trends.

## File Upload Anomaly

- Triage: review `file_upload_rejected` events, MIME/extension mismatches, size rejections, and storage path health.
- Containment: keep `FileUpload` rate limit enabled and block abusive sources at proxy if needed.
- Recovery: verify file storage writability, scan policy, download/preview behavior, and audit writes.
- Postmortem: update `docs/ops/file-storage-production.md` and upload policy tests if required.

## Disk Space Low

- Triage: identify filesystem, file storage path, logs, temporary directories, and database disk usage.
- Containment: stop nonessential writes and uploads if free space is critically low.
- Recovery: rotate or archive logs, expand disk, move storage only with reviewed config and backup plan.
- Postmortem: add disk alerts and storage retention decisions.

## Migration Failure

- Triage: record migration version, failing SQL, database state, and whether app deploy has started.
- Containment: stop deploy and prevent app version mismatch.
- Recovery: use the reviewed migration plan; prefer forward repair unless restore is approved.
- Postmortem: improve migration dry-run, checksum, and backup verification.

## Frontend Contract Mismatch

- Triage: compare `artifacts/openapi/wecms-api-v1.json`, generated frontend types, browser error, and backend commit.
- Containment: roll back frontend or backend to a matching release pair.
- Recovery: regenerate types from committed OpenAPI and rerun frontend gate before redeploy.
- Postmortem: strengthen generated artifact and route permission gate checks.

## API 5xx Spike

- Triage: inspect structured logs by traceId, endpoint, status, elapsedMs, deployment timestamp, and dependency health.
- Containment: rollback app if spike correlates with release and health checks fail.
- Recovery: deploy reviewed fix or restore previous known-good artifact.
- Postmortem: add regression tests and alert thresholds for the failing path.

## Common Evidence

- Security events: query `sys_security_event` by `event_type`, `severity`, `created_at`, `ip`, `trace_id`.
- Audit logs: query operation audit tables by actor, action, target id, and trace id.
- Health: use `/health/live`, `/health/ready`, and protected `/health/dependencies`.
- Release context: attach the completed `docs/runbooks/release-checklist.md` copy to incident records.
