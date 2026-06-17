# M2-FE-006 Menus And Departments Spec

## Scope

Implement `/system/menus` and `/system/depts` pages:

- menu tree list/create/edit/delete/enable/disable
- department tree list/create/edit/delete/enable/disable
- parent selection with self/descendant prevention in UI
- builtin menu delete/status UI protection

## Validation

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

## Audit Rules

- No CMS route/API call.
- No backend production code change.
- Menu button type is not treated as route generation.
- Destructive actions require confirmation.
