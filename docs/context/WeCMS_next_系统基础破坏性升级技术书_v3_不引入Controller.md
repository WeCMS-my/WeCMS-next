# WeCMS-next 系统基础破坏性升级技术书 v3

> 适用仓库：`WeCMS-my/WeCMS-next`  
> 适用阶段：无生产环境使用、允许大范围破坏性升级、暂不实现 CMS 内容模块，只升级系统基础功能模块  
> 核心决策：**继续使用 ASP.NET Core Minimal API；明确不引入 Controller Web API / MVC Controller / ControllerBase。**  
> 交付目标：形成 Codex / AI Coding Agent 可直接执行的升级计划、任务拆分、验收标准和测试要求。  
> 文件性质：技术规划文档，不直接修改仓库代码。

---

## 0. 总体决策

### 0.1 本次明确不做的事

本次升级明确 **不做**：

1. 不引入 MVC Controller。
2. 不引入 ControllerBase。
3. 不使用 `AddControllers()`。
4. 不使用 `MapControllers()`。
5. 不使用 Razor / Razor Pages。
6. 不把 HTTP API 切换为传统 Controller Web API 架构。
7. 不实现 CMS 内容模块，包括站点、栏目、内容、审核流、发布流、搜索索引等。
8. 不做旧 ThinkPHP runtime compatibility。
9. 不做旧数据迁移兼容。
10. 不考虑生产环境兼容与平滑迁移。
11. 不保留旧 `WeCms.Modules.System` 大模块作为长期结构。
12. 不保留旧 `WeCms.Persistence` 大一统持久化结构作为长期结构。

### 0.2 本次要做的事

本次升级目标是：在保持 Minimal API HTTP 入口路线的前提下，进行一次系统基础能力的破坏性模块化升级。

核心升级方向：

1. 保持并升级 Minimal API，改为模块化 Endpoint Definition 架构。
2. 拆分臃肿的 `WeCms.Modules.System`。
3. 拆分臃肿的 `WeCms.Persistence`。
4. 建立系统基础模块边界：
   - Identity
   - AccessControl
   - Organization
   - Configuration
   - Audit
   - Security
   - FileCenter
   - Platform
5. 建立 SqlSugar 数据平台：
   - CodeFirst 建模
   - Migration 固化
   - QueryFilter 运行时治理
   - 多库 / 多租户连接管理
   - SQL 日志与审计
6. 建立 RBAC + URL 权限 + 按钮权限体系。
7. 建立统一缓存抽象。
8. 建立 AOP 事务与缓存拦截。
9. 建立操作日志、异常日志、SQL 日志、审计日志统一模型。
10. 建立 Swagger / Scalar、MiniProfiler、限流、国际化等后台基础设施。
11. 建立 EventBus + Outbox 基础能力，为后续 CMS 内容发布异步动作预留。
12. 同步更新 AGENTS、code_review、ADR、architecture tests、quality gate。

### 0.3 一句话架构方向

> **继续 Minimal API，不引入 Controller Web API；拆分 System 大模块，建立模块化系统基础平台；用 CodeFirst 建模、Migration 固化、QueryFilter 隔离、多库多租户连接治理、SQL 审计可观测；通过 DI/IOC/AOP/缓存/EventBus 提升系统扩展性。**

---

## 1. 当前项目主要问题判断

### 1.1 `WeCms.Modules.System` 已经过于臃肿

当前 `System` 模块承载了：

```text
Auth
Users
Roles
Permissions
Menus
Departments
Posts
Dicts
Settings
I18n
Logs
Security
Files
TwoFactor
System
```

这些功能属于不同变化原因：

| 当前目录 | 实际领域 | 是否应该继续放在 System |
|---|---|---|
| `Auth` | 身份认证 | 否 |
| `Users` | 身份与账号 | 否 |
| `Roles` | 访问控制 | 否 |
| `Permissions` | 访问控制 | 否 |
| `Menus` | 访问控制 / 管理端导航 | 否 |
| `Departments` | 组织结构 | 否 |
| `Posts` | 岗位/职位 | 否，且命名会与 CMS 文章冲突 |
| `Dicts` | 配置元数据 | 否 |
| `Settings` | 配置中心 | 否 |
| `I18n` | 国际化配置 | 否 |
| `Logs` | 审计日志 | 否 |
| `Security` | 安全治理 | 否 |
| `Files` | 文件中心 | 否 |
| `TwoFactor` | 账号安全 | 否 |
| `System` | 平台健康检查 | 否，应改名 Platform |

结论：

> `WeCms.Modules.System` 在早期阶段作为聚合模块可以接受，但在本次升级中必须拆分，否则会演变成 God Module。

### 1.2 `WeCms.Persistence` 也过于集中

当前 `WeCms.Persistence` 同时承担：

```text
SqlSugar client factory
UnitOfWork
Migration runner
Seed runner
Auth repository
User repository
Role repository
Permission repository
Menu repository
Department repository
Post repository
Dict repository
Setting repository
I18n repository
Log repository
Security repository
File repository
System probe
```

问题：

1. 所有系统基础模块的持久化实现集中在一个项目里。
2. Repository 注册集中在一个 `AddWeCmsPersistence` 中。
3. 未来加入多库、多租户、QueryFilter、CodeFirst、SQL 审计后，该项目会继续膨胀。
4. 模块边界无法在持久化层体现。

结论：

> 需要拆成 `WeCms.Data.SqlSugar` 数据平台层 + 各模块 `.SqlSugar` 适配层。

