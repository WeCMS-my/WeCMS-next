# M0-BE-009 System API Checklist

- [x] `GET /health/live` does not depend on database.
- [x] `GET /health/ready` checks database connectivity.
- [x] `GET /api/v1/system/ping` returns `ApiResult<T>`.
- [x] `GET /api/v1/system/version` returns `ApiResult<T>`.
- [x] `GET /api/v1/system/db-check` checks database connectivity.
- [x] DB check failure does not expose `Exception.Message`.
- [x] `secure-ping` remains permission protected.
- [x] System DTOs are in `WeCmsJsonSerializerContext`.
