# WeCMS Next 完整迁移重构计划 v3.0

## 0. 文档定位

文档类型：WeCMS Next 从 0 重构主计划
适用项目：WeCMS Next
执行模式：推倒重建，从 0 开始开发
开发工具：Codex / Codex CLI / Codex App
后端技术栈：.NET 10 + ASP.NET Core Minimal APIs + SqlSugar + MySQL
前端技术栈：Vue3 + SoybeanAdmin，后移开发
编译模式：普通 JIT 编译，不采用 Native AOT
旧系统定位：只作为业务、Schema、权限模型参考，不做数据迁移
文档状态：重新定版
适用阶段：M0 → M6

---

# 1. 总体决策

## 1.1 项目推倒重建

WeCMS Next 不再基于当前已实现代码继续修补，而是重新从 0 开始设计和开发。

当前仓库中的既有实现只作为参考，不作为必须保留的基础。

允许：

```text
保留有价值的文档
保留已验证过的业务理解
保留数据库设计参考
保留部分测试思路
保留 OpenAPI 质量门禁思路
保留后端-only 阶段策略
```

不保留：

```text
旧工程结构包袱
旧 Repository 实现
旧 Dapper / Dapper.AOT 代码
旧 Native AOT 规则
旧临时兼容逻辑
旧不稳定质量门禁脚本
旧错误分层
旧重复 Persistence 实现
旧前后端联动假设
```

---

## 1.2 旧系统不迁移数据

ThinkPHP 旧系统当前处于开发阶段，未真实应用。

因此：

```text
不迁移旧用户
不迁移旧角色
不迁移旧权限
不迁移旧菜单
不迁移旧配置
不迁移旧日志
不迁移旧文件
不迁移旧 token
不迁移旧 session
不迁移旧 2FA secret
不迁移旧 SMTP 密码
不迁移旧 auth_key
不兼容旧密码 hash
不实现 password_migrated_at
不做 legacy runtime compatibility
```

旧系统只用于：

```text
业务模块识别
权限模型参考
菜单模型参考
数据库字段参考
后台功能边界参考
```

---

## 1.3 新系统从 0 初始化

新系统从空数据库开始，通过 seed 生成基础数据：

```text
基础权限码
super_admin 角色
admin 管理员用户
角色-权限绑定
系统基础配置
```

默认管理员：

```text
username: admin
password: Admin@123
```

生产前必须强制修改默认密码。

---

# 2. 最新技术路线

## 2.1 后端技术栈

```text
.NET 10
ASP.NET Core Minimal APIs
SqlSugarCore
MySQL
System.Text.Json
OpenAPI
JWT Bearer
模块化单体
Clean Architecture 风格分层
接口抽象
构造函数注入
普通 JIT 发布
```

---

## 2.2 明确取消的技术路线

```text
Native AOT
PublishAot
Dapper
Dapper.AOT
EF Core
MVC Controller
Razor Pages
运行时插件 DLL
动态代理 AOP
旧系统运行时兼容
```

---

## 2.3 为什么取消 Native AOT

WeCMS 是后台 CMS 系统，核心目标是：

```text
稳定开发
快速迭代
清晰架构
后台业务完整
权限安全
可维护性
可扩展性
前后端契约稳定
```

Native AOT 对 CMS 后台系统不是当前第一优先级。

取消 AOT 后可以降低：

```text
JSON source generator 复杂度
Minimal API metadata 限制
ORM 兼容风险
第三方库限制
调试复杂度
构建门禁复杂度
```

---

## 2.4 为什么改用 SqlSugar

SqlSugar 更适合当前 WeCMS 的后台管理系统场景：

```text
CRUD 开发效率更高
分页查询更方便
条件查询更直观
后台管理模块更快落地
实体映射更友好
复杂管理页面开发成本更低
```

但 SqlSugar 必须严格限制在：

```text
WeCms.Persistence
```

业务模块不得感知 SqlSugar。

---

# 3. 总体架构

## 3.1 架构模式

WeCMS Next 采用：

```text
模块化单体 + Clean Architecture 风格分层
```

不采用：

```text
传统三层架构
完整 DDD 战术建模
前后端混合架构
运行时动态插件架构
```

---

## 3.2 后端项目结构

