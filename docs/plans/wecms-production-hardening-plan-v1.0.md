# WeCMS Next Production Hardening Plan v1.0

Document version: v1.0
Generated: 2026-06-19
Project: WeCMS Next
Stage: after Phase 1 hardening PASS

## Baseline

Phase 1 hardening is accepted as the engineering stable point:

- Phase 1 hardening: PASS
- Backend quality gate: PASS
- Frontend quality gate: PASS
- Known P0/P1/P2/P3: 0
- Final review result: APPROVE

This is not the same as production-ready. Production Hardening turns the stable baseline into a deployable, observable, recoverable, auditable, and secure operating baseline.

## Non-Goals

- No CMS Articles, Channels, Pages, Media, Tags, or Links.
- No AI runtime, Agent, Prompt, Vector, or RAG.
- No old ThinkPHP runtime compatibility.
- No old system data migration.
- No multi-tenant system.
- No complex workflow.
- No new business module.
- No large refactor of accepted Phase 1 modules.
- No architecture boundary change outside the accepted Phase 1 baseline.
- No bypass of backend or frontend quality gates.

## Execution Rules

- Execute one PH stage at a time.
- Each PH stage should use an independent branch and PR.
- Every stage must have tests, gate checks, or documentation checks.
- Each completed stage must run:
  - `bash scripts/quality-gate-backend.sh`
  - `bash scripts/quality-gate-frontend.sh`
- If the plan and code disagree, stop scope expansion, make only the minimal necessary correction, and record the deviation.

## Stage Overview

| Stage | Name | Priority | Goal |
| --- | --- | --- | --- |
| PH-0 | Production configuration baseline | P0 | Required production config, secrets, env vars, fail-fast |
| PH-1 | Security baseline | P0 | HTTPS, Cookie, CORS, CSP, secure headers, rate limits |
| PH-2 | Database production governance | P0 | Least privilege, pooling, migration, backup, restore |
| PH-3 | Logging, monitoring, health | P1 | Structured logs, health checks, audit, alerting |
| PH-4 | File storage productionization | P1 | Local boundary, object storage abstraction, upload safety |
| PH-5 | Release, rollback, runbooks | P1 | Release checklist, rollback, incident response |
| PH-6 | Frontend production hardening | P1 | Build env, API base, permission routes, error handling |
| PH-7 | Production readiness gate | P0 | Single executable production-ready check |

## PH-0: Production Configuration Baseline

Tasks:

- PH-0-001: Add `docs/ops/production-configuration.md`.
- PH-0-002: Add `backend/src/WeCms.Api/appsettings.Production.example.json`.
- PH-0-003: Add production required configuration fail-fast checks.
- PH-0-004: Replace Development `replace-me` password placeholder with `__SET_BY_USER_SECRETS__`.

Required production keys:

- `ConnectionStrings:Default`
- `Auth:AccessTokenSecret`
- `Security:TwoFactor:SecretProtectionKey`
- `Security:AllowedOrigins`
- `Database:SeedAdminPassword`

Forbidden in repository:

- Production database connection strings
- JWT secrets
- 2FA protection keys
- Seed admin password
- Storage access keys
- SMTP, webhook, or object storage secrets

## PH-1: Security Baseline

Tasks:

- PH-1-001: HTTPS, HSTS, reverse proxy strategy.
- PH-1-002: Refresh token cookie production strategy.
- PH-1-003: Production CORS whitelist.
- PH-1-004: CSP report-only to enforce rollout.
- PH-1-005: RateLimit production parameters and security event linkage.

## PH-2: Database Production Governance

Tasks:

- PH-2-001: Production database least-privilege accounts.
- PH-2-002: Migration production execution strategy.
- PH-2-003: Backup and restore runbook.
- PH-2-004: Database connection pool and timeout configuration.

## PH-3: Logging, Monitoring, And Health Checks

Tasks:

- PH-3-001: Structured logging baseline.
- PH-3-002: Layered health checks: live, ready, dependencies.
- PH-3-003: Security event alerting strategy.

## PH-4: File Storage Productionization

Tasks:

- PH-4-001: FileStorage configuration baseline.
- PH-4-002: Object storage adapter boundary.
- PH-4-003: Upload security enhancement and optional scan abstraction.

## PH-5: Release, Rollback, And Runbooks

Tasks:

- PH-5-001: Release checklist.
- PH-5-002: Rollback runbook.
- PH-5-003: Incident response runbook.

## PH-6: Frontend Production Hardening

Tasks:

- PH-6-001: Frontend production env baseline.
- PH-6-002: Frontend error handling and expired session experience.

## PH-7: Production Ready Gate

Tasks:

- PH-7-001: Add `scripts/quality-gate-production.sh`.
- PH-7-002: Add `docs/reports/wecms-production-hardening-final-acceptance.md`.

## Completion Definition

Production Hardening is complete only when:

- PH-0 through PH-7 are complete.
- Backend quality gate passes.
- Frontend quality gate passes.
- Production readiness gate passes.
- Configuration, security, database, backup, release, and rollback documents exist.
- No P0/P1/P2 residual risk remains.
- Any P3 residual risk has an owner and next-stage plan.
- Final acceptance report exists.
