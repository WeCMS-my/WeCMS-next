# WeCMS Next M0-BE 后端-only 开发计划

> 文档类型：后端-only M0 重建执行计划  
> 适用阶段：M0-BE  
> 推荐执行工具：Trae IDE  
> 技术栈：ASP.NET Core Minimal APIs / .NET 10 / Native AOT Only / Dapper / Dapper.AOT / MySQL  
> 前端范围：本阶段不操作 `frontend/soybean-admin`  
> 文档版本：v1.0  
> 生成日期：2026-06-10  

---

## 0. 核心结论

WeCMS Next 当前 M0 阶段建议收缩为 **M0-BE：后端-only 工程底座重建**。

本阶段不再操作 SoybeanAdmin 前端代码，不初始化前端、不修改前端 request、不生成前端 TypeScript generated、不运行 pnpm 命令。

M0-BE 只交付一个可信后端底座：

```text
.NET 10 Minimal APIs
+ Native AOT publish
+ Dapper.AOT
+ MySQL
+ ApiResult / PagedResult / ApiCodes
+ JsonSerializerContext
+ Health / System API
+ Auth 最小闭环
+ Permission Metadata
+ OpenAPI 后端契约输出
+ ThinkPHP 迁移 Spike
+ Backend-only Quality Gate
```

M0-BE 完成后，再进入：

```text
M0.5-FE：SoybeanAdmin 接入验证
M1：完整认证安全闭环
M2：用户、角色、菜单、权限正式业务模块
M3：系统基础模块
M4：CMS 内容模块
```

---

## 1. 设计依据

本计划依据以下项目文档重新收敛 M0 范围：

```text
AGENTS.md
code_review.md
docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md
docs/context/WeCMS_工程骨架验证文档.md
docs/context/WeCMS_工程落地执行计划与交付工件.md
docs/context/WeCMS_ThinkPHP_系统详细说明文档.md
```

核心设计原则：

```text
1. 后端契约优先。
2. Native AOT 从第一天进入门禁。
3. Dapper.AOT 从第一天进入数据访问规范。
4. M0 只验证底座，不堆业务功能。
5. 前端 SoybeanAdmin 不参与 M0-BE。
6. OpenAPI 作为后端契约产物保留。
7. 权限元数据必须在 M0-BE 建立。
8. ThinkPHP 迁移只做 Spike，不做完整迁移。
```

---

## 2. M0-BE 阶段边界

### 2.1 M0-BE 只做

| 范围 | 是否做 | 说明 |
|---|---:|---|
| 后端 solution | 是 | 创建 `backend/WeCms.sln` |
| .NET 10 Minimal API | 是 | 只使用 Minimal APIs |
| Native AOT publish | 是 | 必须真实通过 |
| Dapper / Dapper.AOT | 是 | 禁止 EF Core |
| MySQL docker-compose | 是 | 提供本地开发数据库 |
| database migration / seed | 是 | 最小 M0 表和 super admin seed |
| ApiResult / PagedResult / ApiCodes | 是 | 后端统一契约 |
| ExceptionMiddleware / RequestIdMiddleware | 是 | 统一错误和 trace |
| JsonSerializerContext | 是 | 当前所有 DTO 必须覆盖 |
| Health / System endpoints | 是 | `live / ready / ping / version / db-check` |
| Auth 最小闭环 | 是 | `login / refresh / logout / me` |
| PermissionMetadata / RequirePermission | 是 | 权限元数据底座 |
| Endpoint 权限扫描测试 | 是 | 无权限码 endpoint 必须被扫描出来 |
| OpenAPI JSON 输出 | 是 | 输出到 `artifacts/openapi/wecms-api-v1.json` |
| ThinkPHP 迁移 Spike | 是 | 只输出报告，不迁移全量 |
| Backend-only quality gate | 是 | 不包含前端命令 |

---

### 2.2 M0-BE 明确不做

