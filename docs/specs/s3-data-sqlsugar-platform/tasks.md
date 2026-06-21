# S3 Data SqlSugar Platform Tasks

1. Add the S3 Data.SqlSugar platform spec trio before production code changes.
2. Migrate `SqlSugarUnitOfWork` and `SqlSugarTransactionContext` into `WeCms.Data.SqlSugar`.
3. Add multi-connection database option models and fail-fast option reader.
4. Add `SqlSugarConnectionRegistry` for enabled named connection configs.
5. Upgrade `SqlSugarClientFactory` to support default and named client creation with hooks.
6. Move migration and seed runner platform code to `WeCms.Data.SqlSugar`.
7. Add CodeFirst model provider, registry, runner, and schema validator skeleton.
8. Run focused tests, the full backend quality gate, and S3 rule audits before closing each task.
