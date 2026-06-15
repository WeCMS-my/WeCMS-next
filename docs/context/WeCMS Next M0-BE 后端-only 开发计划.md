# WeCMS Next M0-BE 后端-only 开发计划书 v1.0

## 0. 文档定位

文档类型：M0-BE 后端-only 开发执行计划
上级文档：WeCMS Next 完整迁移重构计划 v3.0
执行方式：推倒重建，从 0 开始开发
开发工具：Codex / Codex CLI / Codex App
后端技术栈：.NET 10 + ASP.NET Core Minimal APIs + SqlSugarCore + MySQL
编译模式：普通 JIT，不采用 Native AOT
前端策略：前端后移，M0-BE 不开发 SoybeanAdmin
旧系统策略：ThinkPHP 旧系统只作为业务和 Schema 参考，不迁移数据、不做兼容
目标状态：建立稳定、清晰、可测试、可扩展的后端工程底座

---

# 1. M0-BE 总目标

M0-BE 的核心目标不是完成完整 CMS，而是建立一个可信的后端底座。

M0-BE 需要完成：

```text id="thejn4"
1. 从 0 初始化后端工程结构。
2. 建立模块化单体 + Clean Architecture 风格分层。
3. 建立 WeCms.Persistence 独立数据库操作层。
4. 接入 SqlSugarCore + MySQL。
5. 建立 Migration / Seed 机制。
6. 建立统一响应、异常、TraceId。
7. 实现最小 Auth 闭环。
8. 实现 Refresh Token rotation。
9. 实现权限元数据与 secure-ping 权限检查。
10. 实现基础 System API。
11. 实现 OpenAPI export。
12. 实现 backend-only quality gate。
13. 实现 GitHub Actions 后端质量门禁。
14. 输出旧系统 reference report，确认不迁移旧数据。
```

M0-BE 不做：

```text id="4z5w6w"
1. 不开发 frontend/**。
2. 不运行 pnpm。
3. 不生成前端 TypeScript generated。
4. 不接入 SoybeanAdmin。
5. 不实现用户/角色/菜单完整 CRUD。
6. 不实现 CMS 栏目/文章/媒体。
7. 不实现文件上传完整系统。
8. 不实现 AI runtime。
9. 不实现多租户。
10. 不实现插件系统。
11. 不兼容旧 ThinkPHP 登录。
12. 不迁移旧系统数据。
```

---

# 2. M0-BE 技术定版

## 2.1 后端技术栈

```text id="5h4oub"
.NET 10
ASP.NET Core Minimal APIs
SqlSugarCore
MySQL
System.Text.Json
OpenAPI
JWT Bearer
PBKDF2 password hashing
模块化单体
Clean Architecture 风格分层
接口抽象
构造函数注入
普通 JIT 发布
```

## 2.2 明确不采用

```text id="3k8zz8"
Native AOT
Dapper
Dapper.AOT
EF Core
MVC Controller
Razor Pages
运行时动态 DLL 插件
动态代理 AOP
旧系统兼容模式
```

---

# 3. M0-BE 工程结构

## 3.1 Solution 结构

```text id="ocvtiw"
backend/
  WeCms.slnx

  src/
    WeCms.Api/
    WeCms.Shared/
    WeCms.Infrastructure/
    WeCms.Persistence/
    WeCms.Modules.System/
    WeCms.Modules.Cms/

  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
    WeCms.Tests.Architecture/
```

## 3.2 项目职责

| 项目                         | 职责                                                                 |
| -------------------------- | ------------------------------------------------------------------ |
| `WeCms.Api`                | API Host、DI Composition Root、Middleware、OpenAPI export、Endpoint 注册 |
| `WeCms.Shared`             | 公共契约、接口、错误码、异常、时间/ID/安全抽象                                          |
| `WeCms.Infrastructure`     | 非数据库基础设施实现，例如 Token、密码、时间、ID                                       |
| `WeCms.Persistence`        | 唯一数据库操作层，SqlSugar、Repository 实现、Migration、Seed                     |
| `WeCms.Modules.System`     | 系统模块业务逻辑、Auth、权限、系统探针、Repository 接口                                |
| `WeCms.Modules.Cms`        | CMS 模块空壳或最小占位，M0-BE 不实现业务                                          |
| `WeCms.Tests.Unit`         | 单元测试                                                               |
| `WeCms.Tests.Integration`  | 集成测试                                                               |
| `WeCms.Tests.Architecture` | 架构边界测试                                                             |

---

# 4. 项目依赖矩阵

