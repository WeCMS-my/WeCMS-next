# M0-BE-011 Quality Gate Spec

## Scope

Upgrade `scripts/quality-gate-backend.sh` into the backend-only M0-BE gate.

## Required Steps

The gate must run:

1. `dotnet restore`
2. `dotnet build -warnaserror`
3. unit, architecture, and integration tests
4. JIT publish with `--self-contained false`
5. OpenAPI export
6. OpenAPI auth request body check
7. DB boundary check
8. layer dependency check
9. DI boundary check
10. no frontend change check
11. code-review rule check
12. migration/seed smoke test

## Exclusions

The gate must not run AOT publish, `/p:PublishAot=true`, Dapper baseline checks, Dapper.AOT checks, or IL trimming/AOT warning checks.

## Environment

The gate requires:

- `rg` from ripgrep for shell scanner checks.
- `WECMS_TEST_MYSQL_CONNECTION_STRING` for MySQL integration and migration/seed smoke tests.