```text
backend/
  WeCms.slnx

  src/
    WeCms.Api/
      Program.cs
      Middleware/
      Extensions/
      OpenApi/
      Json/

    WeCms.Shared/
      ApiResult.cs
      ApiCodes.cs
      DomainException.cs

      Data/
        IUnitOfWork.cs
        ITransactionContext.cs

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
        SqlSugarTransactionContext.cs
        PersistenceServiceCollectionExtensions.cs

      Entities/
        System/
          SysUserEntity.cs
          SysRoleEntity.cs
          SysPermissionEntity.cs
          SysRefreshTokenEntity.cs
          SysLoginLogEntity.cs
          SysSecurityEventEntity.cs

      Migration/
        DbMigrationRunner.cs
        SeedRunner.cs

      Modules/
        System/
          Auth/
            AuthRepository.cs
          Permissions/
            PermissionChecker.cs
          Users/
            UserRepository.cs
          Roles/
            RoleRepository.cs
          Menus/
            MenuRepository.cs

        Cms/
          Channels/
            ChannelRepository.cs
          Articles/
            ArticleRepository.cs
          Pages/
            PageRepository.cs
          Media/
            MediaRepository.cs

    WeCms.Modules.System/
      Auth/
        AuthEndpoints.cs
        AuthService.cs
        AuthDtos.cs
        IAuthRepository.cs

      Users/
        UserEndpoints.cs
        UserService.cs
        UserDtos.cs
        IUserRepository.cs

      Roles/
        RoleEndpoints.cs
        RoleService.cs
        RoleDtos.cs
        IRoleRepository.cs

      Menus/
        MenuEndpoints.cs
        MenuService.cs
        MenuDtos.cs
        IMenuRepository.cs

      Permissions/
        PermissionEndpointFilter.cs
        PermissionEndpointExtensions.cs
        SystemPermissions.cs

      System/
        SystemEndpoints.cs
        SystemDtos.cs

    WeCms.Modules.Cms/
      Channels/
      Articles/
      Pages/
      Media/
      Tags/

  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
    WeCms.Tests.Architecture/

database/
  migrations/
  seeds/
  legacy-reference/

scripts/
  quality-gate-backend.sh
  checks/
```

---

# 4. 项目依赖矩阵

## 4.1 允许依赖

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

---

## 4.2 禁止依赖

```text
WeCms.Modules.System -> WeCms.Persistence
WeCms.Modules.Cms -> WeCms.Persistence
WeCms.Modules.* -> SqlSugarCore
WeCms.Modules.* -> MySqlConnector
WeCms.Modules.* -> 数据库连接对象
WeCms.Modules.* -> ORM Client
WeCms.Infrastructure -> WeCms.Persistence
WeCms.Shared -> 任何生产工程
```

---

# 5. 数据库访问边界

## 5.1 唯一数据库访问层

所有数据库访问只能在：

```text
WeCms.Persistence
```

只有 `WeCms.Persistence` 可以：

```text
引用 SqlSugarCore
创建 SqlSugarClient / SqlSugarScope
定义数据库实体
执行 Queryable / Insertable / Updateable / Deleteable
执行事务
执行 migration
执行 seed
包含 SQL / ORM 查询表达式
访问 MySQL 连接串
```

---

## 5.2 模块层禁止事项

`WeCms.Modules.*` 禁止出现：

```text
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
SQL 字符串
SELECT
INSERT
UPDATE
DELETE
```

模块层只能依赖：

```text
Repository interface
IUnitOfWork
业务 DTO
业务 Service
权限常量
Shared 抽象
```

---

## 5.3 Repository 规则

Repository interface 放在模块层：

```text
WeCms.Modules.System/Auth/IAuthRepository.cs
WeCms.Modules.System/Users/IUserRepository.cs
WeCms.Modules.Cms/Articles/IArticleRepository.cs
```

Repository implementation 放在 Persistence：

```text
WeCms.Persistence/Modules/System/Auth/AuthRepository.cs
WeCms.Persistence/Modules/System/Users/UserRepository.cs
WeCms.Persistence/Modules/Cms/Articles/ArticleRepository.cs
```

Repository 返回：

```text
Row record
DTO projection
强类型结果
```

不得返回：

```text
dynamic
DataTable
SqlSugar Entity 到业务层
object
Dictionary<string, object>
```

---

# 6. DI 与接口规则

## 6.1 默认全部构造函数注入

所有有副作用依赖必须通过接口注入。

包括：

```text
Repository
UnitOfWork
Clock
IdGenerator
PasswordHasher
TokenService
RefreshTokenHasher
PermissionChecker
FileStorage
EmailSender
CacheService
CurrentUserAccessor
AuditContextAccessor
HttpClient typed client
```

---

## 6.2 业务代码禁止直接实例化

