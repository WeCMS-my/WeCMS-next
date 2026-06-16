# M1-BE-012 SecurityEvent API Spec

## Scope

Implement backend-only read APIs for security events:

- `GET /api/v1/system/security-events`
- `GET /api/v1/system/security-events/{id}`

## Contracts

List responses use `PagedResult<SecurityEventSummaryDto>`. Detail responses return `SecurityEventDetailDto`.

Supported list filters:

- `eventType`
- `severity`
- `user`
- `ip`
- `from`
- `to`

## Rules

- Security event APIs are read-only.
- No create, update, or delete endpoints are exposed.
- Page size must be between 1 and 100.
- `from` must be earlier than or equal to `to` when both are present.
- Missing detail returns `NotFound`.
- Endpoints require JWT authorization and permission metadata.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope

- Mutating security events.
- Frontend implementation.
- Runtime AI integration.
