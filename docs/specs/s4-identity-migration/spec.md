# S4 Identity Migration Spec

## Goal

Migrate Auth, Users, TwoFactor, AccountProfile, RefreshToken, login failure, password, token, session, and logout identity code from the transitional `WeCms.Modules.System` namespace into `WeCms.Modules.Identity` and `WeCms.Modules.Identity.SqlSugar`.

## Scope

- `WeCms.Modules.Identity` owns identity DTOs, request/response contracts, application records, service abstractions, services, endpoint definitions, permissions, and repository interfaces.
- `WeCms.Modules.Identity.SqlSugar` owns identity persistence implementations and SqlSugar entities.
- Existing HTTP routes remain backward compatible unless a later endpoint task explicitly changes OpenAPI routes.
- `WeCms.Modules.System` remains a migration allow-list for modules that have not yet moved, but it must no longer own Auth, Users, TwoFactor, or AccountProfile identity code when S4 closes.

## Non-Goals

- Do not migrate AccessControl, Organization, Configuration, Audit, Security, FileCenter, or Platform beyond interfaces required by Identity.
- Do not introduce MVC Controller, Razor, runtime endpoint scanning, EF Core, or AI runtime capability.
- Do not rewrite authentication semantics, token format, refresh-token rotation, 2FA rules, or permission codes except where S4 explicitly moves ownership.

## Acceptance

- Auth, Users, TwoFactor, and AccountProfile DTOs and records live under `WeCms.Modules.Identity`.
- Identity application services depend on interfaces and do not reference SqlSugar or concrete AccessControl implementations.
- Identity endpoints are registered through explicit Minimal API endpoint definitions and carry permission, audit, rate-limit, and OpenAPI metadata.
- Identity repository interfaces expose no SqlSugar or connector types.
- Identity repository implementations live in `WeCms.Modules.Identity.SqlSugar`.
- Identity DI is provided by `AddWeCmsIdentity()` and `AddWeCmsIdentitySqlSugar()`.
- Identity permission seed coverage and OpenAPI `x-wecms-permission` metadata remain valid.
- Full backend quality gate and S4-focused audits pass after each completed task.
