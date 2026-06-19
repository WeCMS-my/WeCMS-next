# PH-5 Release, Rollback, And Runbooks

## Scope

PH-5 defines repeatable release, rollback, and incident response procedures for WeCMS Next production hardening.

## Requirements

- Release checklist must be directly copyable for each production release.
- Rollback runbook must cover application, database, configuration, file storage, DNS/proxy, health checks, admin login, and audit notes.
- Incident runbook must cover common security, database, upload, migration, frontend contract, and API 5xx incidents.
- Release checklist must reference backup/restore and rollback runbooks.
- A gate script must verify runbook presence and required operational fields.

## Non-Goals

- No CI/CD provider integration.
- No automatic production deployment.
- No destructive rollback automation.
- No production credentials or environment-specific hostnames.
