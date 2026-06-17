# M2-FE-004 Users Page Spec

## Scope

Implement `/system/users` frontend page:

- paged user list
- keyword/status/dept filters
- create user
- edit user
- delete user
- enable/disable user
- reset password
- assign roles
- assign posts
- detail loading for edit/assign flows

## Non-Goals

- No role, post, or department management pages.
- No backend changes.
- No CMS route/API call.
- No AI runtime.

## Contract Notes

- `UserSummaryDto` exposes `deptId`, not department name.
- Department display must be mapped from `/api/v1/system/depts/tree`.
- Role and post selection must use `/api/v1/system/roles` and `/api/v1/system/posts`.
- Password hash must never be displayed.

## Required Permission Codes

- `sys:user:list`
- `sys:user:detail`
- `sys:user:create`
- `sys:user:update`
- `sys:user:delete`
- `sys:user:enable`
- `sys:user:disable`
- `sys:user:reset-password`
- `sys:user:assign-role`
- `sys:user:assign-post`

## Validation

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

## Audit Rules

- No password hash field in UI or types.
- Dangerous actions use confirmation.
- Writes show success/failure feedback.
- Buttons are wrapped by permission checks.
- `/system/users` route has permission metadata.
- No backend production code changes.
