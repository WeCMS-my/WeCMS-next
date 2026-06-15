# WeCMS Next M0-BE 后端-only 开发计划 v2.0

## 0. 文档定位

文档类型：M0-BE 后端-only 重建执行计划
适用阶段：M0-BE
技术路线：.NET 10 + ASP.NET Core Minimal APIs + SqlSugar + MySQL
编译模式：普通 JIT 发布，不再采用 Native AOT
前端范围：本阶段不操作 `frontend/**`
旧系统迁移策略：旧系统仅作为业务和 Schema 参考，不迁移旧数据，不做兼容模式
文档状态：Accepted Draft
推荐执行工具：Codex

---

# 1. M0-BE 最新定版结论

M0-BE 不再采用 Native AOT，不再使用 Dapper / Dapper.AOT。

新的 M0-BE 目标是：

```text
建立一个可运行、可测试、可生成 OpenAPI、具备最小 Auth 与权限闭环、数据库访问严格收敛到 Persistence 层的后端底座。
```

最终技术栈：

```text
.NET 10
ASP.NET Core Minimal APIs
普通 JIT 编译 / 发布
SqlSugarCore
MySQL
System.Text.Json
OpenAPI
模块化单体
Clean Architecture 风格分层
构造函数注入
接口抽象
后端-only
```

明确取消：

```text
Native AOT Only
PublishAot
Dapper
Dapper.AOT
Dapper AOT baseline
AOT exception baseline
IL2026 / IL3050 专项门禁
JsonContext AOT 强制门禁
AOT publish gate
```

---

# 2. 核心架构原则

## 2.1 后端-only

M0-BE 只做后端，不操作 SoybeanAdmin 前端。

禁止：

```text
frontend/**
pnpm install
pnpm build
pnpm typecheck
pnpm lint
frontend generated 类型
SoybeanAdmin request / route / store / view
```

前端开发进入条件：

```text
后端 Auth API 完成
用户 / 角色 / 菜单 / 权限 API 完成
系统基础 API 完成
CMS API 完成
OpenAPI 契约稳定
后端质量门禁通过
```

---

## 2.2 旧系统不迁移

旧 ThinkPHP 系统目前仅处于开发阶段，未真实使用。

因此：

```text
不迁移旧用户
不迁移旧角色
不迁移旧权限
不迁移旧菜单
不迁移旧配置
不迁移旧日志
不迁移旧文件
不兼容旧密码 hash
不实现 password_migrated_at 登录升级流程
不迁移旧 token / session / 2FA secret / backup code / SMTP 密码 / auth_key
不在运行时代码中加入 legacy 分支
```

旧系统只作为：

```text
业务理解参考
Schema 设计参考
权限模型参考
菜单模型参考
```

---

## 2.3 数据库访问边界

M0-BE 必须建立独立数据库操作层：

```text
WeCms.Persistence
```

所有数据库访问只能发生在 `WeCms.Persistence`。

禁止在以下项目中出现数据库操作：

```text
WeCms.Api
WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Infrastructure
WeCms.Shared
```

只有 `WeCms.Persistence` 可以引用：

```text
SqlSugarCore
MySQL 连接器相关能力
数据库连接配置
事务实现
Repository implementation
Migration Runner
Seed Runner
SQL / ORM 查询表达式
```

额外硬约束：

```text
Repository interface 只允许存在于模块层或 WeCms.Shared
Repository implementation 只允许存在于 WeCms.Persistence
Service / UseCase 只能通过接口 + DI 获取 Repository、UnitOfWork、Clock、Token、密码、随机数等有副作用依赖
WeCms.Api / WeCms.Infrastructure / WeCms.Shared 也不得持有 SQL 文本、ORM Client、数据库连接或 Repository implementation
```

---

## 2.4 DI 与接口抽象

所有有副作用服务必须通过接口 + DI 注入。

必须使用接口的对象：

```text
数据库 Repository
事务 UnitOfWork
时间 Clock
ID Generator
密码 Hasher
Token Service
Refresh Token Hasher
权限 Checker
文件存储
邮件发送
缓存
HTTP Client
外部服务 Client
```

业务代码禁止直接实例化：

```text
new Repository(...)
new SqlSugarClient(...)
new SqlSugarScope(...)
new MySqlConnection(...)
new JwtTokenService(...)
new Pbkdf2PasswordHasher(...)
new HttpClient(...)
new SmtpClient(...)
DateTime.UtcNow
Guid.NewGuid()
Random.Shared
```

