# H3 Final Acceptance Spec

## Scope

H3 closes the phase-one hardening acceptance loop for the foundation system. The scope is validation and baseline freezing, not new CMS functionality.

In scope:

- Backend and frontend quality gates.
- OpenAPI contract review.
- Permission, audit log, security event, Cookie Origin / CSRF coverage reviews.
- AdminGate / CSRF migration review against the new architecture.
- ThinkPHP foundation feature delta review.
- Foundation freeze baseline for CMS phase two.

Out of scope:

- CMS content APIs, tables, permissions, menus, or frontend pages.
- AI runtime or provider integration.
- Old ThinkPHP runtime compatibility.
- Copying old AdminGate as a new all-in-one middleware.

## Requirements

- The backend quality gate must include repeatable checks for security event coverage, Cookie auth Origin protection, AdminGate / CSRF migration, ThinkPHP feature delta, and foundation freeze baseline.
- The frontend quality gate must keep typecheck, lint, build, no CMS frontend scan, no untrusted `v-html`, route permission coverage, and smoke fixture checks.
- The freeze baseline must identify OpenAPI, migrations, seeds, gate scripts, accepted H3 tasks, and CMS phase-two entry constraints.
- Failure states must be explicit and fail fast; scripts must not silently accept missing artifacts or wildcard production origins.

## Verification

Required verification:

```bash
WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend bash scripts/quality-gate-backend.sh
bash scripts/quality-gate-frontend.sh
bash scripts/checks/check-cookie-auth-origin-protection.sh
bash scripts/checks/check-admingate-csrf-migration.sh
bash scripts/checks/check-thinkphp-feature-delta.sh artifacts/openapi/wecms-api-v1.json
bash scripts/checks/check-foundation-freeze-baseline.sh
```