### 1.3 当前 Minimal API 方向是对的，但组织方式需要升级

当前 Minimal API 优点：

1. Endpoint 显式注册。
2. 权限元数据显式挂载。
3. Endpoint Filter 已用于权限检查。
4. 质量门禁可以扫描 Endpoint / OpenAPI / 权限 / 审计覆盖。
5. 与模块化单体匹配。

当前不足：

1. Endpoint Definition 还不够标准化。
2. OpenAPI 元数据维护成本较高。
3. 请求验证机制不够统一。
4. 审计、限流、安全事件元数据还未完全规范化。
5. 随着模块增多，`Program.cs` 容易继续膨胀。

结论：

> 不切 Controller，而是升级 Minimal API 组织方式。

---

## 2. HTTP API 架构决策：继续 Minimal API

### 2.1 硬性规则

升级后必须明确写入 `AGENTS.md`、`code_review.md` 和 ADR：

```text
[必须]
- 使用 ASP.NET Core Minimal API。
- 使用 MapGroup / MapGet / MapPost / MapPut / MapDelete。
- Endpoint 必须显式注册或通过明确的模块定义注册。
- 业务 Endpoint 必须绑定权限码或显式 AllowAnonymous / InternalOnly 策略。
- Endpoint 必须带 OpenAPI 元数据。
- 写操作 Endpoint 必须带审计元数据。
- 高风险写操作必须带限流策略。
- Endpoint Handler 必须保持薄层，只处理 HTTP 绑定和返回。
- 业务逻辑必须在 Application Service 中。

[禁止]
- 禁止 Controller。
- 禁止 ControllerBase。
- 禁止 AddControllers。
- 禁止 MapControllers。
- 禁止 MVC Action Filter 作为业务 API 主入口。
- 禁止 Razor / Razor Pages。
- 禁止 Controller Attribute Routing。
```

### 2.2 推荐 Endpoint Definition 模式

新增接口：

```csharp
namespace WeCms.Shared.Endpoints;

public interface IEndpointDefinition
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

每个模块提供一个或多个 Endpoint Definition：

```text
WeCms.Modules.Identity/
  Endpoints/
    AuthEndpoints.cs
    AccountEndpoints.cs
    UserEndpoints.cs

WeCms.Modules.AccessControl/
  Endpoints/
    RoleEndpoints.cs
    PermissionEndpoints.cs
    MenuEndpoints.cs
```

示例：

```csharp
public sealed class UserEndpointDefinition : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity/users")
            .WithTags("Identity.Users")
            .RequireAuthorization();

        group.MapGet("", ListAsync)
            .WithName("Identity.Users.List")
            .WithSummary("List users")
            .ProducesApi<PagedResult<UserSummaryDto>>()
            .RequirePermission(UserPermissions.List)
            .Audit("identity", "user", "list");

        group.MapPost("", CreateAsync)
            .WithName("Identity.Users.Create")
            .WithSummary("Create user")
            .ProducesApi<UserMutationResponse>()
            .RequirePermission(UserPermissions.Create)
            .RequireRateLimiting(RateLimitPolicyNames.AdminWrite)
            .Validate<CreateUserRequest>()
            .Audit("identity", "user", "create");
    }

    private static async Task<ApiResult<PagedResult<UserSummaryDto>>> ListAsync(
        int page,
        int pageSize,
        string? keyword,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(new UserListQuery(page, pageSize, keyword), cancellationToken);
        return ApiResult<PagedResult<UserSummaryDto>>.Ok(result);
    }
}
```

### 2.3 Endpoint 统一扩展

新增：

```text
WeCms.Api/Endpoints/EndpointConventionExtensions.cs
WeCms.Api/Endpoints/EndpointValidationExtensions.cs
WeCms.Api/Endpoints/EndpointAuditExtensions.cs
WeCms.Api/Endpoints/EndpointOpenApiExtensions.cs
```

推荐扩展：

```csharp
public static RouteHandlerBuilder ProducesApi<T>(this RouteHandlerBuilder builder);
public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder);
public static RouteHandlerBuilder Audit(this RouteHandlerBuilder builder, string module, string resource, string action);
public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permissionCode);
public static RouteHandlerBuilder RequireButtonPermission(this RouteHandlerBuilder builder, string permissionCode);
public static RouteHandlerBuilder RequireUrlPermission(this RouteHandlerBuilder builder, string permissionCode);
```

### 2.4 Endpoint Filter Pipeline

统一使用 Endpoint Filter，而不是 Controller Filter。

推荐 Filter：

```text
PermissionEndpointFilter
ValidationEndpointFilter<TRequest>
AuditEndpointFilter
IdempotencyEndpointFilter
SecurityEventEndpointFilter
```

执行顺序建议：

```text
RequestId Middleware
Exception Middleware
Authentication
SecurityBan
RateLimiter
Authorization
PermissionEndpointFilter
ValidationEndpointFilter
AuditEndpointFilter
Endpoint Handler
```

### 2.5 OpenAPI / Swagger / Scalar 策略

不引入 Controller，也可以引入 Swagger/Scalar。

推荐：

1. 开发环境启用 Swagger/Scalar UI。
2. CI 继续导出 OpenAPI JSON。
3. OpenAPI 读取 Endpoint Metadata。
4. 自动输出：
   - `x-wecms-permission`
   - `x-wecms-audit`
   - `x-wecms-rate-limit`
   - `x-wecms-module`
5. 不再手工维护大量 endpoint descriptor。

---

## 3. 目标项目结构

### 3.1 生产代码目标结构

```text
backend/src/
  WeCms.Api/
    Program.cs
    Endpoints/
    Middleware/
    OpenApi/
    RateLimiting/
    Security/
    Configuration/

  WeCms.Shared/
    Api/
    Data/
    Endpoints/
    Security/
    Caching/
    Events/
    Auditing/
    Tenancy/
    Time/
    Results/

  WeCms.Infrastructure/
    Files/
    Id/
    Clock/
    Crypto/
    External/

  WeCms.Data.SqlSugar/
    Data/
    CodeFirst/
    Filters/
    Tenancy/
    Audit/
    Migration/
    Seed/
    Entities/Common/

  WeCms.Caching/
    Abstractions/
    Memory/
    Redis/

  WeCms.EventBus/
    Abstractions/
    InMemory/
    Outbox/

  WeCms.Aop/
    Attributes/
    Interceptors/
    Autofac/

  WeCms.Modules.Identity/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.Identity.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.AccessControl/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.AccessControl.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.Organization/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.Organization.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.Configuration/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.Configuration.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.Audit/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.Audit.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.Security/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.Security.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.FileCenter/
    Endpoints/
    Services/
    Contracts/
    Permissions/
    Repositories/
    Records/

  WeCms.Modules.FileCenter.SqlSugar/
    Repositories/
    Entities/

  WeCms.Modules.Platform/
    Endpoints/
    Services/
    Contracts/
