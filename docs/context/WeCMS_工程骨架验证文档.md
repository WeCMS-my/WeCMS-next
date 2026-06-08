# WeCMS Next 工程骨架验证文档

> 文档版本：v1.0  
> 生成日期：2026-06-07  
> 适用阶段：M0 工程启动阶段  
> 技术栈：ASP.NET Core Minimal APIs、.NET 10、Native AOT Only、Dapper、Dapper.AOT、MySQL、SoybeanAdmin  
> 最高原则：后端契约优先，前端一切数据格式以后端为准。

---

## 1. 文档定位

本文件用于指导 **WeCMS Next** 项目的第一个工程验证阶段，即：

```text
M0：工程骨架搭建 + AOT 可发布验证 + Dapper.AOT 数据访问验证 + OpenAPI 契约闭环 + SoybeanAdmin 联通验证
```

本文件不是替代《WeCMS Next .NET10 AOT + SoybeanAdmin 完整迁移重构计划》，而是把其中的架构、规则、约束转化为可以直接执行的工程验证清单。

M0 阶段只追求一个目标：

```text
证明新技术栈、工程结构、AOT 编译、数据库访问、契约生成、前端联通和最小认证权限闭环全部可行。
```

---

## 2. M0 阶段目标

M0 阶段必须跑通以下闭环：

```text
1. 新仓库与目录结构创建完成。
2. .NET 10 Minimal API 项目可运行。
3. Native AOT publish 可成功。
4. MySQL 可连接。
5. Dapper / Dapper.AOT 强类型查询可运行。
6. OpenAPI JSON 可生成。
7. 前端可基于 OpenAPI 生成 TypeScript 类型。
8. SoybeanAdmin 可调用真实后端。
9. 登录、刷新、退出、/auth/me 最小闭环可运行。
10. 权限码元数据和权限过滤器可运行。
11. CI 能执行 build、test、AOT publish、frontend build。
12. ThinkPHP 用户、角色、菜单、权限迁移 Spike 可输出报告。
```

M0 成功后，才进入 M1/M2 的正式业务模块开发。

---

## 3. M0 非目标

M0 阶段明确不做以下内容：

```text
1. 不开发完整 CMS 文章模块。
2. 不开发复杂多租户。
3. 不做完整 WAF 规则调优。
4. 不做完整 2FA。
5. 不做完整审批流。
6. 不做所有 SoybeanAdmin 页面。
7. 不一次性迁移全部旧数据。
8. 不为了 UI 细节修改后端契约。
9. 不引入大量第三方包。
10. 不跳过 AOT publish 验证。
```

M0 的核心不是功能数量，而是底座正确。

---

## 4. 推荐仓库结构

建议创建新仓库：

```text
wecms-next/
  backend/
    src/
      WeCms.Api/
      WeCms.Shared/
      WeCms.Infrastructure/
      WeCms.Modules.System/
      WeCms.Modules.Cms/
    tests/
      WeCms.Tests.Unit/
      WeCms.Tests.Integration/

  frontend/
    soybean-admin/

  database/
    migrations/
    seeds/
    legacy-migration/

  docs/
    architecture/
    api/
    database/
    security/
    ops/
    adr/

  scripts/
    build/
    deploy/
    migration/
    openapi/

  artifacts/
    openapi/
    reports/
```

### 4.1 目录职责

| 目录 | 职责 |
|---|---|
| `backend/src/WeCms.Api` | API 启动项目，Minimal APIs 入口 |
| `backend/src/WeCms.Shared` | 统一返回、错误码、权限常量、公共 DTO |
| `backend/src/WeCms.Infrastructure` | 数据库、缓存、存储、安全、邮件、任务基础设施 |
| `backend/src/WeCms.Modules.System` | 用户、角色、菜单、权限、认证、设置、日志等系统模块 |
| `backend/src/WeCms.Modules.Cms` | 栏目、文章、页面、媒体等 CMS 模块 |
| `database/migrations` | 新系统数据库迁移脚本 |
| `database/seeds` | base/demo/test 种子数据 |
| `database/legacy-migration` | ThinkPHP 旧数据迁移脚本 |
| `frontend/soybean-admin` | SoybeanAdmin 前端项目 |
| `artifacts/openapi` | 后端生成的 OpenAPI 契约产物 |
| `artifacts/reports` | 迁移报告、安全报告、性能报告 |

---

## 5. M0 交付物总表

