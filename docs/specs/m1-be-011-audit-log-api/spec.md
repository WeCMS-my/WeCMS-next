# M1-BE-011 AuditLog API Spec

## Scope

Implement backend-only read APIs for audit logs:

- `GET /api/v1/system/audit-logs`
- `GET /api/v1/system/audit-logs/{id}`

## Contracts

List responses use `PagedResult<AuditLogSummaryDto>`. Detail responses return `AuditLogDetailDto`.

Supported list filters:

- `user`
- `module`
- `resource`
- `action`
- `result`
- `from`
- `to`

## Rules

- Audit log APIs are read-only.
- No create, update, or delete endpoints are exposed.
- Page size must be between 1 and 100.
- `from` must be earlier than or equal to `to` when both are present.
- Missing detail returns `NotFound`.
- Endpoints require JWT authorization and permission metadata.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope

- Mutating audit logs.
- Frontend implementation.
- Runtime AI integration.