允许直接 `new`：

```text
DTO
record
Value Object
Response
Command
Query
List
Dictionary
局部无副作用对象
```

---

# 3. 最新项目结构

```text
backend/
  WeCms.slnx

  src/
    WeCms.Api/
      Program.cs
      Middleware/
      Extensions/
      Json/

    WeCms.Shared/
      ApiResult.cs
      ApiCodes.cs
      DomainException.cs
      Data/
        IDbConnectionFactory.cs
        IUnitOfWork.cs
      Time/
        IClock.cs
      Id/
        IIdGenerator.cs
      Security/
        IPasswordHasher.cs
        ITokenService.cs
        IRefreshTokenHasher.cs
        IPermissionChecker.cs

    WeCms.Infrastructure/
      Security/
        JwtTokenService.cs
        Pbkdf2PasswordHasher.cs
        RefreshTokenHasher.cs
        CryptoTokenGenerator.cs
      Time/
        SystemClock.cs
      Id/
        SystemGuidIdGenerator.cs
      Extensions/
        InfrastructureServiceCollectionExtensions.cs

    WeCms.Persistence/
      WeCms.Persistence.csproj
      Data/
        SqlSugarClientFactory.cs
        SqlSugarUnitOfWork.cs
        SqlSugarTransactionFacade.cs
        PersistenceServiceCollectionExtensions.cs
      Migration/
        DbMigrationRunner.cs
      Modules/
        System/
          Auth/
            AuthRepository.cs
          Permissions/
            PermissionChecker.cs
        Cms/

    WeCms.Modules.System/
      Auth/
        AuthEndpoints.cs
        AuthService.cs
        AuthDtos.cs
        IAuthRepository.cs
      Permissions/
        PermissionEndpointFilter.cs
        PermissionEndpointExtensions.cs
      System/
        SystemEndpoints.cs
        SystemDtos.cs
      Json/
        WeCmsModulesSystemJsonContext.cs

    WeCms.Modules.Cms/

  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
    WeCms.Tests.Architecture/

database/
  migrations/
  seeds/
  legacy-migration/

scripts/
  quality-gate-backend.sh
  checks/
    check-db-boundary.sh
    check-layer-dependency.sh
    check-openapi-auth-request-body.sh
    check-no-select-star.sh
    check-no-dynamic-query.sh
    check-no-frontend-change.sh
    check-code-review.sh
    check-di-boundary.sh
```

---

# 4. 项目依赖矩阵

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Persistence
  -> WeCms.Shared

WeCms.Modules.System
  -> WeCms.Shared

WeCms.Modules.Cms
  -> WeCms.Shared