禁止：

```text
new AuthRepository(...)
new UserRepository(...)
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

```text
new DTO
new record
new ValueObject
new List
new Dictionary
new ApiResult
new 局部无副作用对象
```

---

# 7. M0-BE 阶段目标

M0-BE 只建立后端底座。

必须交付：

```text
后端 solution
项目分层
SqlSugar Persistence
MySQL 连接
Migration / Seed
统一响应结构
统一异常处理
统一 RequestId / TraceId
最小 Auth
Refresh Token rotation
权限元数据
System API
OpenAPI export
Backend quality gate
GitHub Actions CI
旧系统 reference report
```

不做：

```text
SoybeanAdmin 前端
用户管理完整页面
角色管理完整页面
菜单管理完整页面
CMS 内容管理页面
文件上传完整系统
AI 模块
多租户
插件系统
复杂工作流
```

---

# 8. M0-BE 任务拆分

## M0-BE-001：从 0 初始化工程

目标：

```text
重新创建 backend solution
建立基础项目结构
```

交付：

```text
backend/WeCms.slnx
WeCms.Api
WeCms.Shared
WeCms.Infrastructure
WeCms.Persistence
WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Tests.Unit
WeCms.Tests.Integration
WeCms.Tests.Architecture
```

验收：

```text
dotnet build backend/WeCms.slnx -warnaserror
```

---

## M0-BE-002：基础分层与依赖矩阵

目标：

```text
确定项目引用关系
建立架构测试
```

交付：

```text
LayerDependencyTests
PersistenceBoundaryTests
DI boundary scan
check-layer-dependency.sh
check-db-boundary.sh
```

验收：

```text
Modules 不引用 Persistence
Modules 不引用 SqlSugar
Infrastructure 不引用 Persistence
Shared 不引用任何生产工程
```

---

## M0-BE-003：接入 SqlSugar Persistence

目标：

```text
在 Persistence 层接入 SqlSugar
```

交付：

```text
SqlSugarCore package
SqlSugarClientFactory
SqlSugarUnitOfWork
SqlSugarTransactionContext
PersistenceServiceCollectionExtensions
```

验收：

```text
只有 WeCms.Persistence 引用 SqlSugarCore
Modules 中无数据库操作
SqlSugar 连接 MySQL 成功
```

---

## M0-BE-004：数据库 Schema 与 Seed

目标：

```text
从空库生成 M0 基础表和种子数据
```

表：

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

Seed：

```text
sys:system:secure-ping
super_admin
admin
super_admin 绑定全部权限
```

---

## M0-BE-005：统一响应与异常

目标：

```text
统一 API 响应结构和错误处理
```

交付：

```text
ApiResult<T>
ApiCodes
DomainException
ExceptionMiddleware
RequestIdMiddleware
```

HTTP 规则：

```text
ValidationError -> 400
Unauthorized -> 401
Forbidden -> 403
NotFound -> 404
Conflict -> 409
TooManyRequests -> 429
BusinessError -> 400
SystemError -> 500
```

---

## M0-BE-006：最小 Auth

API：

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

要求：

```text
密码使用 PBKDF2
Refresh Token 只存 hash
Refresh Token rotation 必须事务
已吊销 token 复用时吊销整个 family
登录失败写 login_log
安全事件写 security_event
HTTP 状态码语义正确
```

---

## M0-BE-007：权限元数据

交付：

```text
PermissionMetadata
RequirePermission extension
PermissionEndpointFilter
IPermissionChecker
SystemPermissions
```

要求：

```text
secure-ping 必须绑定 sys:system:secure-ping
未登录返回 401
无权限返回 403
权限检查由 Persistence 查询数据库
```

---

## M0-BE-008：System API

API：

```text
GET /health/live
GET /health/ready
GET /api/v1/system/ping
GET /api/v1/system/version
GET /api/v1/system/db-check
GET /api/v1/system/secure-ping
```

要求：

```text
db-check 失败不返回 ex.Message
secure-ping 需要权限
health/ready 可以检查数据库连接
```

---

## M0-BE-009：OpenAPI export

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
Auth requestBody 存在
Auth response schema 存在
所有 endpoint 覆盖
server url 稳定
```

---

## M0-BE-010：质量门禁

脚本：

```text
scripts/quality-gate-backend.sh
```

检查项：

```text
dotnet restore
dotnet build -warnaserror
dotnet test
dotnet publish -c Release --no-self-contained
OpenAPI export
OpenAPI auth request body check
DB boundary
Layer dependency
DI boundary
No frontend change
Code review rules
```