```

### 3.2 暂不启用 CMS 模块

本次暂不实现 CMS 内容模块。建议：

```text
WeCms.Modules.Cms 暂时不参与 Api 引用
WeCms.Modules.Cms 暂时不参与 Data.SqlSugar 引用
WeCms.Modules.Cms 暂时不参与 OpenAPI
WeCms.Modules.Cms 暂时不参与 quality gate 功能覆盖
```

如果保留目录，必须加入说明：

```text
该模块为后续 CMS 内容域预留。
本次系统基础升级期间不得向 Cms 模块添加系统基础能力。
```

---

## 4. 拆分 System 模块

### 4.1 拆分映射表

| 当前 `WeCms.Modules.System` 子目录 | 新模块 |
|---|---|
| `Auth` | `WeCms.Modules.Identity` |
| `TwoFactor` | `WeCms.Modules.Identity` |
| `Users` | `WeCms.Modules.Identity` |
| `Roles` | `WeCms.Modules.AccessControl` |
| `Permissions` | `WeCms.Modules.AccessControl` |
| `Menus` | `WeCms.Modules.AccessControl` |
| `Departments` | `WeCms.Modules.Organization` |
| `Posts` | `WeCms.Modules.Organization/Positions` |
| `Dicts` | `WeCms.Modules.Configuration` |
| `Settings` | `WeCms.Modules.Configuration` |
| `I18n` | `WeCms.Modules.Configuration` |
| `Logs` | `WeCms.Modules.Audit` |
| `Security` | `WeCms.Modules.Security` |
| `Files` | `WeCms.Modules.FileCenter` |
| `System` | `WeCms.Modules.Platform` |

### 4.2 `Posts` 必须重命名为 `Positions`

原因：

1. CMS 中 `Post` 极易被理解为文章。
2. 系统岗位应使用 `Position`。
3. 避免后续 CMS 内容模块与系统岗位冲突。

破坏性重命名：

```text
Post -> Position
Posts -> Positions
sys_post -> sys_position
sys_user_post -> sys_user_position
PostService -> PositionService
IPostRepository -> IPositionRepository
PostPermissions -> PositionPermissions
```

### 4.3 删除旧模块

最终状态：

```text
删除 WeCms.Modules.System 项目
删除 WeCms.Persistence 项目
删除旧 namespace WeCms.Modules.System.*
删除旧 namespace WeCms.Persistence.*
```

Codex 执行时允许短期编译中间态，但最终 PR 不应保留旧项目。

---

## 5. 拆分 Persistence

### 5.1 新数据平台层

新增：

```text
WeCms.Data.SqlSugar
```

职责：

1. SqlSugar client / scope 创建。
2. 多连接注册。
3. 租户连接解析。
4. UnitOfWork。
5. TransactionContext。
6. CodeFirst Runner。
7. Schema Validator。
8. Migration Runner。
9. Seed Runner。
10. QueryFilter 注册。
11. SQL Audit 注册。
12. 公共 Entity 基类。
13. 公共数据访问工具。

禁止：

1. 禁止写具体业务 Repository。
2. 禁止依赖具体业务实现。
3. 禁止承载业务规则。
4. 禁止承载 HTTP 逻辑。
5. 禁止承载权限编排。

### 5.2 模块 SqlSugar 适配层

每个模块一个持久化适配项目：

```text
WeCms.Modules.Identity.SqlSugar
WeCms.Modules.AccessControl.SqlSugar
WeCms.Modules.Organization.SqlSugar
WeCms.Modules.Configuration.SqlSugar
WeCms.Modules.Audit.SqlSugar
WeCms.Modules.Security.SqlSugar
WeCms.Modules.FileCenter.SqlSugar
```

每个适配层职责：

1. 实现对应模块的 Repository 接口。
2. 定义对应模块的 SqlSugar Entity。
3. 承载该模块的 CodeFirst model provider。
4. 承载该模块的 seed provider。
5. 不承载业务规则。

---

## 6. 依赖矩阵

### 6.1 允许依赖

```text
WeCms.Api
  -> WeCms.Shared
  -> WeCms.Infrastructure
  -> WeCms.Data.SqlSugar
  -> WeCms.Caching
  -> WeCms.EventBus
  -> WeCms.Aop
  -> WeCms.Modules.Identity
  -> WeCms.Modules.Identity.SqlSugar
  -> WeCms.Modules.AccessControl
  -> WeCms.Modules.AccessControl.SqlSugar
  -> WeCms.Modules.Organization
  -> WeCms.Modules.Organization.SqlSugar
  -> WeCms.Modules.Configuration
  -> WeCms.Modules.Configuration.SqlSugar
  -> WeCms.Modules.Audit
  -> WeCms.Modules.Audit.SqlSugar
  -> WeCms.Modules.Security
  -> WeCms.Modules.Security.SqlSugar
  -> WeCms.Modules.FileCenter
  -> WeCms.Modules.FileCenter.SqlSugar
  -> WeCms.Modules.Platform

