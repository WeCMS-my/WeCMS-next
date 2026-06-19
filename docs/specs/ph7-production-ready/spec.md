# PH-7 Production Ready Gate

## Scope

PH-7 collects PH-0 through PH-6 hardening outputs into one production readiness gate and final acceptance report.

## Requirements

- Add `scripts/quality-gate-production.sh`.
- Add checks for production configuration docs and production templates without secrets.
- Reuse backend and frontend quality gates.
- Verify release runbooks and frontend production env checks.
- Add final production hardening acceptance report.

## Non-Goals

- No production deployment.
- No tag creation.
- No CMS phase-two feature work.
