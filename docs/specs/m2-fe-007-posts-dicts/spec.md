# M2-FE-007 Posts And Dicts Spec

## Scope

Implement `/system/posts` and `/system/dicts` pages:

- post list/create/edit/delete/enable/disable
- dict type list/create/edit/delete
- dict value list/create/edit/delete for selected type
- default/status display

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
- Destructive actions require confirmation.
- System dict type deletion is disabled in UI.
