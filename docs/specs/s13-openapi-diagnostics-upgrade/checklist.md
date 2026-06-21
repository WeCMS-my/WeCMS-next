# S13 OpenAPI Diagnostics Upgrade Checklist

- [x] Spec trio exists before Sprint 13 production code changes.
- [x] Sprint 13 boundary is documented: Swagger, Scalar, OpenAPI metadata, and MiniProfiler diagnostics only.
- [x] Current OpenAPI and diagnostics baseline is documented.
- [x] Swagger package review is documented.
- [x] Scalar package review is documented.
- [x] Swagger/Scalar are registered only in `WeCms.Api`.
- [x] Swagger/Scalar UI is enabled in Development.
- [x] Swagger/Scalar UI is disabled by default outside Development.
- [x] Swagger/Scalar setup does not call `AddControllers`.
- [x] Swagger/Scalar setup does not call `MapControllers`.
- [x] OpenAPI contains bearer auth security scheme.
- [x] OpenAPI contains permission-code extensions.
- [x] OpenAPI contains `x-wecms-module`.
- [x] OpenAPI contains `x-wecms-permission`.
- [x] OpenAPI contains `x-wecms-audit`.
- [x] OpenAPI contains `x-wecms-rate-limit`.
- [x] OpenAPI export continues to pass.
- [x] OpenAPI coverage tests are updated.
- [x] MiniProfiler package review is documented.
- [x] MiniProfiler is registered in Development.
- [x] MiniProfiler is disabled by default outside Development.
- [x] MiniProfiler records HTTP timing.
- [x] SQL timing can be recorded through MiniProfiler.
- [x] SQL timing redacts sensitive parameters.
- [x] S13 does not add frontend behavior.
- [x] S13 does not implement CMS content APIs or CMS runtime behavior.
- [x] S13 does not introduce Controller/MVC/Razor endpoint surface.
- [x] S13 does not introduce EF Core, dynamic query/return type, legacy fallback, or AI runtime capability.
- [x] Full backend quality gate passes with MySQL for each completed Sprint 13 implementation task.
- [x] Final Sprint 13 total audit passes.

Audit note: `scope-audit.md` records the Sprint 13 owned surface, frontend diff attribution, final audit commands, and the live-port smoke environment limitation.