WeCms.Persistence
  -> WeCms.Shared
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> SqlSugarCore

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> no production project
```

禁止：

```text
WeCms.Modules.System -> WeCms.Persistence
WeCms.Modules.Cms -> WeCms.Persistence
WeCms.Modules.* -> SqlSugarCore
WeCms.Modules.* -> Dapper
WeCms.Modules.* -> MySqlConnector
WeCms.Infrastructure -> WeCms.Persistence
WeCms.Shared -> any production project
```

---

# 5. 需要从旧 M0-BE 计划中删除的内容

删除以下 Native AOT 相关内容：

```text
Native AOT Only
PublishAot
IsAotCompatible 作为硬约束
EnableTrimAnalyzer 作为硬约束
AOT publish gate
IL2026 / IL3050 检查
Dapper AOT exception baseline
AOT self-warning suppression check
AOT compatibility report
JsonContext 必须覆盖所有 DTO 的 AOT 强制要求
```

删除以下 Dapper 相关内容：

```text
Dapper
Dapper.AOT
DapperDataExtensions
DapperAotModule
CommandDefinition 作为推荐写法
QueryAsync / ExecuteAsync 作为数据访问方式
Dapper 版本 baseline
Dapper AOT ADR
```

---

# 6. 新增 SqlSugar 规则

## 6.1 SqlSugar 只能在 Persistence 层

只有以下项目可以引用 `SqlSugarCore`：

```text
WeCms.Persistence
```

禁止：

```text
WeCms.Api 引用 SqlSugarCore
WeCms.Modules.System 引用 SqlSugarCore
WeCms.Modules.Cms 引用 SqlSugarCore
WeCms.Infrastructure 引用 SqlSugarCore
WeCms.Shared 引用 SqlSugarCore
```

---

## 6.2 Repository 实现只能在 Persistence 层

模块层只保留接口：

```text
WeCms.Modules.System/Auth/IAuthRepository.cs
WeCms.Modules.System/Users/IUserRepository.cs
WeCms.Modules.System/Roles/IRoleRepository.cs
WeCms.Modules.System/Menus/IMenuRepository.cs
```

Persistence 层实现接口：

```text
WeCms.Persistence/Modules/System/Auth/AuthRepository.cs
WeCms.Persistence/Modules/System/Users/UserRepository.cs
WeCms.Persistence/Modules/System/Roles/RoleRepository.cs
WeCms.Persistence/Modules/System/Menus/MenuRepository.cs
```

---

## 6.3 事务边界

Service / UseCase 可以控制事务，但只能通过抽象：

```text
IUnitOfWork
```

业务层不得直接使用：

```text
SqlSugarClient
SqlSugarScope
DbConnection
DbTransaction
Ado
BeginTran
CommitTran
RollbackTran
```

---

## 6.4 SqlSugar 查询规范

在 Persistence 层可以使用：

```text
Queryable
Insertable
Updateable
Deleteable
Ado.SqlQuery
Ado.ExecuteCommand
```

但必须满足：

```text
返回强类型 DTO / Row record
不得返回 dynamic
不得返回 DataTable 到业务层
不得拼接用户输入 SQL
复杂查询必须有测试
分页查询必须限制 pageSize 最大值
写操作必须检查 affected rows
删除必须默认软删除
```

---

# 7. 推荐 SqlSugar 接入方式

## 7.1 WeCms.Persistence.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SqlSugarCore" Version="5.1.4.214" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WeCms.Shared\WeCms.Shared.csproj" />
    <ProjectReference Include="..\WeCms.Modules.System\WeCms.Modules.System.csproj" />
    <ProjectReference Include="..\WeCms.Modules.Cms\WeCms.Modules.Cms.csproj" />
  </ItemGroup>

</Project>
```

---

## 7.2 SqlSugar Client 工厂

```csharp
public interface ISqlSugarClientFactory
{
    ISqlSugarClient Create();
}
```

```csharp
public sealed class SqlSugarClientFactory : ISqlSugarClientFactory
{
    private readonly string _connectionString;

    public SqlSugarClientFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default 未配置");
    }

    public ISqlSugarClient Create()
    {
        return new SqlSugarScope(new ConnectionConfig
        {
            DbType = DbType.MySql,
            ConnectionString = _connectionString,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }
}
```

---

## 7.3 SqlSugar UnitOfWork

```csharp
public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    private readonly ISqlSugarClient _db;

    public SqlSugarUnitOfWork(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task BeginAsync(CancellationToken cancellationToken)
    {
        _db.Ado.BeginTran();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        _db.Ado.CommitTran();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        _db.Ado.RollbackTran();
        return Task.CompletedTask;
    }
}
```

实际实现时需要确认生命周期：

```text
ISqlSugarClient / SqlSugarScope 建议 Scoped
IUnitOfWork 建议 Scoped
Repository 建议 Scoped
```

---

# 8. AuthRepository 改造目标

## 旧 Dapper 风格删除

删除：

```text
using Dapper
CommandDefinition
QuerySingleAsync
QuerySingleOrDefaultAsync
ExecuteAsync
```

## 新 SqlSugar 风格示例

```csharp
public sealed class AuthRepository : IAuthRepository
{
    private readonly ISqlSugarClient _db;

    public AuthRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<UserRow?> GetUserByUsernameAsync(
        IDbTransactionFacade? transaction,
        string username,
        CancellationToken cancellationToken)
    {
        return await _db.Queryable<SysUserEntity>()
            .Where(x => x.Username == username && x.DeletedAt == null)
            .Select(x => new UserRow(
                x.Id,
                x.Username,
                x.DisplayName,
                x.PasswordHash,
                x.Status,
                x.SecurityStamp,
                x.PermissionVersion))
            .FirstAsync();
    }
}
```

---

# 9. Entity 规则

Persistence 层可以定义数据库实体：

