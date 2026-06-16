# M1-BE-010 LoginLog API Spec

## Scope

Implement backend-only read APIs for login logs:

- `GET /api/v1/system/login-logs`
- `GET /api/v1/system/login-logs/{id}`

## Contracts

List responses use `PagedResult<LoginLogSummaryDto>`. Detail responses return `LoginLogDetailDto`.

Supported list filters:

- `username`
- `ip`
- `result`
- `from`
- `to`

## Rules

- Login log APIs are read-only.
- No create, update, or delete endpoints are exposed.
- Page size must be between 1 and 100.
- `from` must be earlier than or equal to `to` when both are present.
- Missing detail returns `NotFound`.
- Endpoints require JWT authorization and permission metadata.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope

- Mutating login logs.
- Frontend implementation.
- Runtime AI integration.
