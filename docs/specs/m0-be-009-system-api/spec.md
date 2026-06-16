# M0-BE-009 System API Spec

## Scope

Implement backend-only System API endpoints:

- `GET /health/live`
- `GET /health/ready`
- `GET /api/v1/system/ping`
- `GET /api/v1/system/version`
- `GET /api/v1/system/db-check`
- Existing `GET /api/v1/system/secure-ping` remains protected by permission metadata from M0-BE-008.

## Contract

All responses use `ApiResult<T>`.

`/health/live` must not depend on database state.

`/health/ready` and `/api/v1/system/db-check` must check database connectivity through a System module abstraction implemented in `WeCms.Persistence`.

Database check failures must return a stable generic message and must not expose `Exception.Message`.

## Architecture

- System module owns endpoint registration, DTOs, and DB-probe abstraction.
- Persistence owns SqlSugar implementation for the DB probe.
- API host only composes DI and maps explicit endpoints.
- No MVC controller, endpoint scanning, dynamic, EF Core, or AI runtime code.

## Status Semantics

- Successful probes return HTTP 200 with `code: 0`.
- Failed DB readiness/probe returns HTTP 503 with `code: 50300` and a generic message.
