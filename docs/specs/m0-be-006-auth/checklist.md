# M0-BE-006 Auth Checklist

- [x] Username/password empty input fails before repository lookup.
- [x] Failed login message is generic.
- [x] Failed login writes `sys_login_log`.
- [x] Failed login writes `sys_security_event`.
- [x] Successful login returns access token.
- [x] Successful login returns refresh token.
- [x] Refresh token is stored only as a hash.
- [x] Successful login updates `last_login_at` and `last_login_ip`.
- [x] `/api/v1/auth/me` requires authorization.
- [x] `/api/v1/auth/me` returns user, roles, permissions, and empty menus.
- [x] Auth DTOs are included in `WeCmsJsonSerializerContext`.
- [x] No MVC Controller, EF Core, Dapper, dynamic, or AI runtime code is introduced.