WeCms.Modules.*
  -> WeCms.Shared
  -> 必要的其他模块 Contracts 抽象

WeCms.Modules.*.SqlSugar
  -> 对应 WeCms.Modules.*
  -> WeCms.Data.SqlSugar
  -> WeCms.Shared

WeCms.Data.SqlSugar
  -> WeCms.Shared

WeCms.Caching
  -> WeCms.Shared

WeCms.EventBus
  -> WeCms.Shared

WeCms.Aop
  -> WeCms.Shared
  -> WeCms.Caching
  -> WeCms.EventBus

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> 无生产项目引用
```

### 6.2 禁止依赖

```text
WeCms.Modules.* 禁止引用 SqlSugar / MySqlConnector
WeCms.Modules.* 禁止引用 *.SqlSugar
WeCms.Modules.* 禁止引用 WeCms.Data.SqlSugar
WeCms.Modules.* 禁止包含 SQL 文本
WeCms.Modules.* 禁止 new Repository
WeCms.Modules.* 禁止 new HttpClient / FileStream / SqlSugarScope / Random / Guid.NewGuid
WeCms.Modules.*.SqlSugar 禁止依赖 WeCms.Api
WeCms.Data.SqlSugar 禁止依赖 WeCms.Api
WeCms.Shared 禁止引用任何生产项目
```

---

## 7. SqlSugar 数据平台升级

### 7.1 CodeFirst 建模

新增实体接口：

```csharp
public interface IEntity<TKey>
{
    TKey Id { get; set; }
}

public interface ISoftDeleteEntity
{
    DateTime? DeletedAt { get; set; }
}

public interface IAuditedEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public interface ITenantEntity
{
    long TenantId { get; set; }
}

