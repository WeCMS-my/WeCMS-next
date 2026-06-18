# H2 System Enhancement Tasks

## Execution Rules

- Execute exactly one H2 subtask at a time.
- Do not start the next subtask until tests, gates, and review for the current subtask are complete.
- Use sub agents for read-only investigation or review support only; the main agent owns final code, verification, and audit conclusions.
- Keep changes scoped to H2 and avoid unrelated refactors.

## Task List

### H2-001 i18n Database and API

- Add `sys_i18n_message` persistence if absent.
- Add module abstractions, service, repository implementation, Minimal API endpoints, permissions, seed coverage, JSON source generation, OpenAPI registry, and tests.
- Expose admin CRUD endpoints and public locale pull endpoint.
- Verify duplicate `locale + message_key`, status filtering, permission coverage, and audit log.

### H2-002 i18n Frontend Page

- Add `/system/i18n` route and page.
- Add API client based on generated contract.
- Implement locale/module/keyword filtering, create, edit, delete, status display, permission buttons, validation, and backend error display.
- Verify typecheck, lint, build, route permission, generated contract, and smoke fixtures.

### H2-003 Menu Batch Sorting

- Add `PUT /api/v1/system/menus/sort` with `sys:menu:sort`.
- Validate ids, parent existence, no cycles, no locked menu move, max batch size, and transaction semantics.
- Add frontend save flow for batch sort.
- Ensure menu or permission version refresh strategy is covered.

### H2-004 Dictionary Status Enable and Disable

- Add independent enable and disable endpoints for dictionary types and values.
- Add permissions, audit, optional cascade-disable behavior for type disable, and public/default query filtering for disabled values.
- Add frontend switch operations with confirmation.

### H2-005 Settings Security Hardening

- Add setting definitions, readonly enforcement, sensitive encryption, masking, cache refresh, IP rule validation, and security events for sensitive changes.
- Reuse `IIpRuleMatcher` for IP/CIDR/IPv6 validation.
- Avoid silent fallback for undefined keys.

### H2-006 File Upload Policy Layering

- Add upload policy abstractions and Avatar/Image/Document policies.
- Keep avatar uploads restricted to Avatar policy.
- Enforce extension and MIME checks, size limits, random object keys, preview safety, auth on download/preview, `nosniff`, and audit.
- Avoid returning physical paths.

### H2-007 SecurityEventClassifier

- Add classifier abstractions and rules for known H2 event categories.
- Normalize severity, source, and trace metadata for security events.
- Keep classifier as categorization support, not a WAF replacement.

### H2-008 Rate Limiting Tiered Policies

- Add named policies for login, refresh, 2FA, admin writes, file upload, and security unban.
- Bind policies to endpoints.
- Emit security events on limit hits.
- Avoid over-limiting ordinary reads.

### H2-009 PermissionVersion Backend and Frontend Closure

- Centralize permission version changes.
- Ensure role, permission, menu, user, password reset, and 2FA reset scenarios increment affected users.
- Return permission version through auth responses.
- Store and compare permission version in frontend and refresh `/me` plus menus on mismatch.

### H2-010 Secure Headers and CSP Report-Only

- Add secure headers middleware.
- Emit `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and CSP report-only.
- Keep development Vite workflow unblocked.
- Ensure file preview uses safe content disposition behavior.

