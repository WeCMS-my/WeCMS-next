# System Foundation Operations

This guide is the operations index for the accepted WeCMS system-foundation modules. No CMS module and No AI runtime capability is operated by this guide. It also does not describe legacy ThinkPHP compatibility or unimplemented distributed transaction behavior.

## Module Ownership

- Identity: login, refresh, logout, account profile, users, 2FA, and auth challenges.
- AccessControl: roles, menus, permissions, access profiles, and permission version updates.
- Organization: departments, positions, and user-position assignments.
- Configuration: settings, dictionaries, and i18n messages.
- Audit: login logs and audit logs.
- Security: security events, rate-limit events, and security bans.
- FileCenter: file metadata, upload, download, preview, storage audit, and file security events.
- Platform: health, version, and database probes.
- Data.SqlSugar and Modules.*.SqlSugar: migration, seed, SqlSugar registration, QueryFilter, raw SQL guardrails, repositories, and CodeFirst metadata.
- Caching, AOP, and EventBus: cache abstraction, application-service transaction/cache/audit interception, integration-event publishing, and Outbox dispatch.

## Health Checks

Use health probes in this order during release and incident triage:

1. `/health/live` confirms the API process is running.
2. `/health/ready` confirms the app is ready for traffic.
3. `/health/dependencies` confirms protected dependency status for authorized internal operators.

If `/health/dependencies` fails, check the database connection, migration state, file storage path, and configured security settings before restarting the service.

## Database And Seed Operations

- Review migration SQL and seed SQL against `docs/ops/database-production.md` before release.
- Production startup automatic DDL remains disabled; use the reviewed migration command and migration account.
- Seeds must stay idempotent. Do not add production-only data dumps or real secrets to seed files.
- QueryFilter does not rewrite raw SQL; follow `docs/specs/s10-data-platform-upgrade/query-filter-raw-sql.md` and prefer PredicateBuilder helpers for guarded raw SQL.

## OpenAPI And Contract Checks

- Export OpenAPI as part of backend gate validation.
- Treat OpenAPI as the backend/frontend contract. Do not hand-edit generated frontend service files.
- When a route, DTO, permission, audit metadata, or rate-limit policy changes, rerun the OpenAPI and permission coverage checks before release.

## Audit And Security Events

- Write operations must record audit rows with actor, target, request method, request path, IP address, user agent, trace id, result, and detail when those fields are available.
- Application Service AOP audit uses `request_method = SERVICE`; HTTP request context is intentionally not available there unless a future approved task adds a request-context accessor.
- Security-sensitive operations must write `sys_security_event` entries. Review `docs/ops/security-alerting.md` and `docs/runbooks/incident-response.md` for triage.
- Never paste secrets, tokens, passwords, 2FA secrets, production dumps, or private file contents into audit notes or incident records.

## Outbox Operations

- Outbox dispatch is same-database consistency only. It is not a distributed transaction mechanism.
- Monitor pending, processing, failed, retry count, available time, locked time, processed time, and error fields in `sys_outbox_message`.
- Repeated failed messages should be triaged by event type, payload validity, handler idempotency, and downstream dependency status.
- Invalid payloads and handler failures are expected to be marked failed for retry; the dispatcher should not terminate the hosted service loop for one bad message.

## Cache And AOP Operations

- Cache keys are tenant-aware and include application, environment, version, module, resource, and parameter hash parts.
- Prefix eviction must stay tenant-scoped. Do not use broad prefixes that can evict another tenant or module.
- Redis remains explicit configuration work; do not assume distributed cache behavior unless production configuration enables it.
- AOP is limited to Application Service interfaces. Repositories, endpoint handlers, entities, and DTOs must not be annotated for AOP.

## Release Checklist Additions

Before go/no-go:

- Run backend and frontend gates or record concrete environment blockers.
- Confirm migration and seed review is complete.
- Confirm OpenAPI export and permission/audit/security-event coverage checks pass.
- Confirm Outbox dispatcher is configured with reviewed batch size and poll interval.
- Confirm cache provider and key version are reviewed.
- Confirm audit/security-event dashboards or query access are available for post-release observation.

## Common Incidents

- DB connection exhaustion: check `/health/dependencies`, database connection limits, slow SQL logs, and recent migration activity.
- Permission anomaly: check permission seed, role assignments, permission version updates, and audit rows.
- Audit gap: check write endpoint metadata, module repository audit inserts, and `SqlSugarAuditWriter`.
- Outbox backlog: check failed messages, handler idempotency, retry delay, and payload validity.
- Cache staleness: check cache key version, tenant id, prefix eviction, and permission-version updates.