## 4.1 允许依赖

```text id="sfaq4o"
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

## 4.2 禁止依赖

```text id="7x6tfr"
WeCms.Modules.System -> WeCms.Persistence
WeCms.Modules.Cms -> WeCms.Persistence
WeCms.Modules.* -> SqlSugarCore
WeCms.Modules.* -> MySqlConnector
WeCms.Modules.* -> ORM Client
WeCms.Modules.* -> SQL 字符串
WeCms.Infrastructure -> WeCms.Persistence
WeCms.Shared -> 任意生产项目
```

---

# 5. 数据库边界规则

## 5.1 唯一数据库访问层

所有数据库访问只能在：

```text id="f3mcw8"
WeCms.Persistence
```

只有 `WeCms.Persistence` 可以：

```text id="0rmcf4"
引用 SqlSugarCore
创建 SqlSugarClient / SqlSugarScope
定义数据库 Entity
执行 Queryable / Insertable / Updateable / Deleteable
执行 SQL
执行 Migration
执行 Seed
管理事务
访问数据库连接串
```

## 5.2 模块层禁止事项

`WeCms.Modules.*` 禁止出现：

```text id="brifw1"
SqlSugarCore
SqlSugarClient
SqlSugarScope
Queryable
Insertable
Updateable
Deleteable
DbConnection
DbTransaction
MySqlConnection
SELECT
INSERT
UPDATE
DELETE
SQL 字符串
```

## 5.3 Repository 规则

Repository interface 放模块层：

```text id="qlh7i4"
WeCms.Modules.System/Auth/IAuthRepository.cs
WeCms.Modules.System/Users/IUserRepository.cs
WeCms.Modules.System/Roles/IRoleRepository.cs
WeCms.Modules.System/Menus/IMenuRepository.cs
```

Repository implementation 放 Persistence：

```text id="42t476"
WeCms.Persistence/Modules/System/Auth/AuthRepository.cs
WeCms.Persistence/Modules/System/Users/UserRepository.cs
WeCms.Persistence/Modules/System/Roles/RoleRepository.cs
WeCms.Persistence/Modules/System/Menus/MenuRepository.cs
```

Repository 返回：

```text id="q11mn3"
Row record
DTO projection
强类型结果
```

禁止返回：

```text id="ux1dmy"
dynamic
DataTable
object
Dictionary<string, object>
SqlSugar Entity 到业务层
```

---

# 6. DI 与接口规则

## 6.1 必须接口化的依赖

所有有副作用服务必须通过接口 + DI 注入：

```text id="e82kog"
Repository
UnitOfWork
Clock
IdGenerator
PasswordHasher
TokenService
RefreshTokenHasher
TokenGenerator
PermissionChecker
FileStorage
EmailSender
CacheService
CurrentUserAccessor
AuditContextAccessor
外部 HTTP Client
```

## 6.2 禁止业务代码直接 new

业务代码禁止：

```text id="kb9g1l"
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

允许：

```text id="3iwijb"
new DTO
new record
new ValueObject
new List
new Dictionary
new ApiResult
new 局部无副作用对象
```

---

# 7. M0-BE 里程碑拆分

M0-BE 拆分为 13 个子任务：

```text id="cmy3ly"
M0-BE-000：清理旧实现与重建准备
M0-BE-001：初始化 solution 与项目结构
M0-BE-002：建立依赖矩阵与架构测试
M0-BE-003：接入 SqlSugar Persistence
M0-BE-004：实现数据库 Migration / Seed
M0-BE-005：统一响应、异常、TraceId
M0-BE-006：实现最小 Auth
M0-BE-007：实现 Refresh Token rotation
M0-BE-008：实现权限元数据与 secure-ping
M0-BE-009：实现 System API
M0-BE-010：实现 OpenAPI export 与契约检查
M0-BE-011：实现质量门禁脚本
M0-BE-012：实现 GitHub Actions CI
M0-BE-013：旧系统 Reference Report 与最终验收
```

---

# 8. M0-BE-000：清理旧实现与重建准备

## 目标

确认项目推倒重建，从 0 开始。

## 允许保留

```text id="voz2qg"
docs/
AGENTS.md
code_review.md
README.md
database/legacy-reference/
已有业务分析文档
```

## 必须删除或废弃

```text id="5f14q5"
旧 Dapper 实现
旧 Dapper.AOT 配置
旧 Native AOT 配置
旧临时 Persistence 重复实现
旧不稳定 quality gate
旧 OpenAPI 产物
旧不一致测试
```