```text
WeCms.Persistence/Entities/System/SysUserEntity.cs
WeCms.Persistence/Entities/System/SysRoleEntity.cs
WeCms.Persistence/Entities/System/SysPermissionEntity.cs
WeCms.Persistence/Entities/System/SysRefreshTokenEntity.cs
```

实体不得暴露到：

```text
WeCms.Api
WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Shared
```

Repository 返回模块层定义的 Row record / DTO，而不是返回 Entity。

---

# 10. OpenAPI 策略

因为不再采用 AOT，Auth endpoint 可以恢复强类型 handler，以便 OpenAPI 自动推断 requestBody。

推荐写法：

```csharp
group.MapPost("/login", LoginAsync)
    .AllowAnonymous()
    .WithName("Auth_Login")
    .Accepts<LoginRequest>("application/json")
    .Produces<ApiResult<LoginResponse>>(StatusCodes.Status200OK)
    .Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
```

即使 handler 使用强类型参数，也必须保留 `.Accepts<T>()`，避免 OpenAPI requestBody 丢失。

必须保留 OpenAPI 检查：

```text
POST /api/v1/auth/login 必须有 LoginRequest requestBody
POST /api/v1/auth/refresh 必须有 RefreshRequest requestBody
POST /api/v1/auth/logout 必须有 LogoutRequest requestBody
```

---

# 11. JsonSerializerContext 新规则

由于不再使用 AOT，JsonSerializerContext 不再作为硬性 AOT 门禁。

但为了稳定 JSON 输出和后续性能优化，可以保留为推荐项。

调整为：

```text
不再要求所有 DTO 必须进入 JsonContext 才能通过 AOT
但核心 API DTO 建议继续登记到 JsonContext
OpenAPI requestBody schema 检查仍然必须保留
```

删除：

```text
AOT JsonContext coverage hard gate
```

保留或弱化为：

```text
JsonContext coverage warning / optional check
```

---

# 12. 新质量门禁

旧 AOT gate 删除。

新的 M0-BE backend quality gate：

```text
[1/12] dotnet restore
[2/12] dotnet build backend/WeCms.slnx -warnaserror
[3/12] dotnet test backend/WeCms.slnx
[4/12] dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release --no-self-contained
[5/12] OpenAPI export
[6/12] OpenAPI auth request body check
[7/12] check-db-boundary
[8/12] check-layer-dependency
[9/12] check-no-frontend-change
[10/12] check-no-sql-in-modules
[11/12] check-di-boundary
[12/12] check-code-review
```

删除：

```text
AOT exception baseline check
AOT self-warning suppression check
Native AOT publish
Dapper version baseline check
Dapper.AOT check
```

---

# 13. GitHub Actions

CI 使用普通 JIT 发布：

```yaml
- name: Publish
  run: dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release --no-self-contained --nologo
```

不再需要：

```text
linux-x64 runtime-specific publish
/p:PublishAot=true
llvm / objcopy
AOT toolchain setup
AOT warning baseline
```

---

# 14. 数据库 Migration / Seed