| 编号 | 交付物 | 验收标准 |
|---|---|---|
| M0-01 | Git 仓库结构 | 目录符合本文档约定 |
| M0-02 | .NET 10 Minimal API 项目 | `dotnet build` 通过 |
| M0-03 | Native AOT 发布 | `dotnet publish /p:PublishAot=true` 通过 |
| M0-04 | MySQL Docker Compose | 可本地启动数据库 |
| M0-05 | Dapper.AOT 查询样例 | 强类型查询成功，无 dynamic |
| M0-06 | `ApiResult` / `PagedResult` | 所有接口统一响应 |
| M0-07 | 异常处理中间件 | 统一错误结构，无堆栈泄露 |
| M0-08 | `JsonSerializerContext` | 当前 DTO 全部覆盖 |
| M0-09 | OpenAPI JSON 生成 | 产出 `artifacts/openapi/wecms-api-v1.json` |
| M0-10 | SoybeanAdmin 初始化 | 前端可启动 |
| M0-11 | 前端 request 封装 | 可解析后端 `ApiResult` |
| M0-12 | 登录接口最小版 | 可登录并返回 token |
| M0-13 | `/auth/me` 最小版 | 返回用户、角色、权限、菜单结构 |
| M0-14 | 权限码元数据最小版 | 未授权返回 403 |
| M0-15 | CI build/test/publish gate | 后端、前端、AOT 均通过 |
| M0-16 | 迁移 Spike 报告 | 输出用户、角色、权限迁移验证报告 |

---

## 6. 后端工程骨架验证

### 6.1 后端项目约束

后端必须满足：

```text
1. ASP.NET Core Minimal APIs。
2. .NET 10。
3. Native AOT Only。
4. 使用 CreateSlimBuilder。
5. 不使用 MVC Controller。
6. 不使用 Razor。
7. 不使用 Session。
8. 不使用运行时扫描 Endpoint。
9. 不使用 dynamic JSON 业务序列化。
10. DTO 必须进入 System.Text.Json Source Generator。
```

### 6.2 `WeCms.Api.csproj` 基线

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### 6.3 最小 Endpoint

M0 第一批接口：

```http
GET /health/live
GET /health/ready
GET /api/v1/system/ping
GET /api/v1/system/version
GET /api/v1/system/db-check
```

### 6.4 统一响应结构

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

C# 基线：

```csharp
public sealed record ApiResult<T>(int Code, string Msg, T? Data)
{
    public static ApiResult<T> Ok(T data) => new(0, "success", data);
    public static ApiResult<T> Fail(int code, string msg) => new(code, msg, default);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Records,
    int Page,
    int PageSize,
    long Total
);
```

### 6.5 JSON Source Generator

```csharp
[JsonSerializable(typeof(ApiResult<SystemVersionResponse>))]
[JsonSerializable(typeof(ApiResult<SystemPingResponse>))]
[JsonSerializable(typeof(ApiResult<DbCheckResponse>))]
[JsonSerializable(typeof(ApiResult<LoginResponse>))]
[JsonSerializable(typeof(ApiResult<CurrentUserResponse>))]
internal partial class WeCmsJsonContext : JsonSerializerContext
{
}
```

验收要求：

```text
1. 所有当前 Endpoint 请求 DTO 加入 JsonSerializerContext。
2. 所有当前 Endpoint 响应 DTO 加入 JsonSerializerContext。
3. AOT publish 不因 JSON 反射警告失败。
```

### 6.6 异常处理中间件

M0 必须具备统一异常处理中间件：

```text
1. 验证异常 -> 400。
2. 认证异常 -> 401。
3. 授权异常 -> 403。
4. 资源不存在 -> 404。
5. 并发冲突 -> 409。
6. 限流 -> 429。
7. 未处理异常 -> 500。
```

生产响应不得包含：

```text
1. 堆栈。
2. SQL。
3. 连接串。
4. 物理路径。
5. Secret。
```

### 6.7 AOT 发布验证命令

```bash
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  /p:PublishAot=true
```

验收标准：

```text
1. 发布成功。
2. Native AOT warning 不得忽略。
3. 原生可执行文件可启动。
4. health endpoint 可访问。
5. 数据库连接检查可访问。
```

---

## 7. 数据库与 Dapper.AOT 验证

### 7.1 MySQL 本地验证环境

建议 M0 提供 `docker-compose.yml`：

```yaml
services:
  mysql:
    image: mysql:8.4
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: wecms_next
      MYSQL_USER: wecms
      MYSQL_PASSWORD: wecms_dev_password
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  mysql_data:
```

### 7.2 M0 最小表

M0 至少创建：

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
```

### 7.3 SQL migration 目录

```text
database/migrations/
  000001_init_system_tables.sql
  000002_seed_base_permissions.sql
  000003_seed_super_admin.sql
