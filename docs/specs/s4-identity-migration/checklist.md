# S4 Identity Migration Checklist

- [x] Spec trio exists before Sprint 4 production code changes.
- [x] Identity DTOs and contracts live in `WeCms.Modules.Identity.Contracts`.
- [x] Identity records live in `WeCms.Modules.Identity.Records`.
- [x] Auth, Users, TwoFactor, and AccountProfile services live in `WeCms.Modules.Identity`.
- [x] Identity service dependencies are interfaces.
- [x] Identity services do not reference SqlSugar or concrete AccessControl implementations.
- [x] Identity endpoints use explicit Minimal API endpoint definitions.
- [x] Identity repository interfaces live in `WeCms.Modules.Identity.Repositories`.
- [x] Identity repository interfaces expose no SqlSugar or connector types.
- [x] Identity repository implementations live in `WeCms.Modules.Identity.SqlSugar`.
- [x] `AddWeCmsIdentity()` and `AddWeCmsIdentitySqlSugar()` register Identity dependencies.
- [x] Old System Auth, Users, and TwoFactor service registrations are removed.
- [x] Identity permissions and seed coverage are updated.
- [x] Login, refresh, Me, user CRUD, and 2FA tests pass.
- [x] No Controller/MVC/Razor endpoint surface is introduced.
- [x] No AI runtime capability is introduced.
- [x] Full backend quality gate passes for each completed S4 task.
