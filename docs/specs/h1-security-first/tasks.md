# Tasks

Execute one task at a time. Do not start the next task until the current task has targeted tests, quality gate evidence, and an audit result.

- [x] H1-000 Create this H1 spec, task list, and checklist before production changes.
- [x] H1-001 Add Cookie auth Origin / CSRF protection for refresh/logout and future 2FA cookie-auth endpoints.
- [x] H1-002 Add `IIpRuleMatcher`, `IpRuleMatcher`, and `IpAccessControlMiddleware`.
- [x] H1-003 Add `sys_security_ban`, `ISecurityBanRepository`, `SecurityBanService`, and `SecurityBanMiddleware`.
- [x] H1-004 Add security center status, bans list/detail, unban, and batch unban backend and frontend.
- [x] H1-005 Add login failure policy/counter and SecurityBan linkage.
- [x] H1-006 Add write endpoint method / permission / audit gate and wire it into backend quality gate.
- [x] H1-007 Add 2FA database model and backend foundation services.
- [x] H1-008 Add 2FA login challenge flow and auth verify/recovery-code endpoints.
- [x] H1-009 Add account 2FA status/setup/confirm/disable/recovery-code regeneration endpoints.
- [x] H1-010 Add 2FA login and account security frontend pages.
- [x] H1-011 Add admin reset 2FA backend endpoint, permission seed, OpenAPI, generated types, and user management action.
- [x] H1-012 Add account profile, password change, avatar upload, account security backend and frontend.
- [x] H1-FINAL Run final backend gate, frontend gate, OpenAPI/generated checks, and full H1 audit.

## Per-Task Closure Requirements

- [x] Failing test or gate added before production implementation.
- [x] Minimal implementation added only for the current task.
- [x] Targeted tests pass.
- [x] Relevant quality gate passes, or an environment blocker is documented separately from code status.
- [x] Current task audit passes against `AGENTS.md`, `code_review.md`, and `.trae/rules/wecms-engineering-principles.md`.
- [x] Modified files and residual risks are summarized before the next task begins.
