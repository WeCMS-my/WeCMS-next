# M1-BE-009 Setting API Spec

## Scope

Implement backend-only system setting management APIs:

- `GET /api/v1/system/settings`
- `GET /api/v1/system/settings/{key}`
- `PUT /api/v1/system/settings/{key}`

## Contracts

Settings use the unified API result envelope. List responses use `PagedResult<T>`.

Sensitive settings must never return plaintext values. The API returns `null` for sensitive values and exposes `isSensitive = true`.

## Rules

- `key` is unique among active settings.
- Page size must be between 1 and 100.
- Setting `valueType` must be one of `string`, `number`, `boolean`, `json`.
- Updating a missing setting fails with `NotFound`.
- Updating a sensitive setting records an audit log entry.
- Setting API endpoints require JWT authorization and permission metadata.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope

- Frontend implementation.
- Creating or deleting settings through API.
- Runtime AI integration.
