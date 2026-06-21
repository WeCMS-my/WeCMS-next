# ADR-0011：M0-BE JIT + SqlSugar Persistence 边界

> S14 更新：本 ADR 的运行时基线仍有效；旧 `WeCms.Persistence` 边界已被 ADR-0019 的 `WeCms.Data.SqlSugar` + `WeCms.Modules.*.SqlSugar` 最终边界取代。

## 状态

Accepted

## 背景

ADR-0009 已将运行时基线从 Native AOT 切换为 `.NET 10 JIT publish/runtime`。M0-BE 还需要把数据库访问技术路线和项目边界固定下来，避免后续把 SqlSugar 泄漏到模块层，或把旧 Dapper / AOT 门禁重新带回主线。

## 决策

1. M0-BE 使用 `.NET 10 JIT publish/runtime`。
2. M0-BE 使用 SqlSugarCore + MySQL 作为数据库访问实现。
3. S14 后 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 是唯一允许直接引用 SqlSugarCore、MySqlConnector、数据库连接、ORM Client、SQL 和 migration/seed 逻辑的生产项目边界。
4. `WeCms.Modules.*` 只能定义 repository port、DTO、业务服务、权限常量和 endpoint；不得引用 SqlSugarCore、MySqlConnector、`DbConnection`、`DbTransaction`，也不得包含 SQL 文本。
5. Repository interface 放在模块层或 `WeCms.Shared`；Repository implementation 只能放在 `WeCms.Modules.*.SqlSugar`。
6. 取消 Native AOT、`PublishAot`、Dapper、Dapper.AOT、IL2026/IL3050 作为现行开发与质量门禁。
7. 后端发布门禁使用标准 JIT publish：

```bash
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

## 影响

- 后端可按常规 JIT 路径使用 SqlSugar，但数据库边界和 SQL 纪律不放松。
- 架构测试和脚本必须持续检查 Data.SqlSugar、module `.SqlSugar`、Layer、DI 边界。
- 后续新增数据库访问必须从模块 port 到对应 module `.SqlSugar` adapter 闭环实现。

### Migration / Seed SQL contract

- `DbMigrationRunner.SplitSqlStatements()` and `SeedRunner` currently support only simple statement-oriented SQL files.
- Statements are split by line endings that terminate with `;`, and lines starting with `--` are skipped as comments.
- `ExtractCreatedTableNames()` only recognizes `CREATE TABLE ...` statements that begin on a single logical line.
- Therefore `database/migrations/*.sql` and `database/seeds/*.sql` must not contain `DELIMITER`, stored procedures, triggers, functions, or any body that relies on internal semicolons or multi-line parser semantics.
- If future schema work requires those features, the change must first introduce a stricter parser or a different migration execution contract, with dedicated tests and spec coverage.

### P2-003 数据访问策略（当前阶段）

- 保持模块 `.SqlSugar` adapter 的 SQL-first 实施：在已迁移的 AuthRepository 场景中，优先保留显式参数化 SQL，确保行为可控、可审计。
- 所有查询与写入必须明确使用 `SugarParameter` 绑定参数，不接受字符串拼接用户输入。
- 关键路径（登录/刷新/退出/安全事件）必须保留集成覆盖（包括 `AuthIntegrationTests`）并通过 OpenAPI/请求体回归脚本，避免 SQL 与契约漂移。
- 需要逐步迁移到 ORM-first 时，仅能在同一批次内补齐 Queryable / Insertable / Updateable 改造与回归测试，不得新引入未覆盖 SQL。

## 验收

- `SqlSugarCore` 只出现在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar`。
- 全仓无 Dapper / Dapper.AOT 主线实现。
- 全仓无现行 `PublishAot` 门禁。
- `Modules` 无 SQL / ORM /持久化实现依赖。
