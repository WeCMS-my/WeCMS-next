# P3 Foundation Hardening Spec

## Scope

P3 is a short-term stability backlog for existing system-foundation surfaces. It is intentionally separate from mainline feature development.

The backlog covers:

- Complex raw SQL guard behavior around `RawSqlFilterGuard`.
- PredicateBuilder-first guardrails for new raw SQL touching guarded tables.
- Audit context consistency across endpoint, AOP, and module-specific audit writers.
- Boundary tests for AOP, cache, and Outbox behavior.
- A system-foundation operations guide.

## Non-Goals

- No CMS module or CMS runtime capability.
- No AI runtime, provider, prompt, RAG, vector store, or agent tooling.
- No legacy ThinkPHP runtime compatibility or old data migration.
- No frontend generated type changes.
- No controller, Razor, EF Core, runtime endpoint scanning, or distributed transaction work.

## Requirements

- Each P3 item must be completed serially with targeted tests before moving to the next item.
- Backend code changes must run targeted tests and `bash scripts/quality-gate-backend.sh`, or record a concrete environment blocker.
- Documentation-only changes may use the rule-document exception, but only when no production, test, script, or generated file changes are included.
- New raw SQL touching soft-delete, tenant, or data-scope tables must prefer the existing predicate builders or explicitly document an exception.
- Audit rows for system writes must either include actor, request context, trace, and target fields or document a deliberate design-level N/A.
- Application Service AOP audit uses `request_method = SERVICE`; HTTP request context is N/A unless a future approved task adds a request-context accessor abstraction.
- The operations guide must describe implemented behavior only.