M0-BE 仍保留：

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_permission
sys_refresh_token
sys_login_log
sys_security_event
sys_schema_migration
```

Seed 保留：

```text
base permissions
super_admin role
admin user
super_admin role bind all permissions
```

默认管理员：

```text
username: admin
password: Admin@123
```

生产前必须强制修改默认密码。

---

# 15. Auth 最小闭环

M0-BE 仍必须交付：

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

要求：

```text
Refresh Token 只存 hash
Refresh Token rotation 必须事务
已吊销 token 被复用时吊销整个 family
Access Token 不携带完整权限列表
登录失败写 login_log / security_event
认证失败返回 HTTP 401
无权限返回 HTTP 403
Validation 返回 HTTP 400
系统错误返回 HTTP 500
```

---

# 16. System API

M0-BE 保留：

```text
GET /health/live
GET /health/ready
GET /api/v1/system/ping
GET /api/v1/system/version
GET /api/v1/system/db-check
GET /api/v1/system/secure-ping
```

其中：

```text
secure-ping 必须 RequirePermission("sys:system:secure-ping")
db-check 失败不得返回 ex.Message
```

---

# 17. M0-BE 任务拆分

## M0-BE-001：重置技术约束

目标：

```text
移除 AOT 目标
移除 Dapper 目标
确定 SqlSugar + JIT
```

执行：

```text
删除 PublishAot
删除 IsAotCompatible 硬约束
删除 EnableTrimAnalyzer 硬约束
删除 AOT baseline 脚本
删除 Dapper / Dapper.AOT 规则
新增 SqlSugar ADR
更新 README / AGENTS / code_review
```

验收：

```text
全仓无 PublishAot
全仓无 Dapper.AOT
全仓无 /p:PublishAot=true
quality gate 不再运行 AOT publish
```

---

## M0-BE-002：建立 Persistence 层

目标：

```text
所有数据库操作进入 WeCms.Persistence
```

执行：

```text
新增 WeCms.Persistence 项目
Api 引用 Persistence
Modules 只引用 Shared
Infrastructure 不再包含 Data / Migration
Repository implementation 移入 Persistence
```

验收：

```text
Modules 不引用 Persistence
Modules 不引用 SqlSugar
Modules 不包含 SQL
Persistence 是唯一 ORM 层
```

---

## M0-BE-003：接入 SqlSugar

目标：

```text
在 Persistence 内部使用 SqlSugar 替代 Dapper
```

执行：

```text
WeCms.Persistence 引用 SqlSugarCore
删除 Dapper
删除 Dapper.AOT
删除 DapperDataExtensions
新增 SqlSugarPersistenceExtensions
新增 SqlSugarClientFactory
新增 SqlSugarUnitOfWork
新增 Entity mapping
```

验收：

```text
全仓无 Dapper 包引用
全仓无 Dapper.AOT 包引用
全仓无 CommandDefinition
全仓无 QueryAsync / ExecuteAsync
Persistence 中存在 SqlSugarCore
Modules 中不存在 SqlSugarCore
```

---

## M0-BE-004：实现 Shared 抽象

目标：

```text
业务层只依赖接口
```

抽象：

```text
IUnitOfWork
IDbTransactionFacade 或等价事务抽象
IAuthRepository
IPermissionChecker
IPasswordHasher
ITokenService
IRefreshTokenHasher
ITokenGenerator
IClock
IIdGenerator
```

验收：

```text
业务 Service 构造函数参数全部为接口
业务 Service 不直接 new 有副作用对象
```

---

## M0-BE-005：实现 Auth

目标：

```text
完成最小登录闭环
```

API：

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

验收：

```text
login 成功返回 accessToken + refreshToken
refresh 成功 rotation
logout 吊销 refresh token
me 返回 user / roles / permissions / menus
```

---

## M0-BE-006：实现权限元数据

目标：

```text
Endpoint permission metadata 可扫描、可验证、可执行
```

交付：

```text
PermissionMetadata
RequirePermission
PermissionEndpointFilter
IPermissionChecker
SystemPermissions
```

验收：

```text
secure-ping 需要 sys:system:secure-ping
无权限返回 403
未登录返回 401
权限检查只通过 Persistence 查询数据库
```

---

## M0-BE-007：实现 System API

目标：

```text
基础探针和系统信息 API 可用
```

API：

```text
/health/live
/health/ready
/api/v1/system/ping
/api/v1/system/version
/api/v1/system/db-check
/api/v1/system/secure-ping
```

---

## M0-BE-008：实现 Migration / Seed

目标：

```text
从空库生成 M0-BE 基础数据
```

交付：

```text
MigrationRunner
SeedRunner
base permissions
super_admin role
admin user
role-permission binding
```

---

## M0-BE-009：OpenAPI 导出

目标：

```text
生成稳定后端契约
```

命令：

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

必须检查：

```text
login requestBody
refresh requestBody
logout requestBody
response schema
stable server url
endpoint coverage
```

---

## M0-BE-010：质量门禁

目标：

```text
CI 可验证，后端-only 可控
```

脚本：

```text
scripts/quality-gate-backend.sh
scripts/checks/check-db-boundary.sh
scripts/checks/check-layer-dependency.sh
scripts/checks/check-openapi-auth-request-body.sh
scripts/checks/check-no-frontend-change.sh
scripts/checks/check-code-review.sh
scripts/review-di.sh
```

---

## M0-BE-011：ThinkPHP Spike

目标：

```text
确认旧系统只作为参考，不做迁移
```

输出：

```text
artifacts/reports/migration-spike-report.md
database/legacy-migration/m0_spike_users_roles_permissions.sql
```

---

## M0-BE-012：CI

目标：

```text
GitHub Actions 自动验证 M0-BE
```

CI 包含：

```text
restore
build
test
publish
OpenAPI export
OpenAPI auth request body check
DB boundary
DI boundary
Layer dependency
No frontend change
Code review rules
```

---

# 18. 最终验收清单

```text
[ ] backend/WeCms.slnx build 通过
[ ] dotnet test 通过
[ ] 普通 dotnet publish 通过
[ ] 不存在 PublishAot
[ ] 不存在 Dapper
[ ] 不存在 Dapper.AOT
[ ] WeCms.Persistence 引用 SqlSugarCore
[ ] WeCms.Modules.* 不引用 SqlSugarCore
[ ] WeCms.Modules.* 不包含 SQL
[ ] Repository implementation 全部在 Persistence
[ ] Repository interface 在模块层或 Shared
[ ] Auth login 可用
[ ] Auth refresh rotation 可用
[ ] Refresh Token 只存 hash
[ ] token reuse 吊销整个 family
[ ] logout 可用
[ ] me 可用
[ ] secure-ping 需要权限码
[ ] 未登录返回 401
[ ] 无权限返回 403
[ ] validation 返回 400
[ ] db-check 不返回 ex.Message
[ ] OpenAPI export 成功
[ ] Auth requestBody schema 存在
[ ] frontend/** 无改动
[ ] GitHub Actions 通过
```

---

# 19. Codex 执行 Prompt

```text
你是 WeCMS Next M0-BE 后端-only 重构 Agent。

本次目标：
将 M0-BE 技术路线从 Native AOT + Dapper/Dapper.AOT 调整为普通 .NET 10 JIT + SqlSugarCore。
同时保留后端-only、Persistence 独立持久化层、DI + 接口抽象、OpenAPI 契约优先、旧系统不迁移、前端后移等规则。

必须遵守：

1. 不修改 frontend/**。
2. 不运行 pnpm。
3. 不生成前端 TypeScript generated。
4. 删除 Native AOT 硬约束。
5. 删除 PublishAot / IsAotCompatible / EnableTrimAnalyzer 作为硬门禁。
6. 删除 /p:PublishAot=true。
7. 删除 Dapper / Dapper.AOT。
8. 删除所有 Dapper 相关 gate。
9. 新增或保留 WeCms.Persistence 作为唯一数据库操作层。
10. WeCms.Persistence 引用 SqlSugarCore。
11. WeCms.Modules.* 禁止引用 SqlSugarCore。
12. WeCms.Modules.* 禁止包含 SQL 字符串。
13. Repository interface 保留在模块层或 Shared。
14. Repository implementation 只能在 Persistence。
15. Service 只通过接口和 DI 获取依赖。
16. 不允许业务代码 new 数据库、ORM、Repository、Token、密码、时间、随机数等有副作用对象。
17. OpenAPI 必须包含 Auth requestBody schema。
18. 质量门禁必须包含 build、test、publish、OpenAPI export、DB boundary、DI boundary、layer dependency、no frontend change。

请按以下顺序执行：

Step 1：更新文档与 ADR
Step 2：移除 AOT 相关配置和脚本
Step 3：移除 Dapper / Dapper.AOT
Step 4：在 Persistence 中接入 SqlSugarCore
Step 5：重写 AuthRepository / PermissionChecker 为 SqlSugar 实现
Step 6：修正 UnitOfWork 为 SqlSugar 事务实现
Step 7：更新 DI 注册
Step 8：更新质量门禁
Step 9：运行 dotnet build / test / publish
Step 10：导出 OpenAPI 并检查 Auth requestBody schema

完成后输出：
- 修改文件清单
- 删除文件清单
- 新增文件清单
- 质量门禁结果
- 风险说明
```

---

# 20. 最终定版

M0-BE v2.0 的最终定版为：

```text
WeCMS Next M0-BE 不再采用 Native AOT，不再使用 Dapper / Dapper.AOT。
后端采用 .NET 10 JIT + ASP.NET Core Minimal APIs + SqlSugarCore + MySQL。
所有数据库访问必须集中在 WeCms.Persistence。
业务模块只依赖接口，不依赖数据库实现。
前端继续后移，旧系统不迁移，不做兼容模式。
```


