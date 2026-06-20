# ADR-0019：SqlSugar 数据平台与模块持久化适配层

## 状态

Accepted

## 背景

当前 `WeCms.Persistence` 同时承载 SqlSugar client、UnitOfWork、Migration、Seed、Repository 实现、数据库探针和系统基础模块 SQL。随着系统基础破坏性升级引入模块拆分、CodeFirst、QueryFilter、多连接、多租户和 SQL 审计，继续扩展单一 Persistence 项目会让数据访问边界失控。

本阶段允许不兼容旧结构，因此可以把旧持久化大模块拆成数据平台层和模块适配层。

## 决策

1. 新增 `WeCms.Data.SqlSugar` 作为 SqlSugar 数据平台层。
2. 新增 `WeCms.Modules.*.SqlSugar` 作为各业务模块持久化适配层。
3. 旧 WeCms.Persistence 不作为长期合法项目。
4. 迁移期间允许旧 `WeCms.Persistence` 暂存，但不得继续新增长期平台能力。
5. 最终验收时，SqlSugar / MySqlConnector / ORM Client 只能出现在：
   - `WeCms.Data.SqlSugar`
   - `WeCms.Modules.*.SqlSugar`
6. `WeCms.Modules.*` 业务模块不得引用 `WeCms.Data.SqlSugar` 或 `WeCms.Modules.*.SqlSugar`。
7. `WeCms.Modules.*` 业务模块不得包含 SQL 文本、SqlSugar 类型或 MySQL 连接器类型。

## Data.SqlSugar 职责

`WeCms.Data.SqlSugar` 只承担数据平台能力：

- SqlSugar client / scope 创建。
- 多连接注册和解析。
- 租户连接解析。
- UnitOfWork / TransactionContext。
- CodeFirst model registry。
- Schema validator。
- Migration runner。
- Seed runner。
- QueryFilter 注册。
- SQL 审计 hook。
- 公共 Entity 基类和数据访问工具。

禁止在 `WeCms.Data.SqlSugar` 中承载业务规则、HTTP 逻辑、权限编排或具体业务 Repository。

## 模块 .SqlSugar 职责

每个模块 `.SqlSugar` 项目只承担对应模块的数据访问适配：

- 实现对应模块暴露的 Repository interface。
- 定义该模块 SqlSugar Entity。
- 提供该模块 CodeFirst model provider。
- 提供该模块 seed provider。

禁止在模块 `.SqlSugar` 项目中承载业务编排、HTTP 逻辑或权限决策。

## CodeFirst + Migration 双轨

本项目采用 CodeFirst + Migration 双轨：

- CodeFirst 用于建模、开发环境初始化和 schema validate。
- Migration 用于固化、审查和 CI smoke。
- 当前无生产环境，允许重置系统基础数据库 baseline。
- 未来生产环境不得自动 DDL。

## QueryFilter

QueryFilter 用于运行时治理：

- 软删除过滤。
- 租户过滤。
- 数据权限过滤。

QueryFilter 不自动覆盖 `_db.Ado.SqlQueryAsync` 等原始 SQL API。新模块 Repository 应优先使用 Queryable；必须使用原始 SQL 时必须经过统一 SQL builder 或显式审计。

## SQL 审计

SQL 审计必须覆盖慢 SQL 和失败 SQL，并在测试环境可验证。审计记录应包含 traceId、userId、tenantId、connection name、repository name、SQL hash、脱敏参数、耗时、affected rows 和错误信息。

SQL 审计必须脱敏以下字段：

```text
password
password_hash
token
refresh_token
access_token
secret
two_factor
recovery_code
private_key
connection_string
```

SQL 审计不得递归审计自身。

## 验收

- 迁移期：`WeCms.Persistence` 允许作为 transition allow-list。
- 最终期：开启最终验收标志后，`WeCms.Persistence` 存在即失败。
- `WeCms.Modules.*` 不得出现 SqlSugar / MySqlConnector / SQL 文本。
- `WeCms.Data.SqlSugar` 不得依赖业务模块具体实现。
- `WeCms.Modules.*.SqlSugar` 只能依赖对应模块、`WeCms.Data.SqlSugar` 和 `WeCms.Shared`。
- SQL 审计脱敏测试必须覆盖 password/token/secret/2FA 等敏感字段。

## 关联

- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
