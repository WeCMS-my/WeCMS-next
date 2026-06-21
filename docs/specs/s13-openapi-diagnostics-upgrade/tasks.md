# S13 OpenAPI Diagnostics Upgrade Tasks

## S13-T00 Spec Trio

Create Sprint 13 spec, tasks, and checklist before production code changes.

Required proof:

- `docs/specs/s13-openapi-diagnostics-upgrade/spec.md`
- `docs/specs/s13-openapi-diagnostics-upgrade/tasks.md`
- `docs/specs/s13-openapi-diagnostics-upgrade/checklist.md`
- documentation consistency audit

## S13-T01 Swagger And Scalar UI

Add Swagger and Scalar UI infrastructure without introducing Controller support.

Required work includes:

- add Swagger and Scalar packages after package review
- register Swagger/Scalar only in `WeCms.Api`
- enable UI only in Development or with explicit configuration
- do not call `AddControllers`
- do not call `MapControllers`
- use Minimal API endpoint metadata for document generation
- add bearer auth security scheme
- show permission-code extensions in OpenAPI

Required proof includes:

- `Swagger_IsNotUsingControllers`
- `Swagger_EnabledInDevelopment`
- `Swagger_NotEnabledByDefaultInNonDevelopment`
- `OpenApi_ContainsBearerAuth`
- `OpenApi_ContainsPermissionExtensions`

## S13-T02 OpenAPI Metadata Upgrade

Upgrade OpenAPI metadata generation from explicit endpoint metadata.

Required work includes:

- read module metadata from endpoints
- read permission metadata from endpoints
- read audit metadata from endpoints
- read rate-limit metadata from endpoints
- emit `x-wecms-module`
- emit `x-wecms-permission`
- emit `x-wecms-audit`
- emit `x-wecms-rate-limit`
- reduce hand-written endpoint descriptors only when equivalent metadata proof exists
- keep OpenAPI export stable
- update OpenAPI coverage tests

Required proof includes:

- `OpenApiExport_IncludesModuleMetadata`
- `OpenApiExport_IncludesAuditMetadataForWrites`
- `OpenApiExport_IncludesRateLimitMetadata`
- `OpenApiExport_StillCoversAllBusinessEndpoints`

## S13-T03 MiniProfiler

Add local diagnostics timing through MiniProfiler.

Required work includes:

- add MiniProfiler package after package review
- register HTTP request timing
- connect SQL audit timing to MiniProfiler
- enable by default only in Development
- disable by default in non-development environments
- redact sensitive SQL parameters
- avoid exposing profiler UI publicly by default

Required proof includes:

- `MiniProfiler_RegisteredInDevelopment`
- `MiniProfiler_NotEnabledByDefaultInNonDevelopment`
- `MiniProfiler_RecordsHttpTiming`
- `SqlTiming_DoesNotExposeSensitiveParameters`

## S13-T04 Final Sprint 13 Audit

Run a total audit after S13-T01 through S13-T03 complete.

Required proof includes:

- Swagger/Scalar UI usable in development
- Swagger/Scalar does not introduce Controller infrastructure
- OpenAPI export continues to pass
- OpenAPI metadata extensions are complete
- MiniProfiler can record HTTP and SQL timing
- MiniProfiler redacts sensitive SQL parameters
- no frontend/CMS scope drift
- no Controller/MVC/Razor/EF Core/dynamic/AI runtime capability
- final checklist complete
- full backend quality gate with MySQL