## 交付物

```text id="3rh1pj"
docs/adr/0008-rebuild-from-zero.md
docs/adr/0009-jit-sqlsugar-persistence.md
docs/adr/0010-frontend-deferred.md
```

## 验收标准

```text id="9bnr9t"
[ ] 明确记录从 0 重建决策
[ ] 明确记录取消 AOT
[ ] 明确记录取消 Dapper / Dapper.AOT
[ ] 明确记录 SqlSugar 只能在 Persistence
[ ] 明确记录前端后移
[ ] 明确记录旧系统不迁移
```

## Codex 任务提示

```text id="jlg4vb"
只更新文档和 ADR，不写代码。
```

---

# 9. M0-BE-001：初始化 solution 与项目结构

## 目标

从 0 创建后端工程骨架。

## 需要创建

```text id="vz4bwf"
backend/WeCms.slnx
backend/src/WeCms.Api
backend/src/WeCms.Shared
backend/src/WeCms.Infrastructure
backend/src/WeCms.Persistence
backend/src/WeCms.Modules.System
backend/src/WeCms.Modules.Cms
backend/tests/WeCms.Tests.Unit
backend/tests/WeCms.Tests.Integration
backend/tests/WeCms.Tests.Architecture
```

## 项目配置

所有项目：

```xml id="x6xlq6"
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

API 项目：

```text id="lqi34r"
Microsoft.NET.Sdk.Web
ASP.NET Core Minimal APIs
不启用 PublishAot
不启用 Native AOT
```

## 验收标准

```text id="3jp1ts"
[ ] backend/WeCms.slnx 存在
[ ] 6 个 src 项目存在
[ ] 3 个 tests 项目存在
[ ] dotnet restore 成功
[ ] dotnet build backend/WeCms.slnx -warnaserror 成功
```

## 验证命令

```bash id="6h4sob"
dotnet restore backend/WeCms.slnx
dotnet build backend/WeCms.slnx -warnaserror --nologo
```

---

# 10. M0-BE-002：建立依赖矩阵与架构测试

## 目标

用测试和脚本锁定项目边界，防止后续越界。

## 交付物

```text id="y0o7zf"
backend/tests/WeCms.Tests.Architecture/LayerDependencyTests.cs
backend/tests/WeCms.Tests.Architecture/PersistenceBoundaryTests.cs
backend/tests/WeCms.Tests.Architecture/DiBoundaryTests.cs
scripts/checks/check-layer-dependency.sh
scripts/checks/check-db-boundary.sh
scripts/checks/check-di-boundary.sh
```

## 测试规则

```text id="r98vji"
[ ] Modules 不引用 Persistence
[ ] Modules 不引用 SqlSugarCore
[ ] Modules 不包含 SQL 字符串
[ ] Modules 不出现 SqlSugarClient / SqlSugarScope
[ ] Modules 不出现 DbConnection / DbTransaction
[ ] Infrastructure 不引用 Persistence
[ ] Shared 不引用任何生产项目
[ ] 只有 Persistence 可以引用 SqlSugarCore
[ ] 业务类构造参数不得是具体 Repository 实现
[ ] 业务类不得 new 有副作用对象
```

## 验证命令

```bash id="mncos0"
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo
bash scripts/checks/check-layer-dependency.sh
bash scripts/checks/check-db-boundary.sh
bash scripts/checks/check-di-boundary.sh
```

---

# 11. M0-BE-003：接入 SqlSugar Persistence

## 目标

建立唯一数据库访问层 `WeCms.Persistence`。

## 需要引入

```xml id="57c6rt"
<PackageReference Include="SqlSugarCore" Version="5.1.4.214" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
```

## 需要创建

```text id="0y8wpx"
WeCms.Persistence/Data/SqlSugarClientFactory.cs
WeCms.Persistence/Data/SqlSugarUnitOfWork.cs
WeCms.Persistence/Data/SqlSugarTransactionContext.cs
WeCms.Persistence/Data/PersistenceServiceCollectionExtensions.cs
```

## DI 注册

```csharp id="emwx0z"
services.AddScoped<ISqlSugarClientFactory, SqlSugarClientFactory>();
services.AddScoped<ISqlSugarClient>(sp =>
    sp.GetRequiredService<ISqlSugarClientFactory>().Create());
