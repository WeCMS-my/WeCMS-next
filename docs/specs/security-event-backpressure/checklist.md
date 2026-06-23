# Checklist

## Scope Control

- [x] Only S-01 RateLimit rejection backpressure is implemented.
- [x] S-02 IP access control aggregation is not mixed into this task.
- [x] S-03 SecurityBan cache is not mixed into this task.
- [x] S-04 CSP enforce, S-05 virus scan enforcement, and S-06 filename hardening are not mixed into this task.
- [x] No frontend files are changed.
- [x] No database schema or migration is changed.
- [x] No AI runtime, MVC Controller, Razor, EF Core, dynamic query/return, or runtime endpoint scanning is introduced.

## Behavior

- [x] `OnRejected` uses an injected buffer and does not await repository/database writes.
- [x] Buffer failure or full capacity does not prevent HTTP 429 response writing.
- [x] Same policy + method + path + user/IP key aggregates within the configured window.
- [x] Different aggregate keys flush as separate security events.
- [x] Flush failures do not affect request handling.
- [x] Circuit breaker opens after repeated flush failures.
- [x] Circuit breaker recovers after cooldown.
- [x] Aggregated security event message includes hit count.

## Verification

- [x] Targeted unit tests pass.
- [x] Rate-limit policy coverage check passes.
- [x] Security event coverage check passes or is updated with equivalent evidence.
- [x] Backend quality gate passes or an environment blocker is documented.
- [x] S-01 final audit passes against `AGENTS.md`, `code_review.md`, and `.trae/rules/wecms-engineering-principles.md`.
