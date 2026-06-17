# P1-AUTHZ-003 Checklist

- [x] Spec-first three-file set exists before production code changes.
- [x] Red tests were observed for the current task before implementation, or N/A is documented for script/spec-only changes.
- [x] `sys_role.is_locked` is `BOOLEAN NOT NULL DEFAULT FALSE`.
- [x] `super_admin` is `is_builtin = TRUE`, `is_locked = TRUE`, `status = enabled`.
- [x] `admin` is enabled and holds `super_admin`.
- [x] Every locked role has at least one enabled non-deleted holder after seed.
- [x] `RoleSummaryDto` and `RoleDetailDto` include `IsLocked`.
- [x] Create/update role request DTOs do not include `IsLocked` or `IsBuiltin`.
- [x] New roles are explicitly inserted with `is_locked = FALSE`.
- [x] Locked roles cannot be updated, deleted, disabled, assigned permissions, or assigned menus.
- [x] Last enabled holder of a locked role cannot be deleted, disabled, or stripped of that locked role.
- [x] Adding a locked role holder is allowed.
- [x] Removing one locked role holder is allowed when another enabled holder remains.
- [x] OpenAPI export includes `isLocked` for role schemas.
- [x] `JsonSerializerContext` covers updated role DTO contracts.
- [x] Static locked-role seed check is wired into `scripts/quality-gate-backend.sh`.
- [x] `dotnet build backend/WeCms.slnx -warnaserror` passed.
- [x] `dotnet test backend/WeCms.slnx` passed.
- [x] `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false` passed.
- [x] `bash scripts/quality-gate-backend.sh` passed.
- [x] Final code audit found no P1-AUTHZ-003 implementation blocking issue.

## Verification note

- `dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --no-restore`: 139 passed.
- `dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --no-restore`: 41 passed.
- `dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo --no-restore`: 24 passed with `WECMS_TEST_MYSQL_CONNECTION_STRING`.
- `dotnet test backend/WeCms.slnx --nologo`: 41 architecture, 139 unit, and 24 integration tests passed with `WECMS_TEST_MYSQL_CONNECTION_STRING`.
- `bash scripts/quality-gate-backend.sh`: 17/17 steps passed with `WECMS_TEST_MYSQL_CONNECTION_STRING`.