services.AddScoped<IUnitOfWork, SqlSugarUnitOfWork>();
```

## 验收标准

```text id="4443k3"
[ ] 只有 WeCms.Persistence 引用 SqlSugarCore
[ ] WeCms.Api 通过 AddWeCmsPersistence() 注册数据库层
[ ] WeCms.Modules.* 不引用 SqlSugarCore
[ ] 数据库连接串缺失时 fail-fast
[ ] 数据库连接测试通过
```

---

# 12. M0-BE-004：数据库 Migration / Seed

## 目标

从空 MySQL 数据库生成 M0-BE 基础结构和种子数据。

## 表结构

```text id="2jds8g"
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

## Seed

```text id="xaqdz4"
sys:system:secure-ping
super_admin 角色
admin 管理员
super_admin 绑定所有权限
```

## 默认账号

```text id="nzf6ka"
username: admin
password: Admin@123
```

## 必须说明

```text id="tyr40t"
默认密码只用于开发和初始化
生产部署前必须强制修改
```

## 交付物

```text id="0fmfkp"
database/migrations/000001_init_identity.sql
database/migrations/000002_init_permission.sql
database/migrations/000003_init_auth_security.sql
database/seeds/000001_seed_base_permissions.sql
database/seeds/000002_seed_super_admin.sql
WeCms.Persistence/Migration/DbMigrationRunner.cs
WeCms.Persistence/Migration/SeedRunner.cs
```

## 验收标准

```text id="t4u5u0"
[ ] 空库可执行 migration
[ ] migration 可重复执行且幂等
[ ] seed 可重复执行且幂等
[ ] admin 用户密码 hash 入库
[ ] refresh token 表有 token_hash 唯一索引
```

---

# 13. M0-BE-005：统一响应、异常、TraceId

## 目标

统一后端 API 行为。

## 交付物

```text id="c6dc8d"
WeCms.Shared/ApiResult.cs
WeCms.Shared/ApiCodes.cs
WeCms.Shared/DomainException.cs
WeCms.Api/Middleware/RequestIdMiddleware.cs
WeCms.Api/Middleware/ExceptionMiddleware.cs
```

## ApiResult

```csharp id="16kajh"
public sealed record ApiResult<T>(
    int Code,
    string Msg,
    T? Data,
    string? TraceId = null,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null);
```

## HTTP 映射

```text id="zf2bsa"
Success -> 200
ValidationError -> 400
Unauthorized -> 401
Forbidden -> 403
NotFound -> 404
Conflict -> 409
TooManyRequests -> 429
BusinessError -> 400
SystemError -> 500
```

## 验收标准

```text id="0u33xu"
[ ] 所有异常响应包含 traceId
[ ] 未处理异常不返回 ex.Message
[ ] DomainException 按 code 映射 HTTP status
[ ] RequestIdMiddleware 写入 X-Trace-Id
```

---

# 14. M0-BE-006：实现最小 Auth

## API

```text id="0j8ajf"
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

## 交付物

```text id="56v7ad"
WeCms.Modules.System/Auth/AuthEndpoints.cs
WeCms.Modules.System/Auth/AuthService.cs
WeCms.Modules.System/Auth/AuthDtos.cs
WeCms.Modules.System/Auth/IAuthRepository.cs
WeCms.Persistence/Modules/System/Auth/AuthRepository.cs
```

## Login 要求

```text id="ouqkay"
[ ] username/password 不能为空
[ ] 登录失败不泄露用户是否存在
[ ] 登录失败写 login_log
[ ] 登录失败写 security_event
[ ] 登录成功生成 access token
[ ] 登录成功生成 refresh token
[ ] refresh token 只保存 hash
[ ] 登录成功更新 last_login_at / last_login_ip
```

## Me 要求

```text id="lx3scl"
[ ] 需要授权
[ ] 返回用户基础信息
[ ] 返回 roles
[ ] 返回 permissions
[ ] menus 可暂时返回空数组
```

---

# 15. M0-BE-007：Refresh Token rotation

## 目标

实现安全的 refresh token 轮换。

## 要求

```text id="4d6tv2"
[ ] refresh token 只存 hash
[ ] refresh 时创建新 token
[ ] refresh 时吊销旧 token
[ ] 创建新 token + 吊销旧 token 必须同一事务
[ ] 已吊销 token 被复用时吊销整个 family
[ ] 过期 token 返回 401
[ ] 用户禁用后 refresh 返回 401
[ ] 并发 refresh 只有一个成功
```

## 测试

```text id="ksh5cr"
AuthRefreshConcurrencyTests
RefreshTokenReuseTests
RefreshTokenExpiredTests
RefreshUserDisabledTests
```

## 验收标准

```text id="zwb8qt"
[ ] 并发 refresh 测试通过
[ ] token reuse 测试通过
[ ] refresh token 明文不会入库
[ ] 安全事件记录完整
```

---

# 16. M0-BE-008：权限元数据与 secure-ping

## 目标

建立 endpoint 权限模型。

## 交付物

```text id="ey20n1"
PermissionMetadata
RequirePermission
PermissionEndpointFilter
IPermissionChecker
SystemPermissions
PermissionMetadataScanTests
```

## 权限码

```text id="1ue8u4"
sys:system:secure-ping
```

## 行为

```text id="l8je5f"
未登录 -> 401
用户禁用 -> 401
无权限 -> 403
有权限 -> 放行
```

## 验收标准

```text id="lnknkj"
[ ] secure-ping 绑定 PermissionMetadata
[ ] secure-ping RequireAuthorization
[ ] 无权限返回 403
[ ] 未登录返回 401
[ ] 权限检查通过 Persistence 查询数据库
```

---

# 17. M0-BE-009：System API

## API

```text id="rvijqj"
GET /health/live
GET /health/ready
GET /api/v1/system/ping
GET /api/v1/system/version
GET /api/v1/system/db-check
GET /api/v1/system/secure-ping
```

## 要求

```text id="7z5f9i"
[ ] live 不依赖数据库
[ ] ready 可检查数据库连接
[ ] db-check 失败不返回 ex.Message
[ ] secure-ping 需要权限
[ ] 所有响应使用 ApiResult<T>
```

---

# 18. M0-BE-010：OpenAPI export 与契约检查

## 目标

生成稳定后端契约。

## 命令

```bash id="c5h9xo"
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

