# M2-FE-008 Settings And Logs

## Scope

Implement the M2-FE system settings and log-reading frontend pages against the accepted M1-BE OpenAPI contract.

## Contract

- `GET /api/v1/system/settings`
- `GET /api/v1/system/settings/{key}`
- `PUT /api/v1/system/settings/{key}`
- `GET /api/v1/system/login-logs`
- `GET /api/v1/system/login-logs/{id:long}`
- `GET /api/v1/system/audit-logs`
- `GET /api/v1/system/audit-logs/{id:long}`
- `GET /api/v1/system/security-events`
- `GET /api/v1/system/security-events/{id:long}`

## Requirements

- Settings list supports keyword and group filters.
- Sensitive setting values are masked in list/detail display.
- Sensitive setting edits do not prefill the existing secret value.
- Settings update is gated by `sys:setting:update`.
- Login logs, audit logs, and security events are read-only pages.
- Log detail operations are gated by each detail permission.
- All pages are registered with explicit route permission metadata.
- Dynamic menu component mapping remains a static whitelist.

## Non-Goals

- No backend changes.
- No CMS frontend pages.
- No AI runtime capability.
- No OpenAPI generation replacement; M2-FE-010 owns generated contract verification.
