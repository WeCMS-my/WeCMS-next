# P1-002 Generated Test Artifacts Gate Spec

## Scope

Prevent transient test runner artifacts from being tracked or accepted by the backend quality gate.

This change covers:

- removing committed test runner artifacts,
- ignoring common transient test outputs,
- adding a dedicated scanner that fails when generated test artifacts are present in the tracked worktree,
- wiring that scanner into the backend quality gate.

## Requirements

- Remove tracked files under `backend/tests/**/TestResults/**` that were created by local test execution.
- Ignore these generated artifact classes:
  - `**/TestResults/`
  - `*.trx`
  - `*.coverage`
  - `*.coveragexml`
  - `vstest.diag*.log`
- Backend gate must fail if tracked files matching the generated test artifact patterns exist.
- The new scanner must not flag intentional contract artifacts such as `artifacts/openapi/wecms-api-v1.json`.
- The change must not relax any existing build, test, publish, or code-review checks.

## Non-Goals

- Reworking release evidence artifact paths.
- Changing OpenAPI artifact ownership.
- Changing test execution behavior beyond artifact hygiene.