public interface IDataScopedEntity
{
    long CreatedByUserId { get; set; }
}
```

新增基础实体：

```csharp
public abstract class EntityBase : IEntity<long>, ISoftDeleteEntity, IAuditedEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
```

模块实体示例：

```csharp
[SugarTable("sys_user")]
[SugarIndex("ux_sys_user_username", nameof(Username), OrderByType.Asc, true)]
public sealed class UserEntity : EntityBase
{
    [SugarColumn(Length = 64)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string DisplayName { get; set; } = string.Empty;

    [SugarColumn(Length = 512)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    public bool IsSuperAdmin { get; set; }

    [SugarColumn(Length = 128)]
    public string SecurityStamp { get; set; } = string.Empty;

    public long PermissionVersion { get; set; }
}
```

### 7.2 Migration 固化

由于当前没有生产环境，本次允许重置 migration baseline。

推荐新 migration：

```text
database/migrations/
  000001_baseline_identity.sql
  000002_baseline_access_control.sql
  000003_baseline_organization.sql
  000004_baseline_configuration.sql
  000005_baseline_audit.sql
  000006_baseline_security.sql
  000007_baseline_file_center.sql
  000008_baseline_platform.sql
  000009_baseline_outbox.sql
```

推荐 seed：

```text
database/seeds/
  000001_seed_system_permissions.sql
  000002_seed_system_menus.sql
  000003_seed_super_admin.sql
  000004_seed_system_settings.sql
  000005_seed_system_dicts.sql
  000006_seed_i18n_messages.sql
```

规则：

```text
开发/测试允许 CodeFirst InitTables
CI 必须执行 migration smoke test
最终数据库结构以 migration 为准
生产环境未来不得自动 DDL
```

### 7.3 QueryFilter 运行时治理

新增：

```text
SoftDeleteQueryFilter
TenantQueryFilter
DataScopeQueryFilter
```

注册方式：

```csharp
public interface IQueryFilterRegistrar
{
    void Register(SqlSugarScopeProvider db);
}
```

软删除：

```csharp
db.QueryFilter.AddTableFilter<ISoftDeleteEntity>(x => x.DeletedAt == null);
```

租户：

```csharp
db.QueryFilter.AddTableFilter<ITenantEntity>(x => x.TenantId == tenantContext.TenantId);
```

数据权限：

```csharp
db.QueryFilter.AddTableFilter<IDataScopedEntity>(
    x => dataScopeContext.IsAllDataAllowed || dataScopeContext.AllowedUserIds.Contains(x.CreatedByUserId));
```

注意：

> QueryFilter 对 `_db.Ado.SqlQueryAsync` 原始 SQL 不自动生效。因此新模块 Repository 应优先使用 Queryable；必须写原始 SQL 的地方必须通过统一 FilterSqlBuilder 或明确审计。

### 7.4 多库 / 多租户连接管理

新增配置模型：

```json
{
  "Database": {
    "DefaultConnection": "main",
    "Connections": [
      {
        "Name": "main",
        "DbType": "MySql",
        "ConnectionStringName": "Default",
        "Role": "Main",
        "Enabled": true
      },
      {
        "Name": "log",
        "DbType": "MySql",
        "ConnectionStringName": "Log",
        "Role": "Log",
        "Enabled": true
      }
    ]
  }
}
```

接口：

```csharp
public interface ISqlSugarClientFactory
{
    ISqlSugarClient Create();
    ISqlSugarClient Create(string connectionName);
    ISqlSugarClient CreateForTenant(long tenantId);
}
```

多租户第一阶段：

```text
共享库 + tenant_id
预留独立库连接 resolver
不做跨库事务
跨库一致性由 Outbox / EventBus 解决
```

### 7.5 SQL 日志与审计

新增：

```csharp
public interface ISqlAuditSink
{
    Task WriteAsync(SqlAuditRecord record, CancellationToken cancellationToken);
}
```

记录字段：

```text
TraceId
UserId
Username
TenantId
ConnectionName
RepositoryName
OperationType
SqlHash
SqlTemplate
ParametersRedacted
ElapsedMs
AffectedRows
IsSlowSql
ErrorMessage
CreatedAt
```

脱敏字段：

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

SqlSugar AOP 注册：

```csharp
client.Aop.OnLogExecuting = ...
client.Aop.OnLogExecuted = ...
client.Aop.OnError = ...
```

要求：

```text
SQL 审计不得递归审计自身
生产默认只记录慢 SQL 和失败 SQL
开发可配置记录全部 SQL
```

---

## 8. 权限模型升级

### 8.1 权限模型

新增：

```csharp
public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Module,
    string Resource,
    string Action,
    PermissionKind Kind,
    bool IsBuiltin);

public enum PermissionKind
{
    Page,
    Menu,
    Button,
    Api,
    Data
}
```

### 8.2 表结构

新增或调整：

```text
sys_permission
sys_role
sys_user_role
sys_role_permission
sys_menu
sys_role_menu
sys_permission_endpoint
sys_permission_button
sys_permission_data_scope
```

### 8.3 URL 权限

新增：

```text
sys_permission_endpoint
  id
  permission_id
  http_method
  route_pattern
  created_at
```

Endpoint 注册时：

```csharp
.RequirePermission(UserPermissions.Create)
.BindEndpointPermission("POST", "/api/v1/identity/users");
```

OpenAPI 输出：

```json
"x-wecms-permission": "identity:user:create"
```

### 8.4 按钮权限

新增按钮权限定义：

```csharp
public static class UserButtonPermissions
{
    public const string Create = "identity:user:button:create";
    public const string Disable = "identity:user:button:disable";
}
```

前端返回：

```json
{
  "permissions": [
    "identity:user:list",
    "identity:user:button:create"
  ]
}
```

---

## 9. 数据权限机制

### 9.1 数据权限类型

```csharp
public enum DataScopeType
{
    All,
    Self,
    Department,
    DepartmentAndChildren,
    CustomDepartments
}
```

### 9.2 数据权限上下文

```csharp
public sealed record DataScopeContext(
    long UserId,
    bool IsAllDataAllowed,
    IReadOnlySet<long> AllowedUserIds,
    IReadOnlySet<long> AllowedDepartmentIds);
```

### 9.3 数据权限服务

```csharp
public interface IDataScopeProvider
{
    Task<DataScopeContext> GetAsync(long userId, CancellationToken cancellationToken);
}
```

第一阶段只实现系统基础数据权限：

```text
本人
本部门
本部门及以下
自定义部门
全部
```

CMS 站点/栏目/内容权限暂不实现。

---

## 10. 统一缓存抽象

### 10.1 项目

新增：

```text
WeCms.Caching
```

### 10.2 接口

```csharp
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken);
}
```

### 10.3 缓存 Key 规范

```text
wecms:{env}:{tenant}:{module}:{resource}:{id}
```

示例：

```text
wecms:dev:t0:access:profile:u1
wecms:dev:t0:menu:tree:u1
wecms:dev:t0:config:settings
wecms:dev:t0:i18n:zh-CN
```

### 10.4 首批缓存对象

```text
用户权限 Profile
菜单树
系统设置
字典
I18n 消息
安全配置
```

---

## 11. AOP 事务与缓存

### 11.1 本次允许引入动态代理 AOP

因为本次明确摒弃旧 AOT 限制，允许引入：

```text
Autofac
Autofac.Extras.DynamicProxy
Castle.DynamicProxy
```

但限定：

```text
只拦截 Application Service 接口
不拦截 Repository
不拦截 Endpoint Handler
不拦截 Domain Entity
```

### 11.2 特性

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class UnitOfWorkAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheableAttribute : Attribute
{
    public string Prefix { get; }
    public int ExpirationSeconds { get; init; } = 300;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheEvictAttribute : Attribute
{
    public string Prefix { get; }
}
```

### 11.3 事务拦截器要求

```text
必须支持 async
异常必须 rollback
异常必须重新 throw
禁止 Task.Wait / .Result / Task.WaitAll
支持嵌套事务策略
支持 CancellationToken
```

### 11.4 缓存拦截器要求