## 必须检查

```text id="mt5yx1"
[ ] POST /api/v1/auth/login 有 requestBody
[ ] POST /api/v1/auth/refresh 有 requestBody
[ ] POST /api/v1/auth/logout 有 requestBody
[ ] Auth response schema 存在
[ ] System API paths 存在
[ ] secure-ping security metadata 存在或可测试
```

## 脚本

```text id="dt8bxu"
scripts/checks/check-openapi-auth-request-body.sh
scripts/checks/check-openapi-endpoint-coverage.sh
```

---

# 19. M0-BE-011：质量门禁脚本

## 目标

建立后端-only 质量门禁。

## 脚本

```text id="zkd3vx"
scripts/quality-gate-backend.sh
```

## 检查项

```text id="smkv5y"
[1/12] dotnet restore
[2/12] dotnet build -warnaserror
[3/12] dotnet test
[4/12] dotnet publish -c Release --no-self-contained
[5/12] OpenAPI export
[6/12] OpenAPI auth request body check
[7/12] check-db-boundary
[8/12] check-layer-dependency
[9/12] check-di-boundary
[10/12] check-no-frontend-change
[11/12] check-code-review
[12/12] migration/seed smoke test
```

## 明确不包含

```text id="5ckxwf"
AOT publish
/p:PublishAot=true
Dapper baseline
Dapper.AOT check
IL2026 / IL3050 check
```

---

# 20. M0-BE-012：GitHub Actions CI

## 目标

CI 自动验证 M0-BE。

## Workflow

```text id="i2qbxr"
.github/workflows/backend-quality-gate.yml
```

## CI 流程

```text id="p7acui"
checkout
setup dotnet 10
start mysql 8
dotnet restore
bash scripts/quality-gate-backend.sh
```

## 验收标准

```text id="rpvdxq"
[ ] push main 触发
[ ] PR 触发
[ ] workflow_dispatch 支持手动触发
[ ] CI 成功生成 OpenAPI
[ ] CI 通过所有 backend gate
```

---

# 21. M0-BE-013：旧系统 Reference Report 与最终验收

## 目标

确认旧系统只参考，不迁移。

## 输出

```text id="2nlmwp"
artifacts/reports/legacy-reference-report.md
database/legacy-reference/thinkphp_schema_reference.sql
```

## 报告内容

```text id="fn7xq8"
旧 think_admin -> 新 sys_user 参考
旧 think_auth_group -> 新 sys_role 参考
旧 think_auth_rule -> 新 sys_menu + sys_permission 参考
旧 think_config -> 新 sys_setting 后续参考
明确无数据迁移
明确无兼容模式
明确无旧密码兼容
```

---

# 22. M0-BE 开发顺序

推荐严格按以下顺序开发：

