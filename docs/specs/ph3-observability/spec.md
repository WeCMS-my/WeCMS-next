# PH-3 Observability And Health Checks

## Scope

PH-3 adds the minimum production observability baseline after PH-2:

- structured request completion logging;
- layered health endpoints;
- critical security event alerting interface;
- operations documentation and gate coverage.

## Non-goals

- No CMS phase 2 content APIs.
- No AI runtime.
- No external monitoring, webhook, or tracing SDK.
- No database schema change.
- No production secret examples.

## Requirements

- `/health/live` must stay anonymous and must not depend on MySQL.
- `/health/ready` must check database and migration status.
- `/health/dependencies` must expose dependency status without connection strings, exception messages, or secrets, and must be protected.
- Request logging must include traceId, userId, username, method, path, statusCode, elapsedMs, and eventType.
- Request logging must not log request bodies, Authorization, Cookie, password, refresh token, or access token values.
- Security alerting must define `ISecurityAlertSink` and provide a logging sink until an external alert channel is approved.
- High or critical security events must be routed to the alert service.

## Validation

- Unit/source tests cover request logging, health endpoint intent, and alert sink behavior.
- `scripts/checks/check-observability-baseline.sh` enforces PH-3 code and documentation guardrails.
- Backend and frontend quality gates must pass. Database validation uses local `127.0.0.1` MySQL only.