```text
不操作 frontend/soybean-admin
不初始化 SoybeanAdmin
不修改前端 request 封装
不生成 frontend/src/service/generated
不运行 pnpm install / typecheck / lint / build
不做前端登录页
不做 Dashboard
不做动态路由
不做按钮权限
不做完整 User / Role / Menu 页面
不做完整 File / Log / Setting / Dict 页面
不做 CMS 栏目 / 文章 / 媒体
不做完整 2FA
不做验证码
不做忘记密码
不做完整 WAF
不一次性迁移全部旧数据
```

---

## 3. M0-BE 总体架构

### 3.1 后端运行链路

```text
HTTP Client / API Tester
  ↓
ASP.NET Core Minimal APIs
  ↓
Endpoint Handler
  ↓
Application Service
  ↓
Repository
  ↓
Dapper / Dapper.AOT
  ↓
MySQL
```

### 3.2 项目结构

```text
backend/
  WeCms.sln

  src/
    WeCms.Api/
      Program.cs
      appsettings.json
      appsettings.Development.json
      Json/
        WeCmsJsonContext.cs
      Middleware/
        ExceptionMiddleware.cs
        RequestIdMiddleware.cs
      Filters/
        PermissionEndpointFilter.cs
      Extensions/
        ServiceCollectionExtensions.cs
        EndpointRouteBuilderExtensions.cs
        OpenApiExtensions.cs

    WeCms.Shared/
      ApiResult.cs
      PagedResult.cs
      ApiCodes.cs
      Permissions.cs
      CurrentUser.cs
      DomainException.cs
      ValidationError.cs
      Pagination/
      Security/
      Time/

    WeCms.Infrastructure/
      Data/
        IDbConnectionFactory.cs
        DbConnectionFactory.cs
        IUnitOfWork.cs
        UnitOfWork.cs
        DapperAotModule.cs
      Security/
        IPasswordHasher.cs
        Pbkdf2PasswordHasher.cs
        ITokenService.cs
        JwtTokenService.cs
        IRefreshTokenHasher.cs
        RefreshTokenHasher.cs
      Time/
        IClock.cs
        SystemClock.cs
      OpenApi/
        OpenApiExporter.cs

    WeCms.Modules.System/
      System/
        SystemEndpoints.cs
        SystemDtos.cs
      Auth/
        AuthEndpoints.cs
        AuthService.cs
        AuthRepository.cs
        AuthDtos.cs
        AuthValidators.cs
      Permissions/
        PermissionMetadata.cs
        PermissionEndpointExtensions.cs
        PermissionEndpointFilter.cs
        PermissionChecker.cs
        PermissionRepository.cs
        SystemPermissions.cs

    WeCms.Modules.Cms/
      ModuleMarker.cs

  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
    WeCms.Tests.Architecture/
```

### 3.3 数据库目录

```text
database/
  migrations/
    000001_init_m0_identity_tables.sql
    000002_init_m0_permission_tables.sql
    000003_init_m0_auth_security_tables.sql
    000004_seed_m0_base_permissions.sql
    000005_seed_m0_super_admin.sql

  seeds/
    seed-dev.sql

  legacy-migration/
    m0_spike_users_roles_permissions.sql
    migration-spike-report-template.md
```

### 3.4 脚本目录

```text
scripts/
  quality-gate-backend.sh
  db/
    reset-dev-db.sh
  smoke-admin-login.sh
  checks/
    check-no-select-star.sh
    check-no-dynamic-query.sh
    check-endpoint-permissions.sh
    check-json-context-coverage.sh
    check-no-frontend-change.sh
  openapi/
    export-openapi.sh
```

### 3.5 artifacts 目录

```text
artifacts/
  openapi/
    wecms-api-v1.json
  reports/
    migration-spike-report.md
    aot-publish-report.md
```

---

## 4. 后端依赖矩阵

