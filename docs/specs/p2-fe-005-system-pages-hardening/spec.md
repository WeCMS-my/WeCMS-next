# P2-FE-005 System Pages Hardening

## Scope

Move system management pages from skeleton CRUD toward usable admin workflows.

## Requirements

- List loading failures must produce visible error feedback.
- Write operations must have local `try/catch` and a submitting/loading state.
- Modal forms must use Naive UI form rules where fields are required by the backend contract.
- Empty states must be explicit for primary system tables.
- Pagination must not silently cap list pages at fixed `pageSize=100` when the backend exposes paged APIs.
- A smoke-test strategy must exist before final acceptance.

## Non-Goals

- No backend contract changes.
- No generated frontend service clients.
- No CMS frontend pages.

## Validation

- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`
