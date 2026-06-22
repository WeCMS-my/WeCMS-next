# Audit Context Fields Spec

## Goal

Enhance generic write audit records with request context already supported by the `sys_audit_log` schema:

- actor user id
- actor username
- client IP
- user agent
- target id from route values

## Scope

- Extend `AuditWriteRecord` with optional context fields.
- Populate the fields in `AuditEndpointFilter` from `HttpContext`.
- Persist the fields in `SqlSugarAuditWriter` to existing `sys_audit_log` columns.
- Add focused unit coverage for endpoint-filter context extraction.

## Out of Scope

- No database schema changes.
- No public API or OpenAPI shape changes.
- No frontend changes.
- No changes to module-specific service audit records.

## Constraints

- Keep ASP.NET Core Minimal APIs.
- Keep SQL/SqlSugar access inside `.SqlSugar` adapter code.
- Do not introduce controllers, EF Core, dynamic data access, legacy fallback, or AI runtime code.
- `target_id` is best-effort for generic endpoint audit and comes from stable route values only.