```text
只缓存查询方法
不缓存 command 方法
Key 必须包含 tenant/user/scope
支持空值缓存
支持前缀清理
缓存命中/未命中可观测
```

---

## 12. 审计与日志

### 12.1 模块

新增：

```text
WeCms.Modules.Audit
WeCms.Modules.Audit.SqlSugar
```

### 12.2 审计接口

```csharp
public interface IAuditWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
}

public interface IExceptionAuditWriter
{
    Task WriteAsync(ExceptionAuditRecord record, CancellationToken cancellationToken);
}

public interface ISqlAuditWriter
{
    Task WriteAsync(SqlAuditRecord record, CancellationToken cancellationToken);
}
```

### 12.3 审计表

```text
sys_audit_log
sys_exception_log
sys_sql_log
sys_login_log
```

### 12.4 操作审计

Endpoint 元数据：

```csharp
.Audit("identity", "user", "create")
```

AOP 或 Endpoint Filter 根据元数据写审计：

```text
module
resource
action
target_id
actor_user_id
actor_username
method
path
ip
user_agent
trace_id
result
detail
created_at
```

### 12.5 异常审计

ExceptionMiddleware 捕获异常后：

```text
写 logger
写 exception audit
返回 ApiResult
```

敏感信息禁止返回前端。

### 12.6 SQL 审计

见第 7.5 节。

---

## 13. 后台基础设施

### 13.1 Swagger / Scalar

规则：

```text
开发环境启用 Swagger/Scalar UI
CI 导出 OpenAPI JSON
所有 Endpoint 必须出现在 OpenAPI
所有业务 Endpoint 必须带权限 metadata
```

禁止：

```text
不要为了 Swagger 引入 Controller
不要使用 Controller Attribute Routing
```

### 13.2 MiniProfiler

新增：

```text
MiniProfiler
SQL timing integration
HTTP timing integration
```

仅开发环境默认启用。

### 13.3 限流

现有限流保留并增强：

```text
AuthLogin
AuthRefresh
AuthTwoFactor
AdminWrite
FileUpload
SecurityUnban
```

新增：

```text
PermissionWrite
ConfigWrite
AuditQuery
FileDownload
```

拒绝时必须写安全事件。

### 13.4 国际化

Configuration 模块统一管理：

```text
I18nMessage
Locale
UserLocale
PublicMessages
```

加入缓存：

```text
wecms:{env}:{tenant}:i18n:{locale}
```

---

## 14. EventBus + Outbox

### 14.1 项目

新增：

```text
WeCms.EventBus
```

### 14.2 接口

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}

public interface IEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

### 14.3 Outbox

表：

```text
sys_outbox_message
  id
  event_type
  aggregate_type
  aggregate_id
  payload_json
  status
  retry_count
  available_at
  locked_at
  processed_at
  created_at
```

### 14.4 本次系统基础事件

由于暂不实现 CMS 内容模块，本次只做系统基础事件：

```text
UserCreatedEvent
UserDisabledEvent
RolePermissionsChangedEvent
MenuChangedEvent
SettingChangedEvent
DictChangedEvent
I18nChangedEvent
SecurityBanCreatedEvent
```

CMS 内容发布事件后续再做。

---

## 15. 敏捷 Sprint 计划

## Sprint 0：规则破坏性升级

目标：解除旧 AOT 限制，但明确继续 Minimal API，不引入 Controller。

任务：

1. 新增 ADR：`001X-minimal-api-remains-controller-forbidden.md`
2. 新增 ADR：`001X-system-module-split.md`
3. 新增 ADR：`001X-sqlsugar-data-platform.md`
4. 修改 `AGENTS.md`
5. 修改 `code_review.md`
6. 修改 architecture tests
7. 修改 quality gate scripts

验收：

```text
AGENTS 明确禁止 Controller
AGENTS 允许 Autofac / DynamicProxy AOP
AGENTS 允许 CodeFirst 建模
code_review 不再禁止 runtime code generation 的受控场景
code_review 仍禁止 Controller
架构测试体现新模块依赖矩阵
```

---

## Sprint 1：创建新项目结构

目标：创建新模块和平台项目，暂不迁移业务。

任务：

1. 创建 `WeCms.Data.SqlSugar`
2. 创建 `WeCms.Caching`
3. 创建 `WeCms.EventBus`
4. 创建 `WeCms.Aop`
5. 创建 8 个系统基础模块
6. 创建 7 个 `.SqlSugar` 适配模块
7. 更新 slnx / solution
8. 更新 Directory.Build.props
9. 添加 AssemblyMarker

测试：

```text
dotnet restore
dotnet build
LayerDependencyTests
```

---

## Sprint 2：迁移 Identity

目标：迁移 Auth / Users / TwoFactor。

小任务：

1. 移动 Auth DTO / Records / Service / Endpoints
2. 移动 Users DTO / Records / Service / Endpoints
3. 移动 TwoFactor
4. 移动 Repository 接口
5. 移动 Repository 实现到 `Identity.SqlSugar`
6. 更新 DI 注册
7. 更新 Endpoint 注册
8. 更新 OpenAPI
9. 更新权限 seed
10. 更新测试命名空间

验收：

```text
登录通过
刷新 token 通过
Me 接口通过
用户 CRUD 通过
2FA 相关测试通过
无 WeCms.Modules.System.Auth 引用
```

---

## Sprint 3：迁移 AccessControl

目标：迁移 Roles / Permissions / Menus。

小任务：

