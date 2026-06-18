# H2 System Enhancement Spec

## Goal

Complete the H2 system enhancement tasks from `docs/context/WeCMS_Next_一期后建议补齐清单详细开发修复计划书_v1.1_任务说明增强版.md` before CMS phase 2 starts.

## Scope

This spec covers only:

- H2-001 i18n database and API.
- H2-002 i18n frontend page.
- H2-003 menu batch sorting.
- H2-004 dictionary status enable and disable.
- H2-005 settings security hardening.
- H2-006 file upload policy layering.
- H2-007 SecurityEventClassifier.
- H2-008 rate limiting tiered policies.
- H2-009 PermissionVersion backend and frontend closure.
- H2-010 secure headers and CSP report-only.

## Explicit Non-Scope

- CMS content APIs, article/category/page/media/SEO/tag management.
- Runtime AI modules, providers, prompt, RAG, vector store, or model calls.
- ThinkPHP runtime compatibility, old session/token compatibility, old password hash compatibility, or old PHP i18n override generation.
- A monolithic AdminGate clone.
- MVC Controller, Razor, EF Core, runtime endpoint scanning, dynamic proxy AOP, or runtime code generation.

## Architecture Constraints

- ASP.NET Core Minimal APIs remain the only backend endpoint style.
- `WebApplication.CreateSlimBuilder(args)` and JIT publish/runtime remain the runtime baseline.
- SqlSugar remains the only ORM.
- Database and SQL access stay inside `WeCms.Persistence`.
- `WeCms.Modules.*` depends on abstractions and `WeCms.Shared`, not persistence implementations.
- New side-effect services must expose `I*` interfaces and use constructor injection.
- All DTOs used by endpoints must be registered in `WeCmsJsonSerializerContext`.
- All new endpoints must be explicitly registered and listed in the static OpenAPI registry.
- Write endpoints must have method, permission or documented authenticated internal policy, DTO validation, and audit log.
- High-risk writes must also create security events when required by the H2 task.
- Frontend generated API types must come from OpenAPI generation, not manual edits.

## Data and Contract Changes

- H2-001 may add `sys_i18n_message` with a unique constraint on `locale + message_key`.
- H2-003 adds `PUT /api/v1/system/menus/sort`.
- H2-004 adds dictionary enable and disable endpoints.
- H2-005 adds settings validation and cache reload endpoints plus setting definitions.
- H2-006 may extend file upload requests with policy information and must keep avatar uploads on Avatar policy.
- H2-007 may extend `sys_security_event` semantics for classification; schema changes must be explicit and covered by migration tests.
- H2-008 adds rate limiting policies and endpoint bindings.
- H2-009 exposes permission version to the frontend and centralizes permission version changes.
- H2-010 adds secure headers middleware with CSP report-only.

## Validation

Each subtask must close with:

- Red test evidence before production implementation, unless the change is documentation only.
- Targeted unit/integration/OpenAPI/architecture tests for the changed surface.
- Backend build, publish, and relevant gate checks.
- Frontend typecheck/lint/build and frontend gates when `frontend/**` changes.
- Code review against `AGENTS.md`, `code_review.md`, and `.trae/rules/wecms-engineering-principles.md`.