```text id="jj8sln"
1. 文档 / ADR / 规则
2. Solution 初始化
3. 依赖矩阵测试
4. Persistence + SqlSugar
5. Migration / Seed
6. 统一响应 / 异常
7. Auth login
8. Refresh rotation
9. 权限元数据
10. System API
11. OpenAPI export
12. Quality gate
13. GitHub Actions
14. Legacy reference report
15. 只读审计
```

不要并行开发前端。

---

# 23. Codex 执行任务清单

## Codex-001：文档和 ADR

允许修改：

```text id="uw8ugj"
README.md
AGENTS.md
code_review.md
docs/adr/**
docs/context/**
```

禁止修改：

```text id="nar2zv"
backend/src/**
backend/tests/**
database/**
scripts/**
frontend/**
```

---

## Codex-002：初始化后端工程

允许修改：

```text id="c53sl3"
backend/**
```

禁止修改：

```text id="o3rzyf"
frontend/**
```

---

## Codex-003：架构测试与边界脚本

允许修改：

```text id="xcc83q"
backend/tests/WeCms.Tests.Architecture/**
scripts/checks/**
```

---

## Codex-004：SqlSugar Persistence

允许修改：

```text id="qji28w"
backend/src/WeCms.Persistence/**
backend/src/WeCms.Shared/Data/**
backend/src/WeCms.Api/Program.cs
```

---

## Codex-005：Migration / Seed

允许修改：

```text id="jyfhin"
database/migrations/**
database/seeds/**
backend/src/WeCms.Persistence/Migration/**
```

---

## Codex-006：响应和异常

允许修改：

```text id="3or47d"
backend/src/WeCms.Shared/**
backend/src/WeCms.Api/Middleware/**
```

---

## Codex-007：Auth

允许修改：

```text id="s84uxp"
backend/src/WeCms.Modules.System/Auth/**
backend/src/WeCms.Persistence/Modules/System/Auth/**
backend/tests/WeCms.Tests.Unit/Auth/**
backend/tests/WeCms.Tests.Integration/Auth/**
```

---

## Codex-008：权限

允许修改：

```text id="f6jqa3"
backend/src/WeCms.Modules.System/Permissions/**
backend/src/WeCms.Persistence/Modules/System/Permissions/**
backend/tests/WeCms.Tests.Architecture/**
```

---

## Codex-009：System API

允许修改：

```text id="ss894u"
backend/src/WeCms.Modules.System/System/**
backend/tests/**
```

---

## Codex-010：OpenAPI

允许修改：

```text id="tp5pew"
backend/src/WeCms.Api/Extensions/**
backend/src/WeCms.Modules.System/**/*
scripts/checks/check-openapi-*.sh
```

---

## Codex-011：Quality Gate

允许修改：

```text id="6pxfys"
scripts/**
.github/workflows/**
```

---

## Codex-012：最终只读审计

不修改文件，只输出报告。

---

# 24. 最终 M0-BE 验收清单

```text id="9uq4hy"
[ ] backend solution 从 0 build 成功
[ ] 所有项目依赖矩阵正确
[ ] SqlSugarCore 只存在于 WeCms.Persistence
[ ] 全仓无 Dapper
[ ] 全仓无 Dapper.AOT
[ ] 全仓无 PublishAot
[ ] Modules 无 SQL
[ ] Modules 无 ORM
[ ] Modules 无 Persistence 引用
[ ] Infrastructure 无数据库访问
[ ] Shared 无生产项目引用
[ ] Migration 可从空库执行
[ ] Seed 可从空库执行
[ ] admin 用户可登录
[ ] refresh token rotation 可用
[ ] 并发 refresh 只有一个成功
[ ] secure-ping 权限可用
[ ] db-check 不泄露异常
[ ] OpenAPI export 成功
[ ] Auth requestBody schema 存在
[ ] quality gate 本地通过
[ ] GitHub Actions 通过
[ ] frontend/** 无修改
[ ] legacy reference report 已输出
```

---

# 25. M0-BE 最终完成定义

M0-BE 完成后，项目应具备：

```text id="2a2yqs"
一个干净的 .NET 10 后端工程
一个独立的 SqlSugar Persistence 层
一个最小可用 Auth + 权限闭环
一个可从空库启动的数据库初始化流程
一个稳定 OpenAPI 契约导出流程
一套 backend-only quality gate
一套 GitHub Actions 后端 CI
明确的前端后移边界
明确的不迁移旧系统数据决策
```

M0-BE 完成后进入：

```text id="9yyf7k"
M1-BE：完整系统管理 API
```

不进入前端。
