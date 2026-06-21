# S10 QueryFilter Raw SQL Guardrails

SqlSugar `QueryFilter` applies to `Queryable<T>` pipelines registered through `QueryFilterRegistrar`.

Raw SQL executed through `Ado`, including `SqlQuery`, `GetScalar`, and `ExecuteCommand`, is not automatically rewritten by QueryFilter. Any raw SQL that reads or mutates soft-delete, tenant, or data-scoped tables must either:

- include explicit `deleted_at IS NULL`, `tenant_id`, and data-scope predicates in the reviewed SQL; or
- use a deliberate QueryFilter bypass with a non-empty reason and audit evidence before the raw SQL is accepted for migration or maintenance work.

The default `QueryFilterBypass` writes a `QueryFilterBypassAuditEvent` through `IQueryFilterBypassAuditSink`. S10-T04 defines the bypass contract and test sink. Persistent SQL audit storage belongs to S10-T06 and must not be implemented here.

Bypass scope is intentionally narrow: it prevents filters from being registered while a client is created inside the bypass scope. It does not remove filters already registered on an existing SqlSugar client.
