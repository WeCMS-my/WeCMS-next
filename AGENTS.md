# AGENTS.md

## Project

This repository is the WeCMS rebuild project.

The project is rebuilt from zero using:
- .NET 10 Native AOT
- ASP.NET Core Minimal API
- SqlSugar
- MySQL
- Vue 3, developed only after backend API contract freeze

## Required Reading

Before making changes, read:

1. docs/architecture/wecms-rebuild-architecture.md
2. docs/plans/wecms-rebuild-task-plan.md
3. docs/codex/task-execution-rules.md
4. docs/codex/api-contract-rules.md
5. docs/codex/architecture-boundaries.md
6. docs/codex/quality-gates.md

## Hard Rules

- Backend first.
- Do not develop frontend before backend API contract freeze.
- Do not modify frontend API types manually.
- Frontend data structures must follow backend OpenAPI.
- Do not use MVC Controllers.
- Use ASP.NET Core Minimal API only.
- Keep Native AOT compatibility.
- Only WeCms.Persistence may reference SqlSugarCore.
- Modules must not reference WeCms.Persistence.
- Modules must not reference SqlSugarCore.
- Do not introduce dynamic reflection scanning.
- Do not introduce AutoMapper.
- Do not introduce Session.
- Do not keep legacy compatibility code.
- Do not implement tasks outside the current requested version.

## Done Means

A task is done only when:

- Code builds successfully.
- Relevant tests pass.
- Architecture boundaries are preserved.
- OpenAPI contract remains valid.
- Native AOT publish does not regress.
- No unrelated files are changed.


## Mandatory Engineering Rules

Before editing code, read:

- docs/architecture/wecms-rebuild-architecture.md
- docs/plans/wecms-rebuild-task-plan.md
- docs/codex/engineering-rules.md
- docs/codex/api-contract-rules.md
- docs/codex/quality-gates.md
- docs/codex/architecture-boundaries.md

Hard rules:

- Follow OOP / SOLID.
- Use interface-first design for side-effect services.
- Use constructor injection.
- Keep high cohesion and low coupling.
- Use TDD for .cs production code changes.
- Keep Minimal API endpoints thin.
- Keep Native AOT compatibility.
- Only WeCms.Persistence may reference SqlSugarCore.
- Modules must not reference WeCms.Persistence.
- Frontend development starts only after backend API contract freeze.
- Frontend data structures must follow backend OpenAPI.
- No implicit legacy compatibility.
- No silent fallback.
- No God Service / CommonService dumping ground.
- Run bash scripts/quality-gate.sh before claiming completion.