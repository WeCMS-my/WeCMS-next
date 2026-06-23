# Tasks

Execute one task at a time. Do not start the next implementation task until the current task has tests, gate evidence, and audit evidence.

- [x] H1-SP-000 Create专项 spec、tasks、checklist and execution plan.
- [x] H1-SP-001 Mitigate login timing side-channel with dummy password hash.
- [x] H1-SP-002 Buffer and aggregate RateLimit/IP-deny security events.
- [x] H1-SP-003 Add SecurityBan active lookup cache and invalidation.
- [x] H1-SP-004 Add AccessProfile cache keyed by user id and permission version.
- [x] H1-SP-005 Replace whole-file image validation with streaming head/tail validation and tighten filename validation.
- [x] H1-SP-006 Enforce production CSP and virus-scan production baseline.
- [x] H1-SP-007 Reject combined `[Cacheable]` and `[CacheEvict]` metadata.
- [x] H1-SP-008 Use `FileMode.CreateNew` in local file storage.
- [x] H1-SP-009 Fix response-started secondary exception behavior in exception/rejection middleware.
- [x] H1-SP-010 Harden MemoryCache prefix invalidation and Outbox dispatcher backoff/observability.
- [x] H1-SP-FINAL Run final code audit across every audit item and record stale/deferred items with evidence.

## Per-Task Closure

- [x] Red test observed for the current behavior gap, or N/A documented for docs-only work.
- [x] Minimal implementation completed for only the current task.
- [x] Targeted tests pass.
- [x] Backend gate passes, or environment blocker is separated from code result and equivalent checks are run.
- [x] Current-task audit passes.
- [x] Modified files and residual risks are summarized before moving on.
