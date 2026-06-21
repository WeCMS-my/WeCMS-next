# S14 Final Cleanup And Acceptance Spec

## Background

Sprint 14 follows the completed Sprint 13 OpenAPI diagnostics upgrade. It is the final cleanup, documentation, full verification, and acceptance-report sprint for the system foundation destructive upgrade.

Primary source documents:

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `AGENTS.md`
- `code_review.md`
- `.trae/rules/wecms-engineering-principles.md`

## Goals

- Prove there are no forbidden naming or dependency residuals after the modular migration.
- Run full backend build, tests, publish, OpenAPI export, migration/seed smoke, and quality gate with MySQL.
- Update project governance and operations documentation to match the final module structure.
- Produce the final system foundation upgrade acceptance report.
- Confirm the repository is ready for the next CMS content-module design phase.

## Non-Goals

- Do not add CMS content APIs, CMS tables, CMS menus, or CMS frontend pages.
- Do not add frontend behavior except documentation references required for final validation.
- Do not introduce Controller, `ControllerBase`, `AddControllers`, `MapControllers`, Razor, EF Core, dynamic query/return types, silent legacy fallback, or AI runtime capability.
- Do not implement new business features while doing final cleanup.
- Do not change public API contracts unless a cleanup audit finds a direct residual from earlier system-foundation migration tasks and the required tests/gates are updated.

## Boundary Decisions

- S14 is primarily verification and documentation. Code changes are allowed only when S14-T01 finds forbidden residuals or when a gate failure proves an already-scoped migration defect.
- `WeCms.Modules.Cms` remains inactive for system foundation gate coverage.
- The final report must distinguish implemented, verified, environment-limited, and deferred CMS work.
- MySQL validation must use `127.0.0.1` in this environment.

## Functional Requirements

- Repository naming cleanup scans must cover backend source, backend tests, database migrations/seeds, and final documentation notes where appropriate.
- Full backend test commands must cover restore, warn-as-error build, unit tests, architecture tests, integration tests, publish, OpenAPI export, quality-gate scripts, and migration/seed smoke.
- Documentation must describe the final module structure, Minimal API-only decision, endpoint addition flow, permission addition flow, repository addition flow, CodeFirst entity addition flow, migration baseline flow, and quality gate commands.
- Acceptance report must include completed sprint list, module split results, old module removal results, database baseline results, test/gate results, remaining risks, and CMS next-step entry.

## Acceptance Criteria

- S14 spec trio exists before S14 production code changes.
- Forbidden naming cleanup passes or every finding is remediated before moving on.
- Full build passes.
- Full unit tests pass.
- Full architecture tests pass.
- Integration tests pass with MySQL `127.0.0.1`.
- Full backend quality gate passes with MySQL `127.0.0.1`.
- Documentation updates are complete and internally consistent.
- Final acceptance report exists and reflects actual verification evidence.
- Final Sprint 14 audit passes with no Controller/MVC/Razor/EF/dynamic/AI/CMS scope drift.
