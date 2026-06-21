# S2 Endpoint Platform Spec

## Scope

Implement the Sprint 2 endpoint platform from `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`.

This change creates the first explicit endpoint-definition foundation while preserving the current Minimal API and static OpenAPI architecture.

This change covers:

- explicit `IEndpointDefinition` registration,
- endpoint metadata records for module, permission, audit, rate limit, and validation,
- endpoint convention helpers for common metadata,
- validation and audit endpoint filters,
- a low-risk migration example for `GET /api/v1/system/ping`,
- OpenAPI/source endpoint coverage updates required by the migrated endpoint.

## Requirements

- Keep ASP.NET Core Minimal APIs and `WebApplication.CreateSlimBuilder(args)`.
- Do not introduce Controller, ControllerBase, Razor, Razor Pages, or `AddControllers`/`MapControllers`.
- Endpoint registration must stay explicit. Runtime assembly scanning, `GetTypes()`, and AppDomain-based endpoint discovery are forbidden.
- `IEndpointDefinition` must allow route definitions to be registered through a concrete registry from `Program.cs`.
- Metadata models must be typed records and must not depend on persistence or SqlSugar.
- Convention helpers must fail fast on invalid text input and must only add metadata.
- `ValidationEndpointFilter<TRequest>` must support multiple validators, pass through when no validator exists, fail fast when validators exist but no request argument is present, and return the unified `ApiResult` validation response on validation failure.
- `AuditEndpointFilter` must read `EndpointAuditMetadata`, write started/completed/failed events through `IAuditWriter`, provide a Noop writer, and must not write SQL.
- The migrated platform ping endpoint must keep `GET /api/v1/system/ping` route compatibility for this sprint and must be registered through `PlatformEndpointDefinition`.
- OpenAPI source coverage must include `WeCms.Api/Endpoints/*EndpointDefinition.cs` so migrated endpoint definitions remain covered by contract tests.

## Non-Goals

- Migrating every existing endpoint to `EndpointDefinition`.
- Replacing the static OpenAPI exporter with runtime `EndpointDataSource` discovery.
- Persisting audit filter events to the audit-log table.
- Automatically attaching validation or audit filters from metadata for all endpoints.
- Moving all health/platform records and probes out of `WeCms.Modules.System`; that remains a later platform migration task.
- Adding frontend generated types or changing SoybeanAdmin runtime behavior.

## Acceptance

- `EndpointDefinition` mapping tests cover explicit definition and registry registration.
- Metadata and convention extension tests cover all S2 metadata helpers.
- Validation filter tests cover invalid, valid, no-validator, and request-missing cases.
- Audit filter tests cover success, failure, and no-metadata cases.
- Platform endpoint definition tests cover route, method, anonymous access, and OpenAPI response metadata.
- OpenAPI source coverage includes endpoint definition files.
- `scripts/quality-gate-backend.sh` passes after the S2 implementation.
- Local audits pass for no Controller, minimal endpoint metadata, layer dependency, SqlSugar boundary, DB boundary, and DI boundary.