M0-BE 必须遵守以下依赖方向：

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Modules.System
  -> WeCms.Shared

WeCms.Modules.Cms
  -> WeCms.Shared

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> 不引用其它生产工程
```

禁止：

```text
WeCms.Shared -> 其它生产工程
WeCms.Infrastructure -> WeCms.Api
WeCms.Infrastructure -> WeCms.Modules.System
WeCms.Infrastructure -> WeCms.Modules.Cms
WeCms.Modules.System -> WeCms.Modules.Cms 内部实现
WeCms.Modules.Cms -> WeCms.Modules.System 内部实现
```

---

## 5. M0-BE 后端技术红线

### 5.1 AOT 红线

```text
禁止 MVC Controller
禁止 Razor / Razor Pages
禁止 Session
禁止 runtime Endpoint 自动扫描
禁止 runtime code generation
禁止动态代理 AOP
禁止 Newtonsoft.Json 进入核心业务路径
禁止 DTO 未加入 JsonSerializerContext
禁止引入未验证 AOT 的第三方库
```

### 5.2 Dapper / SQL 红线

```text
禁止 EF Core
禁止 Query<dynamic>
禁止 dynamic 返回
禁止 SELECT *
禁止拼接用户输入 SQL
禁止前端传任意排序字段
禁止无 WHERE 的 UPDATE / DELETE
禁止写操作不检查 affected rows
禁止 Repository 不接收 CancellationToken
禁止 Service 直接写 SQL
```

### 5.3 安全红线

```text
禁止 Refresh Token 明文入库
禁止 password/token/secret/2FA 写日志
禁止 Access Token 携带完整权限列表
禁止未登录业务 Endpoint 返回 200 假成功
禁止无权限业务 Endpoint 返回 200 假成功
禁止业务 Endpoint 无权限码
禁止一期实现 AI runtime
禁止创建 WeCms.Modules.Ai
```

### 5.4 前端红线

```text
M0-BE 禁止修改 frontend/**
M0-BE 禁止运行 pnpm
M0-BE 禁止生成 frontend generated 类型
M0-BE 禁止改 SoybeanAdmin request / route / store / view
```

---

## 6. M0-BE API 范围

### 6.1 Health / System API

| Method | Path | 权限 | 说明 |
|---|---|---|---|
| GET | `/health/live` | Anonymous | 进程存活 |
| GET | `/health/ready` | Anonymous | 数据库 ready |
| GET | `/api/v1/system/ping` | Anonymous | API smoke |
| GET | `/api/v1/system/version` | Anonymous | 版本信息 |
| GET | `/api/v1/system/db-check` | Anonymous | MySQL 连接检查 |
| GET | `/api/v1/system/secure-ping` | `sys:system:secure-ping` | 权限过滤器验证 |

### 6.2 Auth API

| Method | Path | 权限 | 审计 | 限流 | 说明 |
|---|---|---|---|---|---|
| POST | `/api/v1/auth/login` | Anonymous | 是 | 是 | 登录 |
| POST | `/api/v1/auth/refresh` | Anonymous | 是 | 是 | 刷新 Token |
| POST | `/api/v1/auth/logout` | Authenticated | 是 | 是 | 退出 |
| GET | `/api/v1/auth/me` | Authenticated | 否 | 是 | 当前用户 |

---

## 7. 统一响应模型

### 7.1 成功响应

```json
{
  "code": 0,
  "msg": "success",
  "data": {},
  "traceId": "00-..."
}
```

### 7.2 失败响应

```json
{
  "code": 1001,
  "msg": "参数验证失败",
  "data": null,
  "traceId": "00-...",
  "fieldErrors": {
    "username": ["用户名不能为空"]
  }
}
```

### 7.3 C# 基线

```csharp
public sealed record ApiResult<T>(
    int Code,
    string Msg,
    T? Data,
    string? TraceId = null,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static ApiResult<T> Ok(T data, string? traceId = null)
        => new(0, "success", data, traceId);

    public static ApiResult<T> Fail(
        int code,
        string msg,
        string? traceId = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
        => new(code, msg, default, traceId, fieldErrors);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Records,
    int Page,
    int PageSize,
    long Total);
```

### 7.4 ApiCodes

```csharp
public static class ApiCodes
{
    public const int Success = 0;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int TooManyRequests = 429;
    public const int ValidationError = 1001;
    public const int BusinessError = 2001;
    public const int SystemError = 5000;
}
```

---

## 8. JsonSerializerContext 规则

所有 M0-BE 当前 DTO 必须进入 `WeCmsJsonContext`。

### 8.1 必须覆盖的类型

```text
ApiResult<string>
ApiResult<object>
ApiResult<SystemPingResponse>
ApiResult<SystemVersionResponse>
ApiResult<DbCheckResponse>
ApiResult<LoginResponse>
ApiResult<RefreshResponse>
ApiResult<CurrentUserResponse>
ApiResult<HealthLiveResponse>
ApiResult<HealthReadyResponse>
LoginRequest
RefreshRequest
LoginResponse
RefreshResponse
CurrentUserResponse
SystemPingResponse
SystemVersionResponse
DbCheckResponse
HealthLiveResponse
HealthReadyResponse
```

### 8.2 验收

```text
AOT publish 不出现 JSON metadata 缺失
check-json-context-coverage.sh 通过
```

---

## 9. 数据库设计

### 9.1 M0-BE 最小表

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_permission
sys_role_menu
sys_refresh_token
sys_user_session
sys_login_log
sys_security_event
sys_schema_migration
```

### 9.2 通用字段

核心业务表默认包含：

```text
id
created_at
created_by
updated_at
updated_by
deleted_at
deleted_by
row_version
legacy_id
```

### 9.3 `sys_user`

```text
id
legacy_id
username
display_name
email
phone
avatar_file_id
password_hash
password_hash_algorithm
password_migrated_at
status
security_stamp
permission_version
two_factor_enabled
two_factor_rebind_required
last_login_at
last_login_ip
created_at
created_by
updated_at
updated_by
deleted_at
deleted_by
row_version
```

### 9.4 `sys_refresh_token`

```text
id
user_id
token_hash
family_id
expires_at
revoked_at
replaced_by_token_id
created_ip
user_agent
created_at
```

### 9.5 `sys_permission`

```text
id
legacy_id
code
name
module
resource
action
http_method
route_pattern
status
is_system
created_at
updated_at
```

---

## 10. Auth 最小闭环设计

### 10.1 Login

```http
POST /api/v1/auth/login
```

请求：

```json
{
  "username": "admin",
  "password": "******"
}
```

响应：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "accessToken": "...",
    "refreshToken": "...",
    "expiresIn": 900
  }
}
```

规则：

```text
1. 用户名和密码必须后端校验。
2. 登录失败不得泄露账号是否存在。
3. 登录失败写 sys_security_event。
4. 登录成功写 sys_login_log。
5. Access Token 不携带完整权限列表。
6. Refresh Token 只保存 hash。
```

### 10.2 Refresh

```http
POST /api/v1/auth/refresh
```

规则：

```text
1. 查询 token_hash。
2. 检查 revoked_at。
3. 检查 expires_at。
4. 检查用户 status。
5. 在同一事务内吊销旧 token。
6. 生成新 token pair。
7. 保存新 refresh token hash。
8. 若发现已吊销 token 被复用，吊销整个 family。
```

### 10.3 Logout

```http
POST /api/v1/auth/logout
```

规则：

```text
1. 必须认证。
2. 吊销当前 refresh token。
3. 写安全事件或审计日志。
```

### 10.4 Me

```http
GET /api/v1/auth/me
```

响应：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "id": 1,
      "username": "admin",
      "displayName": "超级管理员"
    },
    "roles": ["super_admin"],
    "permissions": ["sys:system:secure-ping"],
    "menus": []
  }
}
```

M0-BE 中 `menus` 可以为空数组，后续 M0.5-FE / M2 再接动态路由。

---

## 11. 权限元数据设计

### 11.1 PermissionMetadata

```csharp
public sealed record PermissionMetadata(string Code);
```

### 11.2 RequirePermission

```csharp
public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return builder
            .RequireAuthorization()
            .WithMetadata(new PermissionMetadata(permissionCode))
            .AddEndpointFilter<PermissionEndpointFilter>();
    }
}
```

### 11.3 PermissionEndpointFilter

必须满足：

```text
未登录 -> HTTP 401
无权限 -> HTTP 403
有权限 -> 执行 next
用户被禁用 -> HTTP 401
permission_version 过期 -> HTTP 401
```

### 11.4 权限码常量

```csharp
public static class SystemPermissions
{
    public const string SystemSecurePing = "sys:system:secure-ping";
}
```

禁止：

```text
.RequirePermission("sys:system:secure-ping")
```

必须：

```text
.RequirePermission(SystemPermissions.SystemSecurePing)
```

---

## 12. OpenAPI 后端契约输出

M0-BE 只做后端 OpenAPI 输出，不生成前端类型。

### 12.1 产物路径

```text
artifacts/openapi/wecms-api-v1.json
```

### 12.2 命令

```bash
dotnet run --project backend/src/WeCms.Api \
  -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

### 12.3 验收

```text
OpenAPI JSON 能生成
包含 health/system/auth/secure-ping endpoints
包含 request / response DTO schema
不依赖前端
不修改 frontend
```

---

## 13. ThinkPHP 迁移 Spike

### 13.1 目标

M0-BE 不做完整迁移，只验证旧系统关键模型能否映射到新系统。

### 13.2 旧表范围

```text
think_admin
think_auth_group
think_auth_group_access
think_auth_rule
think_config
```

### 13.3 新表范围

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_menu
sys_role_permission
sys_setting
```

### 13.4 输出报告

```text
artifacts/reports/migration-spike-report.md
```

报告必须包含：

```text
旧用户数量
新用户数量
旧角色数量
新角色数量
旧规则数量
新菜单数量
新权限数量
角色权限关系数量
异常记录
需要人工处理的数据
```

---

## 14. M0-BE 开发阶段

### M0-BE-001：创建 backend solution 和项目结构

#### 目标

创建干净后端 solution。

#### 允许修改

```text
backend/**
docs/**
```

#### 禁止修改

```text
frontend/**
```

#### 验收

```bash
dotnet build backend/WeCms.sln -warnaserror
```

---

### M0-BE-002：配置 .NET 10 Native AOT

#### 目标

配置 Native AOT、Trim Analyzer、WarningsAsErrors。

#### 验收

```bash
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  /p:PublishAot=true
```

---

### M0-BE-003：实现 Shared 契约层

#### 目标

实现统一响应、分页、错误码、异常、权限常量。

#### 验收

```bash
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj
```

---

### M0-BE-004：实现 Middleware

#### 目标

实现 RequestId 和 ExceptionMiddleware。

#### 验收

```text
500 响应不泄露堆栈
400/401/403/404/409/429/500 结构统一
响应包含 traceId
```

---

### M0-BE-005：实现 Infrastructure Data / Dapper.AOT

#### 目标

实现 MySQL 连接、UnitOfWork、Dapper.AOT 配置。

#### 验收

```text
无 Query<dynamic>
无 SELECT *
Repository 方法带 CancellationToken
AOT publish 通过
```

---

### M0-BE-006：实现 database migration 和 seed

#### 目标

从空库初始化 M0 最小表和 super admin。

#### 验收

```bash
docker compose up -d mysql
bash scripts/db/reset-dev-db.sh
dotnet run --project backend/src/WeCms.Api --launch-profile http
```

开发环境的 schema、基础权限和 `admin / Admin@123` 账号由 `DbMigrationRunner` 在应用启动时统一创建。
主初始化流程不得手工执行 `database/seeds/*.sql` 写入 admin 密码。

---

### M0-BE-007：实现 System endpoints

#### 目标

实现 health、ping、version、db-check、secure-ping。

#### 验收

```bash
curl http://localhost:5207/health/live
curl http://localhost:5207/health/ready
curl http://localhost:5207/api/v1/system/ping
curl http://localhost:5207/api/v1/system/version
curl http://localhost:5207/api/v1/system/db-check
```

---

### M0-BE-008：实现 Auth 最小闭环

#### 目标

实现 login / refresh / logout / me。

#### 验收

```text
登录成功返回 accessToken / refreshToken
Refresh Token 只保存 hash
refresh 后旧 token 失效
logout 后 refresh token 失效
/auth/me 返回 user / roles / permissions / menus
scripts/smoke-admin-login.sh 可验证初始化后 admin / Admin@123 真实登录
```

---

### M0-BE-009：实现权限元数据和权限扫描

#### 目标

实现 RequirePermission 和 Endpoint 权限扫描测试。

#### 验收

```text
未登录访问 secure-ping -> HTTP 401
无权限访问 secure-ping -> HTTP 403
有权限访问 secure-ping -> HTTP 200
业务 Endpoint 缺权限元数据 -> 测试失败
```

---

### M0-BE-010：实现 OpenAPI 导出

#### 目标

输出后端契约 JSON。

#### 验收

```bash
dotnet run --project backend/src/WeCms.Api \
  -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

---

### M0-BE-011：实现 ThinkPHP 迁移 Spike

#### 目标

生成 migration-spike-report。

#### 验收

```text
artifacts/reports/migration-spike-report.md 存在
报告包含 row count
报告包含异常数据
报告说明 token / 2FA secret / SMTP 密码不迁移
```

---

### M0-BE-012：实现 backend-only quality gate

#### 目标

实现后端质量门禁。

#### 验收

```bash
bash scripts/quality-gate-backend.sh
```

---

## 15. M0-BE WBS

| 顺序 | 任务包 | 主要产物 | 验收命令 |
|---:|---|---|---|
| 1 | M0-BE-001 | solution / projects | `dotnet build` |
| 2 | M0-BE-002 | AOT csproj / Program | `dotnet publish /p:PublishAot=true` |
| 3 | M0-BE-003 | Shared 契约 | `dotnet test` |
| 4 | M0-BE-004 | Middleware | Integration tests |
| 5 | M0-BE-005 | Infrastructure / Dapper.AOT | SQL checks + AOT |
| 6 | M0-BE-006 | migration / seed | DB reset + seed |
| 7 | M0-BE-007 | System endpoints | curl smoke |
| 8 | M0-BE-008 | Auth | Auth integration tests |
| 9 | M0-BE-009 | Permission metadata | 401/403 tests |
| 10 | M0-BE-010 | OpenAPI export | export-openapi |
| 11 | M0-BE-011 | Migration Spike | report exists |
| 12 | M0-BE-012 | backend gate | `quality-gate-backend.sh` |

---

## 16. Backend-only Quality Gate

### 16.1 `scripts/quality-gate-backend.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

echo "=== WeCMS M0-BE Backend Quality Gate ==="

dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true

dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json

bash scripts/checks/check-no-select-star.sh
bash scripts/checks/check-no-dynamic-query.sh
bash scripts/checks/check-endpoint-permissions.sh
bash scripts/checks/check-json-context-coverage.sh
bash scripts/checks/check-no-frontend-change.sh

echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
```

### 16.2 禁止运行

```bash
pnpm install
pnpm typecheck
pnpm lint
pnpm build
pnpm openapi:generate
```

---

## 17. Trae IDE 执行 Prompt

```text
你是 WeCMS Next 项目的资深 .NET 10 Native AOT 架构师、Dapper.AOT 工程师、后端契约优先架构师和安全审计工程师。

现在执行 WeCMS Next M0-BE：只重建后端工程底座，不操作 SoybeanAdmin 前端代码。

必须优先阅读：

- AGENTS.md
- code_review.md
- docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md
- docs/context/WeCMS_工程骨架验证文档.md
- docs/context/WeCMS_工程落地执行计划与交付工件.md
- docs/context/WeCMS_ThinkPHP_系统详细说明文档.md

本轮 M0-BE 只允许修改：

- backend/**
- database/**
- scripts/**
- artifacts/openapi/**
- artifacts/reports/**
- docs/architecture/**
- docs/api/**
- docs/database/**
- docs/security/**
- docs/adr/**

本轮 M0-BE 禁止修改：

- frontend/**
- frontend/soybean-admin/**
- 任何 SoybeanAdmin 页面
- 任何前端 request 封装
- 任何前端 generated 类型
- 任何 pnpm 配置
- 任何前端路由、store、view、component

M0-BE 目标：

1. 创建干净的 backend solution。
2. 创建 WeCms.Api、WeCms.Shared、WeCms.Infrastructure、WeCms.Modules.System、WeCms.Modules.Cms。
3. 使用 ASP.NET Core Minimal APIs。
4. 使用 .NET 10。
5. 使用 WebApplication.CreateSlimBuilder。
6. 启用 Native AOT Only。
7. 禁止 MVC Controller。
8. 禁止 Razor / Razor Pages。
9. 禁止 EF Core。
10. 禁止 runtime Endpoint 自动扫描。
11. 禁止 dynamic / Query<dynamic>。
12. 禁止 SELECT *。
13. 接入 Dapper / Dapper.AOT / MySqlConnector。
14. 建立 ApiResult、PagedResult、ApiCodes、ValidationError、DomainException。
15. 建立 ExceptionMiddleware、RequestIdMiddleware。
16. 建立 WeCmsJsonContext，所有当前请求和响应 DTO 必须进入 JsonSerializerContext。
17. 建立 MySQL docker-compose、最小 migration、seed。
18. 实现 /health/live、/health/ready、/api/v1/system/ping、/api/v1/system/version、/api/v1/system/db-check。
19. 实现最小 Auth：POST /api/v1/auth/login、POST /api/v1/auth/refresh、POST /api/v1/auth/logout、GET /api/v1/auth/me。
20. Refresh Token 必须高强度随机，数据库只保存 hash，refresh 后旧 token 失效。
21. 登录成功写 sys_login_log，登录失败写 sys_security_event。
22. 实现 PermissionMetadata、RequirePermission、PermissionEndpointFilter、SystemPermissions 常量。
23. 实现一个 GET /api/v1/system/secure-ping 用于验证权限过滤器。
24. 未登录访问受保护 endpoint 必须返回 HTTP 401。
25. 无权限访问受保护 endpoint 必须返回 HTTP 403。
26. 实现 Endpoint 权限扫描测试。
27. 实现 OpenAPI JSON 导出到 artifacts/openapi/wecms-api-v1.json。
28. 实现 ThinkPHP 迁移 Spike，只输出 artifacts/reports/migration-spike-report.md，不做完整迁移。
29. 实现 scripts/quality-gate-backend.sh。
30. 实现 scripts/check-no-frontend-change.sh，确保本轮没有修改 frontend。

M0-BE 开发顺序：

1. M0-BE-001：创建 backend solution 和项目结构。
2. M0-BE-002：配置 .NET 10 Native AOT。
3. M0-BE-003：实现 Shared 契约层。
4. M0-BE-004：实现 Middleware。
5. M0-BE-005：实现 Infrastructure Data / Dapper.AOT。
6. M0-BE-006：实现 database migration 和 seed。
7. M0-BE-007：实现 System endpoints。
8. M0-BE-008：实现 Auth 最小闭环。
9. M0-BE-009：实现权限元数据和权限扫描。
10. M0-BE-010：实现 OpenAPI 导出。
11. M0-BE-011：实现 ThinkPHP 迁移 Spike。
12. M0-BE-012：实现 backend-only quality gate。

每个任务开始前必须输出：

- 本任务目标
- 允许修改文件
- 禁止修改文件
- 风险点
- 验证命令

每个任务完成后必须运行或说明以下验证：

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
bash scripts/checks/check-no-frontend-change.sh
```

最终只允许使用后端门禁：

```bash
bash scripts/quality-gate-backend.sh
```

不要运行：

```bash
pnpm install
pnpm typecheck
pnpm lint
pnpm build
pnpm openapi:generate
```

不要修改 frontend 目录。

M0-BE 完成标准：

- 后端可以 build。
- 后端测试通过。
- Native AOT publish 通过。
- MySQL 可连接。
- migration + seed 可从空库执行。
- health/system endpoints 正常。
- login / refresh / logout / me 可用。
- Refresh Token 只保存 hash。
- 未登录返回 HTTP 401。
- 无权限返回 HTTP 403。
- OpenAPI JSON 可生成。
- Endpoint 权限扫描通过。
- JsonSerializerContext 覆盖扫描通过。
- SQL 扫描无 SELECT *、无 Query<dynamic>。
- ThinkPHP 迁移 Spike 输出报告。
- frontend 目录无任何改动。
```

---

## 18. M0-BE 最终验收清单

```text
[ ] backend/WeCms.sln build 通过
[ ] dotnet test 通过
[ ] Native AOT publish 通过
[ ] 原生可执行文件可启动
[ ] /health/live 正常
[ ] /health/ready 正常
[ ] /api/v1/system/ping 正常
[ ] /api/v1/system/version 正常
[ ] /api/v1/system/db-check 正常
[ ] ApiResult 统一响应
[ ] ExceptionMiddleware 生效
[ ] JsonSerializerContext 覆盖当前 DTO
[ ] Dapper.AOT 查询成功
[ ] Repository 无 dynamic
[ ] SQL 无 SELECT *
[ ] Refresh Token 只存 hash
[ ] login / refresh / logout / me 可用
[ ] 未登录返回 HTTP 401
[ ] 无权限返回 HTTP 403
[ ] OpenAPI JSON 生成成功
[ ] migration 可从空库执行
[ ] seed 可创建超级管理员
[ ] sys_permission 可写入基础权限码
[ ] migration-spike-report.md 输出
[ ] scripts/quality-gate-backend.sh 通过
[ ] frontend/soybean-admin 没有任何文件改动
```

---

## 19. 阶段移交

### M0-BE 完成后移交到 M0.5-FE

M0.5-FE 才开始处理：

```text
SoybeanAdmin 初始化 / 整理
前端 request 封装
OpenAPI -> TypeScript generated
登录页接入
/auth/me 接入
Dashboard 当前用户展示
401 / 403 前端统一处理
动态路由 Spike
```

### M0-BE 不直接进入 M2

M0-BE 完成后不能直接做完整 User / Role / Menu。  
必须先完成 M0.5-FE 或明确前端后移策略，然后再进入 M1 / M2。

---

## 20. 最终结论

WeCMS Next M0-BE 的目标不是做完整后台，而是交付一个 **后端工程底座可信、AOT 可信、Dapper.AOT 可信、契约输出可信、认证权限最小闭环可信** 的基础版本。

一句话定版：

```text
M0-BE 不操作 SoybeanAdmin 前端代码，只交付一个可 AOT 发布、可连接 MySQL、可生成 OpenAPI、具备最小 Auth 与权限元数据闭环的后端底座。
```
