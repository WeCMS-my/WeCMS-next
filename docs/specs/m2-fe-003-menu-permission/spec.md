# M2-FE-003 Menu And Permission Spec

## Scope

Implement the frontend permission and navigation loop:

- permission helper functions
- `PermissionButton`
- menu API shell
- dynamic menu rendering
- dynamic route component whitelist
- guarded dynamic route registration

## Non-Goals

- No system CRUD page implementations.
- No role/permission/menu management forms.
- No backend contract changes.
- No CMS routes or `/api/v1/cms` calls.
- No AI runtime.

## Contract Notes

- `/auth/me` returns `AuthMenuDto`, but this DTO is too small for full route generation.
- `/api/v1/system/menus/tree` returns `MenuTreeDto`, which has route component, icon, hidden, keepAlive, externalUrl, permissionCode, status, and children.
- `/api/v1/system/menus/tree` requires `sys:menu:tree`; fallback calls must be permission-gated.

## Required Behavior

- Expose `hasPermission`, `hasAnyPermission`, and `hasAllPermissions`.
- `PermissionButton` renders only when permissions pass.
- App shell renders visible menu links from authorized static/dynamic routes.
- Dynamic route generation must use a component whitelist.
- Unknown backend `component` values must be skipped and warned.
- Hidden menus must not display in navigation.
- Disabled menus must not register routes.
- Button-type menus must not register routes.
- Route permission metadata must be set from `permissionCode`.
- Frontend permission checks remain UI-only.

## Validation

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

## Audit Rules

- No arbitrary dynamic import from backend component strings.
- No CMS route/API call.
- No backend production code change.
- Request client still preserves backend `data` shape.
- All `/system` routes have permission metadata.