明确删除：

```text
Native AOT publish
AOT baseline
Dapper baseline
Dapper.AOT check
IL2026 / IL3050 gate
```

---

## M0-BE-011：GitHub Actions CI

Workflow：

```text
.github/workflows/backend-quality-gate.yml
```

CI 执行：

```text
checkout
setup .NET 10
start MySQL
restore
quality-gate-backend.sh all
```

---

## M0-BE-012：旧系统 Reference Report

输出：

```text
artifacts/reports/legacy-reference-report.md
database/legacy-reference/thinkphp_schema_reference.sql
```

要求：

```text
只做参考
不执行数据迁移
不做字段兼容
不做旧密码兼容
```

---

# 9. M1-BE 后续范围

M0-BE 完成后进入 M1-BE。

M1-BE 做完整系统管理 API：

```text
用户管理
角色管理
菜单管理
权限管理
部门管理
岗位管理
字典管理
系统配置
登录日志
操作日志
安全事件
文件基础能力
```

仍不做前端。

---

# 10. M2-BE CMS API

M2-BE 做 CMS 内容 API：

```text
站点
栏目
文章
单页
媒体
标签
链接
内容版本
发布记录
回收站
SEO 设置
```

---

# 11. M3-FE 前端开发

只有后端 API 稳定后，才进入前端。

前端范围：

```text
SoybeanAdmin 初始化
登录页
Token refresh
动态菜单
权限按钮
用户管理页面
角色管理页面
菜单管理页面
CMS 内容页面
文件管理页面
```

---

# 12. 最终验收标准

M0-BE 完成必须满足：

```text
[ ] 从 0 新工程可 build
[ ] 从 0 空库可 migration
[ ] 从 0 可 seed admin
[ ] SqlSugar 只存在于 Persistence
[ ] Modules 无 SQL / ORM
[ ] 所有业务服务通过接口 + DI
[ ] Auth login 可用
[ ] Refresh rotation 事务可用
[ ] 权限过滤可用
[ ] System API 可用
[ ] OpenAPI export 成功
[ ] Auth requestBody schema 存在
[ ] backend quality gate 通过
[ ] GitHub Actions 通过
[ ] frontend/** 无修改
```

---

# 13. Codex 执行原则

使用 Codex 开发，不使用 Trae。

Codex 每轮只做一个小任务。

禁止给 Codex：

```text
一次性完成整个 M0-BE
```

推荐顺序：

```text
001 初始化文档和 ADR
002 初始化 solution 和项目结构
003 建立依赖矩阵测试
004 接入 SqlSugar Persistence
005 实现 Migration / Seed
006 实现统一响应和异常
007 实现 Auth
008 实现权限元数据
009 实现 System API
010 实现 OpenAPI export
011 实现质量门禁
012 只读复审
```

---

# 14. Codex Prompt 模板

```text
你是 WeCMS Next 后端工程开发 Agent。

当前任务：
<填写单个任务名称>

项目决策：
1. 项目推倒重建，从 0 开始。
2. 使用 Codex 开发，不使用 Trae。
3. 后端使用 .NET 10 + ASP.NET Core Minimal APIs + SqlSugar + MySQL。
4. 不采用 Native AOT。
5. 不使用 Dapper / Dapper.AOT。
6. 数据库访问只能在 WeCms.Persistence。
7. WeCms.Modules.* 不得引用 SqlSugar。
8. 业务代码必须通过接口 + DI。
9. 不修改 frontend/**。
10. 旧系统不迁移，不兼容，只参考。

本轮只允许修改：
<填写允许修改路径>

本轮禁止修改：
<填写禁止修改路径>

必须完成：
<填写任务要求>

验证命令：
<填写命令>

输出：
1. 修改文件清单
2. 新增文件清单
3. 删除文件清单
4. 验证结果
5. 风险说明
6. 下一轮建议任务
```

---

# 15. 最终定版

WeCMS Next 重新定版为：

```text
从 0 开始重建
.NET 10
ASP.NET Core Minimal APIs
SqlSugarCore
MySQL
普通 JIT 发布
模块化单体
Clean Architecture 风格分层
Persistence 独立数据库层
业务模块接口 + DI
前端后移
旧系统不迁移
不做兼容模式
不采用 Native AOT
不使用 Dapper / Dapper.AOT
```

M0-BE 的核心目标不是完成所有 CMS 功能，而是建立一个稳定、清晰、可测试、可扩展的后端底座。
