# Tasks

Execute one task at a time. Do not start S-02 or other hardening tasks until S-01 has tests, gate evidence, and audit closure.

- [x] S-01-000 Create this spec, task list, and checklist.
- [x] S-01-001 Add failing tests for buffered rate-limit rejection behavior.
- [x] S-01-002 Add `IRateLimitHitBuffer`, `IRateLimitHitAggregator`, aggregation records, and in-memory implementation.
- [x] S-01-003 Add `RateLimitSecurityEventFlushHostedService` with flush failure circuit breaker.
- [x] S-01-004 Change `WeCmsRateLimitingExtensions.OnRejected` to record through the buffer without awaiting database writes.
- [x] S-01-005 Register the new services through DI.
- [x] S-01-006 Run targeted tests and relevant security/rate-limit checks.
- [x] S-01-FINAL Run backend gate or document an environment blocker, then audit the S-01 diff.

## Closure Requirements

- [x] Red -> Green -> Refactor evidence exists for behavior changes.
- [x] Request rejection still returns HTTP 429 when security-event flushing fails.
- [x] No DB write happens on the request rejection path.
- [x] Summary events preserve policy, method, path, user, IP, trace ID, and hit count.
- [x] No frontend files are changed.
- [x] No database migration is changed.