```

### 7.4 Dapper.AOT 数据访问规则

M0 即执行以下限制：

```text
1. 禁止 Query<dynamic>。
2. 禁止 SELECT *。
3. 禁止拼接前端参数到 SQL。
4. 排序字段必须白名单。
5. Repository 返回强类型 DTO。
6. Repository 方法必须接收 CancellationToken。
7. 写操作必须校验 affected rows。
8. 所有 SQL 必须进入 Code Review。
```

### 7.5 数据访问样例

```csharp
public sealed record MinimalUserItem(
    long Id,
    string Username,
    string DisplayName,
    int Status,
    DateTimeOffset CreatedAt
);

public sealed class UserRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<MinimalUserItem>> GetMinimalUsersAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT id, username, display_name AS DisplayName, status, created_at AS CreatedAt
        FROM sys_user
        WHERE deleted_at IS NULL
        ORDER BY id DESC
        LIMIT 20
        """;

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<MinimalUserItem>(sql);
        return rows.AsList();
    }
}
```

### 7.6 Dapper.AOT 验收项

```text
[ ] AOT publish 无 Dapper 相关阻断。
[ ] Repository 不返回 dynamic。
[ ] SQL 字段显式列出。
[ ] 查询 DTO 与 SQL 字段匹配。
[ ] Repository 集成测试通过。
```

---

## 8. OpenAPI 与前端类型生成验证

### 8.1 契约链路

```text
后端 DTO
  ↓
OpenAPI JSON
  ↓
CI 保存契约快照
  ↓
前端 generated TypeScript 类型
  ↓
SoybeanAdmin service/api
  ↓
页面组件
```

### 8.2 OpenAPI 产物路径

```text
artifacts/openapi/wecms-api-v1.json
```

### 8.3 生成命令建议

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

### 8.4 前端类型目录

```text
frontend/soybean-admin/src/service/generated/
```

### 8.5 验收规则

```text
1. OpenAPI 能生成。
2. generated 类型能生成。
3. generated 目录禁止手写。
4. SoybeanAdmin mock 类型不能进入正式业务接口。
5. 前端 request 不重塑业务 data。
6. 后端 DTO 字段名是唯一事实源。
```

---

## 9. SoybeanAdmin 联通验证

### 9.1 前端 M0 目标

M0 前端只验证：

```text
1. SoybeanAdmin 项目可启动。
2. 真实 API baseURL 可配置。
3. request client 可调用 .NET 后端。
4. ApiResult 可解析。
5. 401 / 403 可统一处理。
6. 登录页调用真实后端。
7. /auth/me 调用真实后端。
8. 最小 Dashboard 可显示当前用户。
```

### 9.2 前端目录建议

```text
frontend/soybean-admin/src/
  service/
    generated/
    api/
      auth.ts
      system.ts
    request/
      index.ts
  store/
    modules/
      auth.ts
      route.ts
      permission.ts
  views/
    _builtin/
    dashboard/
    login/
```

### 9.3 ApiResult 类型

前端类型必须以后端生成类型为准。M0 可先手写临时类型，但进入正式联调前必须由 OpenAPI 生成。

```ts
export interface ApiResult<T> {
  code: number
  msg: string
  data: T
}
```

### 9.4 request 封装规则

```text
1. 只处理 token、401、403、code、msg。
2. 不重命名业务字段。
3. 不改分页结构。
4. 不在 interceptor 中重塑 data。
5. 不使用 SoybeanAdmin mock 契约替代后端契约。
```

---

## 10. 认证最小闭环验证

### 10.1 M0 Auth Endpoint

```http
POST /api/v1/auth/login
POST /api/v1/auth/logout
POST /api/v1/auth/refresh
GET  /api/v1/auth/me
```

### 10.2 最小表

```text
sys_user
sys_refresh_token
sys_user_session
sys_login_log
sys_security_event
```

### 10.3 登录规则

```text
1. Access Token 不存完整权限列表。
2. Refresh Token 只保存 hash。
3. 登录成功记录登录日志。
4. 登录失败记录安全事件。
5. 登出吊销当前 Refresh Token。
6. /auth/me 返回后端定义的用户、角色、权限、菜单结构。
```

### 10.4 `/auth/me` 最小响应

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
    "permissions": ["sys:user:list"],
    "menus": []
  }
}
```

---

## 11. 权限元数据验证

### 11.1 必须实现的组件

```text
PermissionMetadata
RequirePermission 扩展方法
PermissionEndpointFilter
Permissions 权限常量
Endpoint 权限扫描测试
```

### 11.2 Endpoint 示例

```csharp
group.MapGet("/system/users", GetUsers)
    .RequirePermission(Permissions.SystemUserList)
    .WithAudit("sys:user:list");
```

### 11.3 验收要求

```text
1. 除 AllowAnonymous 外，所有业务 Endpoint 必须有权限元数据。
2. 权限码必须使用常量。
3. 未登录返回 401。
4. 无权限返回 403。
5. CI 能扫描未绑定权限的 Endpoint。
```

---

## 12. CI/CD 骨架验证

### 12.1 后端 CI

```yaml
backend:
  steps:
    - dotnet restore
    - dotnet build --configuration Release -warnaserror
    - dotnet test --configuration Release
    - dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
```

### 12.2 前端 CI

```yaml
frontend:
  steps:
    - pnpm install --frozen-lockfile
    - pnpm typecheck
    - pnpm lint
    - pnpm build
```

### 12.3 M0 必须有的门禁

```text
1. 后端 build。
2. 后端 test。
3. 后端 Native AOT publish。
4. OpenAPI 生成。
5. 前端 typecheck。
6. 前端 build。
7. generated 类型不可手写修改。
8. Endpoint 权限元数据扫描。
```

---

## 13. ThinkPHP 迁移 Spike 验证

### 13.1 迁移 Spike 目标

M0 不做完整迁移，只做小样本验证：

```text
1. 旧用户能否映射到 sys_user。
2. 旧角色能否映射到 sys_role。
3. 旧菜单和规则能否拆分成 sys_menu / sys_permission。
4. 旧 think_auth_group.rules CSV 能否拆成 sys_role_menu / sys_role_permission。
5. legacy_id 是否足够回溯。
6. 异常数据是否可识别。
```

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

### 13.4 迁移报告

输出：

```text
artifacts/reports/migration-spike-report.md
```

报告必须包含：

```text
1. 旧用户数量。
2. 新用户数量。
3. 旧角色数量。
4. 新角色数量。
5. 旧规则数量。
6. 新菜单数量。
7. 新权限数量。
8. 角色权限关系数量。
9. 异常记录。
10. 需要人工处理的数据。
```

---

## 14. M0 执行顺序

建议执行顺序：

```text
第 1 步：建仓库和目录结构。
第 2 步：创建 .NET 10 Minimal API。
第 3 步：开启 PublishAot。
第 4 步：实现 ApiResult、异常中间件、health。
第 5 步：接 MySQL、Dapper、Dapper.AOT。
第 6 步：创建最小系统表 migration。
第 7 步：实现 OpenAPI 生成。
第 8 步：初始化 SoybeanAdmin。
第 9 步：前端 request 适配后端 ApiResult。
第 10 步：实现登录和 /auth/me。
第 11 步：实现权限元数据和权限过滤器。
第 12 步：做旧系统迁移小样本验证。
```

---

## 15. M0 任务拆分 WBS

| 编号 | 任务 | 产出 | 验收 |
|---|---|---|---|
| M0-BE-001 | 创建后端 solution | `.sln` 与项目结构 | build 通过 |
| M0-BE-002 | 配置 AOT | csproj | AOT publish 通过 |
| M0-BE-003 | 实现 ApiResult | Shared 类型 | 接口统一响应 |
| M0-BE-004 | 实现异常中间件 | Middleware | 500 不泄露堆栈 |
| M0-BE-005 | 实现 health | Endpoint | live/ready 可访问 |
| M0-BE-006 | 实现 JSON Source Generator | JsonContext | AOT 无 JSON 阻断 |
| M0-DB-001 | MySQL compose | docker-compose | 数据库可启动 |
| M0-DB-002 | 最小 migration | SQL 文件 | 表可创建 |
| M0-DB-003 | Dapper.AOT 查询 | Repository | 强类型查询成功 |
| M0-API-001 | OpenAPI 生成 | JSON 契约 | artifact 生成 |
| M0-FE-001 | 初始化 SoybeanAdmin | 前端项目 | dev/build 通过 |
| M0-FE-002 | request 封装 | request client | 可调 ping |
| M0-FE-003 | 登录联通 | Login 页面 | 可登录 |
| M0-AUTH-001 | 登录接口 | auth endpoints | 返回 token |
| M0-AUTH-002 | `/auth/me` | 当前用户接口 | 返回用户信息 |
| M0-AUTHZ-001 | 权限元数据 | filter/extension | 无权限 403 |
| M0-CI-001 | 后端 CI | pipeline | build/test/AOT 通过 |
| M0-CI-002 | 前端 CI | pipeline | typecheck/build 通过 |
| M0-MIG-001 | 迁移 Spike | report | 输出迁移报告 |

---

## 16. M0 验收清单

### 16.1 后端验收

```text
[ ] dotnet build 通过。
[ ] dotnet test 通过。
[ ] Native AOT publish 通过。
[ ] 原生可执行文件可启动。
[ ] health endpoint 正常。
[ ] ApiResult 统一响应。
[ ] ExceptionMiddleware 生效。
[ ] JsonSerializerContext 覆盖当前 DTO。
[ ] Dapper.AOT 查询成功。
[ ] OpenAPI JSON 生成成功。
```

### 16.2 数据库验收

```text
[ ] MySQL 本地环境可启动。
[ ] migration 可重复执行。
[ ] seed base 可执行。
[ ] sys_user 可创建超级管理员。
[ ] sys_permission 可写入基础权限码。
[ ] sys_menu 可写入基础菜单。
```

### 16.3 前端验收

```text
[ ] SoybeanAdmin 可启动。
[ ] SoybeanAdmin 可 build。
[ ] request client 可调真实后端。
[ ] 登录页调用真实后端。
[ ] /auth/me 调用真实后端。
[ ] 401/403 统一处理。
[ ] 不使用 mock 契约。
```

### 16.4 权限验收

```text
[ ] Endpoint 可绑定 RequirePermission。
[ ] 未登录返回 401。
[ ] 无权限返回 403。
[ ] AllowAnonymous 接口可登记。
[ ] CI 可扫描未绑定权限的业务 Endpoint。
```

### 16.5 迁移验收

```text
[ ] ThinkPHP 用户样本可读取。
[ ] 旧角色样本可读取。
[ ] 旧规则样本可读取。
[ ] CSV 权限可拆分。
[ ] legacy_id 可保留。
[ ] 迁移异常能输出报告。
```

---

## 17. M0 退出标准

只有同时满足以下条件，M0 才算完成：

```text
1. 后端可以 Native AOT 发布。
2. 后端可以连接数据库。
3. 后端可以生成 OpenAPI。
4. 前端可以从后端契约生成类型或完成生成链路验证。
5. SoybeanAdmin 可以调用真实后端。
6. 登录和 /auth/me 可以跑通。
7. 权限元数据机制可以跑通。
8. CI 可以阻止不符合 AOT 和契约规则的提交。
9. ThinkPHP 用户、角色、权限小样本迁移可以生成报告。
```

---

## 18. M0 到 M1 的移交条件

M0 完成后，进入 M1。M1 建议目标：

```text
1. 完整认证安全。
2. Refresh Token Rotation。
3. 2FA。
4. 验证码。
5. 密码找回。
6. 会话管理。
7. 登录限流。
8. 安全事件日志。
```

M0 移交给 M1 时必须提交：

```text
1. 工程仓库。
2. AOT publish 产物验证记录。
3. OpenAPI 契约文件。
4. 数据库 migration。
5. 前端 request 封装。
6. 登录联通演示。
7. 权限元数据演示。
8. CI 运行记录。
9. migration-spike-report.md。
```

---

## 19. 风险与控制

| 风险 | 表现 | 控制 |
|---|---|---|
| AOT 不兼容 | 新包或写法导致 publish 失败 | 从第一个 PR 开始执行 AOT gate |
| Dapper.AOT 限制 | dynamic、复杂映射不可用 | Repository 只用强类型 DTO |
| 前端契约漂移 | SoybeanAdmin mock 影响真实接口 | generated 类型和后端 OpenAPI 为准 |
| 旧权限迁移复杂 | CSV 权限难拆分 | M0 做迁移 Spike |
| 登录安全后补困难 | Token、session 设计不稳 | M0 最小认证即按最终方向设计 |
| CI 后补成本高 | 后续大量问题堆积 | M0 即建立 CI 门禁 |

---

## 20. 最终结论

M0 阶段的价值不是交付大量业务功能，而是验证新系统最关键的工程假设：

```text
ASP.NET Core Minimal APIs
+ .NET 10 Native AOT Only
+ Dapper.AOT
+ MySQL
+ OpenAPI
+ SoybeanAdmin
+ 后端契约优先
+ 权限元数据
+ CI AOT Gate
+ ThinkPHP 数据迁移 Spike
```

如果 M0 跑通，后续 M1/M2/M3 的业务开发才有稳定基础。  
如果 M0 没跑通，不能继续堆业务模块，必须先修正工程底座。
