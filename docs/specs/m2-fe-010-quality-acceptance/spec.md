# M2-FE-010 Quality Gate And Acceptance

## Scope

Close M2-FE with a strengthened frontend quality gate, acceptance report, and final audit over the M2-FE implementation scope.

## Requirements

- Frontend gate runs install, lint, typecheck, build, no-CMS scan, API contract check, and route permission coverage.
- API contract check verifies M2-FE OpenAPI schemas have frontend type declarations.
- Route permission coverage checks each `/system/*` route block, not only aggregate counts.
- Acceptance report maps the M2-FE plan acceptance criteria to implementation evidence.
- Final audit confirms no CMS route/API, no AI runtime, no generated frontend service hand edits outside the current placeholder strategy, and all touched files stay within line limits.

## Non-Goals

- No new business feature.
- No backend code changes.
- No frontend E2E smoke because no running backend fixture exists in this task.