1. 移动 Role
2. 移动 Permission
3. 移动 Menu
4. 建立 PermissionDefinition
5. 建立 URL 权限绑定
6. 建立按钮权限模型
7. 更新 PermissionEndpointFilter
8. 更新 AccessProfileService
9. 更新权限版本服务
10. 更新 seed

验收：

```text
角色 CRUD 通过
权限 CRUD 通过
菜单 CRUD 通过
权限检查通过
OpenAPI x-wecms-permission 正确
写接口权限覆盖检查通过
```

---

## Sprint 4：迁移 Organization

目标：迁移 Departments / Posts，并把 Posts 改名 Positions。

小任务：

1. `Posts` 重命名为 `Positions`
2. `sys_post` 重命名为 `sys_position`
3. `sys_user_post` 重命名为 `sys_user_position`
4. 迁移 Departments
5. 迁移 Position
6. 建立 OrganizationLookupService
7. Identity 调用 Organization 抽象校验部门/岗位

验收：

```text
部门接口通过
岗位接口通过
用户创建时部门/岗位校验通过
代码中无 sys_post / PostService / IPostRepository
```

---

## Sprint 5：迁移 Configuration

目标：迁移 Settings / Dicts / I18n。

小任务：

1. 移动 Settings
2. 移动 Dicts
3. 移动 I18n
4. 建立 ConfigCacheInvalidator
5. 建立 I18n 缓存
6. 建立 Setting 安全规则
7. 更新 endpoint / seed / tests

验收：

```text
设置接口通过
字典接口通过
i18n 接口通过
缓存失效测试通过
```

---

## Sprint 6：迁移 Audit / Security / FileCenter / Platform

目标：拆完剩余模块。

任务：

1. Logs -> Audit
2. Security -> Security
3. Files -> FileCenter
4. System -> Platform
5. FileStorage 实现继续留在 Infrastructure
6. Security 事件写入抽象化
7. Audit 查询统一化
8. Platform 健康检查更新

验收：

```text
审计日志接口通过
登录日志接口通过
安全事件接口通过
文件接口通过
健康检查通过
无 WeCms.Modules.System 引用
```

---

## Sprint 7：拆分 Persistence

目标：删除旧 `WeCms.Persistence`。

任务：

1. SqlSugar 平台能力迁移到 `WeCms.Data.SqlSugar`
2. Identity repository 实现迁移到 `Identity.SqlSugar`
3. AccessControl repository 实现迁移到 `AccessControl.SqlSugar`
4. Organization repository 实现迁移到 `Organization.SqlSugar`
5. Configuration repository 实现迁移到 `Configuration.SqlSugar`
6. Audit repository 实现迁移到 `Audit.SqlSugar`
7. Security repository 实现迁移到 `Security.SqlSugar`
8. FileCenter repository 实现迁移到 `FileCenter.SqlSugar`
9. 删除旧 `WeCms.Persistence`
10. 更新 DB boundary tests

验收：

```text
无 WeCms.Persistence 项目
无 WeCms.Persistence namespace
SqlSugar 只出现在 WeCms.Data.SqlSugar 和 *.SqlSugar
模块层无 SQL
```

---

## Sprint 8：SqlSugar 数据平台

目标：实现 CodeFirst / Migration / QueryFilter / 多库 / SQL 审计。

任务：

1. EntityBase
2. CodeFirstRunner
3. SchemaValidator
4. MigrationScaffold
5. QueryFilterRegistrar
6. SoftDeleteFilter
7. TenantFilter
8. DataScopeFilter
9. MultiConnectionOptions
10. TenantConnectionResolver
11. SqlAuditRegistrar
12. SqlAuditRedactor

验收：

```text
CodeFirst validate 通过
Migration smoke 通过
QueryFilter 测试通过
多连接解析测试通过
SQL 审计测试通过
```

---

## Sprint 9：缓存 + AOP

目标：统一缓存和事务拦截。

任务：

1. ICache
2. MemoryCacheProvider
3. RedisCacheProvider 预留
4. CacheKeyBuilder
5. UnitOfWorkAttribute
6. CacheableAttribute
7. CacheEvictAttribute
8. TransactionInterceptor
9. CacheInterceptor
10. Autofac 注册

验收：

```text
事务拦截成功提交
异常自动 rollback
缓存命中/失效测试通过
禁止同步阻塞
```

---

## Sprint 10：EventBus + Outbox

目标：系统基础事件可异步处理。

任务：

1. IIntegrationEvent
2. IEventBus
3. IEventHandler<T>
4. OutboxWriter
5. OutboxDispatcher
6. Outbox migration
7. UserCreatedEvent
8. RolePermissionsChangedEvent
9. SettingChangedEvent
10. Handler 幂等

验收：

```text
事件写入 Outbox
Dispatcher 可处理事件
失败可重试
重复事件幂等
```

---

## Sprint 11：OpenAPI / Swagger / MiniProfiler

目标：增强后台基础设施，不引入 Controller。

任务：

1. 引入 Swagger/Scalar UI
2. 基于 Endpoint Metadata 生成 OpenAPI
3. 保留 CI OpenAPI export
4. 自动输出权限/审计/限流 metadata
5. 引入 MiniProfiler
6. SQL timing 接入 MiniProfiler
7. 更新 quality gate

验收：

```text
Swagger/Scalar 开发环境可访问
OpenAPI 导出通过
x-wecms-permission 正确
MiniProfiler 可看到 HTTP/SQL timing
无 AddControllers/MapControllers
```

---

## 16. TDD 要求

