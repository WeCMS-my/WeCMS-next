# PH-0 Production Configuration Baseline Tasks

## PH-0-001: Production Configuration Inventory

- Add `docs/ops/production-configuration.md`.
- List Auth, DB, 2FA, CORS / origin, Cookie, Secure Headers, Rate Limiting, Login Failure, FileStorage, Logging, Database seed, and frontend production keys.
- Mark secrets as forbidden in git.
- Document Development, Staging, and Production behavior.

## PH-0-002: Production Configuration Example

- Add `backend/src/WeCms.Api/appsettings.Production.example.json`.
- Use non-runnable placeholders for secrets.
- Do not include real domains, IP addresses, tokens, or passwords.
- Link the template from the production configuration document.

## PH-0-003: Production Fail-Fast Validation

- Add a startup validator under `WeCms.Api`.
- Validate required production settings only when `ASPNETCORE_ENVIRONMENT=Production`.
- Add unit tests for required production settings and Development allowance.

## PH-0-004: Development Placeholder Cleanup

- Replace `pwd=replace-me` with `pwd=__SET_BY_USER_SECRETS__`.
- Update README with user-secrets and env override guidance.
- Correct the README migration / seed wording so it does not claim an automatic startup behavior that the current code does not implement.

## Gate Check

- Add `scripts/checks/check-production-config-baseline.sh`.
- Wire it into `scripts/quality-gate-backend.sh`.
