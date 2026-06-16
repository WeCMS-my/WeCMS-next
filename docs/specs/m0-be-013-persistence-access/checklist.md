# M0-BE-013 Persistence Access Checklist

- [x] `WeCms.Persistence` remains the only runtime place to reference `SqlSugar` / SQL text.
- [x] `PermissionRepository` data access is fully async.
- [x] Repository cancellation tokens are preserved for DB calls.
- [x] System API metadata audit test verifies explicit permission coverage.
- [x] OpenAPI auth contract checks include logout security/metadata expectations.