每个 Sprint 必须遵循：

```text
Red -> Green -> Refactor
```

### 16.1 单元测试

覆盖：

```text
Service 业务规则
PermissionChecker
DataScopeProvider
CacheKeyBuilder
AOP Interceptor
AuditWriter
EventBus Handler
```

### 16.2 集成测试

覆盖：

```text
Repository SQL
Migration / Seed
QueryFilter
多连接
Outbox
Endpoint
文件上传
权限拒绝
```

### 16.3 架构测试

覆盖：

```text
无 Controller
无 AddControllers
无 MapControllers
模块依赖矩阵
DB boundary
DI boundary
Endpoint permission coverage
Endpoint audit coverage
SqlSugar boundary
No System God Module
```

---

## 17. Codex 执行批次

### Batch 0：只读扫描

目标：确认当前文件、引用和测试状态。

输出：

```text
当前项目引用图
当前 System 子目录清单
当前 Persistence repository 清单
当前 endpoint 清单
当前权限码清单
当前 migration/seed 清单
```

禁止修改文件。

---

### Batch 1：规则破坏性升级

修改：

```text
AGENTS.md
code_review.md
docs/adr/*
backend/tests/WeCms.Tests.Architecture/*
scripts/checks/*
```

必须明确：

```text
继续 Minimal API
不引入 Controller
允许 Autofac/DynamicProxy AOP
允许 CodeFirst 建模
允许 Swagger/Scalar
允许 MiniProfiler
禁止业务层 SQL
禁止 SELECT *
禁止 SQL 拼接用户输入
```

---

### Batch 2：创建新项目骨架

创建新项目，不迁移业务逻辑。

验收：

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj
```

---

### Batch 3：迁移 Identity

只迁移 Identity，不同时迁移其他模块。

验收：

```bash
dotnet test backend/tests/WeCms.Tests.Unit --filter Identity
dotnet test backend/tests/WeCms.Tests.Integration --filter Auth
```

---

### Batch 4：迁移 AccessControl

只迁移权限/角色/菜单。

验收：

```bash
dotnet test backend/tests/WeCms.Tests.Unit --filter AccessControl
dotnet test backend/tests/WeCms.Tests.Integration --filter Permission
```

---

### Batch 5：迁移 Organization / Configuration

迁移组织和配置模块。

---

### Batch 6：迁移 Audit / Security / FileCenter / Platform

迁移剩余系统基础模块。

---

### Batch 7：删除旧 System / Persistence

删除旧项目和 namespace，更新所有引用。

验收：

```text
rg "WeCms.Modules.System" backend/src 返回空
rg "WeCms.Persistence" backend/src 返回空
```

---

### Batch 8：SqlSugar 数据平台

实现 CodeFirst / Migration / QueryFilter / 多库 / SQL 审计。

---

### Batch 9：缓存 + AOP

实现缓存和 AOP 拦截。

---

### Batch 10：EventBus + Outbox

实现基础事件总线。

---

### Batch 11：Swagger / MiniProfiler / OpenAPI 元数据

增强基础设施。

---

## 18. Definition of Done

每个 Batch 必须满足：

```text
[ ] 未引入 Controller / ControllerBase
[ ] 未调用 AddControllers / MapControllers
[ ] Endpoint 仍为 Minimal API
[ ] 新增 Endpoint 已绑定权限或 AllowAnonymous/InternalOnly
[ ] 写操作 Endpoint 已绑定审计 metadata
[ ] 高风险写操作已绑定限流
[ ] Service 只依赖接口
[ ] Repository 实现只在 *.SqlSugar
[ ] SqlSugar 只在 Data.SqlSugar 和 *.SqlSugar
[ ] 模块层无 SQL 文本
[ ] 无 SELECT *
[ ] 无 SQL 拼接用户输入
[ ] 事务支持 async 且异常重新 throw
[ ] 缓存 Key 包含租户/模块/资源维度
[ ] SQL 日志脱敏
[ ] EventBus Handler 幂等
[ ] 单元测试通过
[ ] 集成测试通过
[ ] 架构测试通过
[ ] OpenAPI 导出通过
[ ] quality gate 通过或明确列出暂时失败项和原因
```

---

## 19. 最终目标状态

完成后，系统基础模块应达到：

```text
HTTP 层：
  Minimal API
  EndpointDefinition
  Endpoint Metadata
  Endpoint Filter
  Swagger/Scalar
  OpenAPI export

业务层：
  Identity
  AccessControl
  Organization
  Configuration
  Audit
  Security
  FileCenter
  Platform

数据层：
  Data.SqlSugar
  *.SqlSugar adapters
  CodeFirst models
  Migration baseline
  QueryFilter
  Multi-connection
  SQL audit

横切能力：
  DI/IOC
  AOP transaction
  AOP cache
  Unified cache
  Audit writer
  EventBus + Outbox

治理：
  Architecture tests
  DB boundary tests
  DI boundary tests
  Permission coverage
  Audit coverage
  OpenAPI coverage
  Quality gate
```

---

## 20. 最终结论

本次升级应明确采用：

> **Minimal API 继续作为唯一 HTTP API 入口，不引入 Controller Web API。**

同时执行：

> **拆分 System 大模块，拆分 Persistence 大模块，重置系统基础数据库 baseline，建立 SqlSugar 数据平台、权限平台、缓存、AOP、审计、EventBus 和后台基础设施。**

这不是从 0 重写项目，而是一次 **不兼容旧结构的系统基础平台重构**。

