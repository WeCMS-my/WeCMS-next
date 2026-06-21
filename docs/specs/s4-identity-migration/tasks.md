# S4 Identity Migration Tasks

1. Add the S4 Identity migration spec trio before production code changes.
2. Move Identity contracts, DTOs, and records from `WeCms.Modules.System` into `WeCms.Modules.Identity`.
3. Move Identity services from System into `WeCms.Modules.Identity` and keep dependencies abstract.
4. Move Identity endpoints into explicit endpoint definitions.
5. Move Identity repository interfaces into `WeCms.Modules.Identity.Repositories`.
6. Move Identity SqlSugar repository implementations into `WeCms.Modules.Identity.SqlSugar`.
7. Add Identity and Identity.SqlSugar DI registration and update API startup.
8. Move Identity permission definitions and seed ownership.
9. Run focused tests, full backend quality gate, and S4 rule audits before closing each task.
