# Logging And Observability Baseline

## Production Log Fields

Every HTTP request completion log must carry these fields:

| Field | Source | Notes |
|---|---|---|
| timestamp | logging provider | UTC recommended |
| level | logging provider | `Information` for request completion |
| requestId | `HttpContext.TraceIdentifier` | same value as `traceId` |
| traceId | `X-Trace-Id` or generated trace id | max 64 safe characters |
| userId | authenticated principal | null for anonymous requests |
| username | authenticated principal | null for anonymous requests |
| method | request method | no body logging |
| path | request path | query strings are intentionally omitted |
| statusCode | response status | integer |
| elapsedMs | request duration | rounded milliseconds |
| eventType | application event name | `http_request_completed` for request logs |

## Sensitive Data Rules

Do not log:

- `Authorization`;
- `Cookie`;
- password fields;
- refresh token values;
- access token values;
- 2FA secrets or recovery codes;
- production connection strings.

Request logging records metadata only. It does not read request bodies.

## Health Endpoints

| Endpoint | Access | Checks | Sensitive detail policy |
|---|---|---|---|
| `/health/live` | anonymous | process live only | no DB checks |
| `/health/ready` | anonymous | DB, migrations, critical config loaded | generic failure messages only |
| `/health/dependencies` | authenticated plus `sys:system:secure-ping` permission | DB latency, migration status, critical config loaded | failure codes only |

`/health/dependencies` is intentionally protected. If an operator needs an unauthenticated dependency endpoint, put it behind an internal reverse proxy rule instead of making it public.

Readiness migration status checks `Database:LatestRequiredMigration` against `sys_schema_migration`. A database with only early migrations must not become ready.

## Minimum Operations Checklist

- Confirm `X-Trace-Id` appears on all API responses.
- Confirm request completion logs include `traceId`, `path`, `statusCode`, and `elapsedMs`.
- Confirm request logs do not contain Authorization or Cookie values.
- Confirm `/health/live` succeeds without a database connection.
- Confirm `/health/ready` fails when MySQL is unavailable.
- Confirm `/health/dependencies` is not public.
