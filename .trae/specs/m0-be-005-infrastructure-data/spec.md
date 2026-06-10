# M0-BE-005: 实现 Infrastructure Data / Dapper.AOT Spec

## Why
M0-BE 需要 MySQL 连接基础设施和 Dapper.AOT 数据访问底座。当前 Infrastructure 项目只有空壳，无法连接数据库。

## What Changes
- 新增 NuGet 包：`MySqlConnector`、`Dapper`、`Dapper.AOT`
- 新增 `IDbConnectionFactory` 接口：定义 MySQL 连接工厂
- 新增 `DbConnectionFactory`：基于 MySqlConnector 的连接工厂实现
- 新增 `IUnitOfWork` 接口：定义事务边界
- 新增 `UnitOfWork`：基于 DbConnection 的事务实现
- 新增 `DapperDataExtensions`：`IServiceCollection` 扩展方法注册数据层
- 新增 `IClock` 实现在 Infrastructure.Time（`SystemClock`），因 Infrastructure 是实现层
- 验证 Dapper.AOT 强类型查询 + AOT publish 通过

## Impact
- Affected specs: M0-BE-002 (AOT 配置), M0-BE-003 (Shared 契约层 — 复用 IClock)
- Affected code: `backend/src/WeCms.Infrastructure/**`, `backend/src/WeCms.Api/Program.cs`
- New NuGet dependencies: 3 packages（说明见 PR）

## ADDED Requirements

### Requirement: IDbConnectionFactory
系统 SHALL 提供 MySQL 连接工厂抽象。

#### Scenario: 创建连接
- **GIVEN** 连接字符串已配置
- **WHEN** 调用 `OpenAsync(CancellationToken)`
- **THEN** 返回打开的 `DbConnection`（MySqlConnection 实例）

### Requirement: DbConnectionFactory
系统 SHALL 基于 MySqlConnector 实现连接工厂，从 `IConfiguration` 读取连接串。

#### Scenario: 获取连接字符串
- **WHEN** 初始化 DbConnectionFactory
- **THEN** 从配置 `ConnectionStrings:Default` 读取

### Requirement: IUnitOfWork
系统 SHALL 提供事务管理抽象。

#### Scenario: 提交事务
- **GIVEN** 事务已开始
- **WHEN** 调用 `CommitAsync()`
- **THEN** 数据库事务提交
- **AND** 连接释放

#### Scenario: 回滚事务
- **GIVEN** 事务已开始
- **WHEN** 调用 `RollbackAsync()` 或 dispose 时未 commit
- **THEN** 数据库事务回滚

### Requirement: Dapper.AOT 数据访问规则
系统 SHALL 在所有数据访问中遵守：
1. 禁止 `Query<dynamic>`，只使用强类型 DTO
2. 禁止 `SELECT *`，显式列出字段
3. 禁止拼接用户输入到 SQL
4. 所有 Repository 方法必须接收 `CancellationToken`
5. Repository 只负责 SQL，不包含业务逻辑

### Requirement: NuGet 依赖
新增 NuGet 包：
- `MySqlConnector` — MySQL ADO.NET 驱动，AOT 兼容
- `Dapper` — 轻量 ORM
- `Dapper.AOT` — Dapper AOT 源生成器

### Requirement: SystemClock
系统 SHALL 在 Infrastructure 层提供 `IClock` 的 `SystemClock` 实现。

## REMOVED Requirements
无。
