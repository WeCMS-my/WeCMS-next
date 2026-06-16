# M1-BE-015 Quality Gate and CI Spec

## Scope

Ensure M1 backend system management APIs are covered by local and CI quality gates.

## Required Checks

- `check-system-permission-coverage`
- `check-system-openapi-coverage`
- `check-no-sql-in-modules`
- `check-db-boundary`
- `check-layer-dependency`
- `check-di-boundary`
- `check-no-frontend-change`

## Rules

- CI must run `scripts/quality-gate-backend.sh`.
- Local fallback NuGet audit mode remains forbidden in CI.
- New scripts must fail fast with clear names.
- Checks must be runnable without secrets other than the MySQL integration-test connection string.
