# S13 OpenAPI Diagnostics Upgrade Spec

## Background

Sprint 13 follows the Sprint 12 EventBus and Outbox upgrade. Sprint 13 enhances OpenAPI documentation and local diagnostics infrastructure while preserving the Minimal API architecture and the no-Controller boundary.

Primary source documents:

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `AGENTS.md`
- `.trae/rules/wecms-engineering-principles.md`

## Current State

- OpenAPI export already exists and is covered by backend quality gates.
- Endpoint metadata and permission coverage checks already exist.
- Swagger, Scalar, and MiniProfiler are explicitly allowed only as OpenAPI, interactive documentation, and local diagnostics infrastructure.
- S13 production implementation has not started in this spec track.

## Goals

- Enable Swagger and Scalar UI without introducing MVC Controller infrastructure.
- Keep UI availability development-only by default, with explicit configuration required outside development.
- Enrich OpenAPI output from Endpoint Metadata.
- Emit module, permission, audit, and rate-limit OpenAPI extensions.
- Add MiniProfiler for HTTP and SQL timing diagnostics.
- Keep diagnostics from exposing sensitive SQL parameters.
- Update quality gates so OpenAPI documentation and diagnostics remain contract-safe.

## Non-Goals

- Do not introduce `AddControllers`, `MapControllers`, MVC Controller, ControllerBase, Razor, or Razor Pages.
- Do not use runtime Endpoint auto-scanning.
- Do not generate business contracts dynamically at runtime.
- Do not add frontend behavior.
- Do not add CMS content APIs or CMS runtime behavior.
- Do not add AI runtime capability.
- Do not use EF Core, `dynamic` query or return types, or legacy compatibility fallbacks.
- Do not expose MiniProfiler or interactive API documentation publicly by default in non-development environments.
- Do not log or display sensitive SQL parameters.

## Boundary Decisions

- `WeCms.Api` owns Swagger, Scalar, MiniProfiler, OpenAPI export, and endpoint metadata projection.
- Business modules continue to expose explicit Minimal API endpoint definitions and metadata; they do not depend on Swagger, Scalar, or MiniProfiler packages.
- OpenAPI metadata must be derived from explicit endpoint metadata, permission metadata, audit metadata, and rate-limit metadata.
- Existing OpenAPI JSON artifacts remain contract delivery outputs.
- MiniProfiler is local diagnostics infrastructure only and must not become an audit log, telemetry store, or production tracing dependency.

## Functional Requirements

- Swagger and Scalar UI are available in development or when explicitly enabled by configuration.
- Swagger/Scalar setup does not call `AddControllers` or `MapControllers`.
- OpenAPI security scheme includes bearer authentication.
- OpenAPI operations include permission extensions where applicable.
- OpenAPI operations include module, audit, and rate-limit extensions where applicable.
- OpenAPI export continues to work in CI and local backend gates.
- MiniProfiler is registered in development by default.
- MiniProfiler is disabled by default outside development.
- MiniProfiler records HTTP timing.
- SQL timing integration must redact sensitive parameters.

## Acceptance Criteria

- Swagger/Scalar can be accessed in development without Controller infrastructure.
- OpenAPI export continues to pass.
- OpenAPI contains bearer auth.
- OpenAPI contains `x-wecms-module`.
- OpenAPI contains `x-wecms-permission`.
- OpenAPI contains `x-wecms-audit`.
- OpenAPI contains `x-wecms-rate-limit`.
- MiniProfiler can record HTTP and SQL timing.
- MiniProfiler does not expose sensitive SQL parameters.
- Full backend quality gate passes with MySQL after each Sprint 13 implementation task.
- Final Sprint 13 audit passes with no frontend/CMS/Controller/EF/dynamic/AI scope drift.

## Package Review

S13-T01 adds `Swashbuckle.AspNetCore` 10.2.2.

- Runtime compatibility: NuGet restore verified the package is compatible with the current `net10.0` JIT runtime baseline.
- License: Swashbuckle.AspNetCore is distributed under the MIT license.
- Maintenance: the package is maintained by `domaindrivendev` and has broad ASP.NET Core ecosystem adoption.
- Alternatives considered: using only the existing hand-written OpenAPI export was rejected because S13 explicitly requires Swagger UI; introducing NSwag was rejected to keep the change limited to Swagger UI and the established Swashbuckle ecosystem.

S13-T01 adds `Scalar.AspNetCore.Swashbuckle` 2.16.4.

- Runtime compatibility: NuGet restore verified the package has a `net10.0` asset and is compatible with the current JIT runtime baseline.
- License: Scalar.AspNetCore.Swashbuckle is distributed under the MIT license.
- Maintenance: the package is maintained by Scalar and is the dedicated Scalar integration for Swashbuckle.
- Alternatives considered: using `Scalar.AspNetCore` with the Microsoft OpenAPI endpoint was rejected for S13-T01 because the Swagger UI task already requires Swashbuckle; a static documentation page was rejected because it would not stay tied to OpenAPI output.

S13-T03 adds `MiniProfiler.AspNetCore.Mvc` 4.5.4.

- Runtime compatibility: NuGet restore verified the package is compatible with the current `net10.0` JIT runtime baseline through its ASP.NET Core integration assets.
- License: MiniProfiler.AspNetCore.Mvc is distributed under the MIT license.
- Maintenance: the package is maintained by StackExchange and is the official MiniProfiler ASP.NET Core integration package.
- Alternatives considered: adding only `MiniProfiler.AspNetCore` was rejected because it provides middleware without the needed service registration extension; building custom request/SQL timing storage was rejected because S13 explicitly scopes local diagnostics to MiniProfiler.
