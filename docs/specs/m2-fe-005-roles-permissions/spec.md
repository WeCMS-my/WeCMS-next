# M2-FE-005 Roles And Permissions Spec

## Scope

Implement `/system/roles` and `/system/permissions` pages:

- role list/create/edit/delete/enable/disable
- assign role permissions and menus
- permission list/tree/create/edit/delete/enable/disable
- locked role UI protection
- builtin permission UI protection
- role-bound delete confirmation

## Non-Goals

- No menu management page editing.
- No user page changes except shared clients/types if needed.
- No backend changes.
- No CMS or AI runtime.

## Validation

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
bash scripts/quality-gate-frontend.sh
git diff --check
```

## Audit Rules

- Locked roles must disable edit/delete/status/assign actions in UI.
- Builtin permissions must disable delete/status actions in UI.
- Deleting role-bound permissions requires confirmation.
- All action buttons are permission-gated.
- No backend production code changes.
