# WeCMS Next 完整迁移重构升级计划

> 目标技术栈：ASP.NET Core Minimal APIs / .NET 10 / Native AOT Only / SqlSugar ORM / SoybeanAdmin\
> 目标类型：完整迁移、架构重构、业务模块重建、前后端分离后台 CMS\
> 文档版本：v1.3\
> 日期：2026-06-07

***

## 1. 项目目标

本项目不是对 ThinkPHP 版本做 UI 换皮，而是以现有 WeCMS ThinkPHP 项目为业务参考，进行完整重构升级：

```text
旧系统：ThinkPHP 8 + 服务端模板 + Tailwind CDN + Session + Auth CSV 权限
新系统：ASP.NET Core Minimal APIs + .NET 10 Native AOT + SqlSugar ORM + SoybeanAdmin
```

### 1.1 迁移目标

1. 完整迁移现有基础 CMS 能力。
2. 重新设计用户、角色、菜单、权限模型。
3. 将后台从服务端模板升级为 SoybeanAdmin SPA。
4. 将后端从 Controller/View 模式升级为 API-first。
5. 后端只允许 Native AOT 发布，不保留 JIT-only 运行路径。
6. 数据访问采用 SqlSugar ORM，不使用 EF Core。
7. 权限从 URL 匹配升级为权限码与 Endpoint Metadata。
8. 数据迁移可追踪、可回滚、可验证。
9. 新系统支持未来内容管理、插件、多租户、API 开放扩展。

### 1.2 非目标

本阶段不建议做：

```text
1. 兼容旧 ThinkPHP 页面运行。
2. 保留 iframe 后台框架。
3. 保留 jQuery 业务页面。
4. 保留 Session 作为主认证体系。
5. 保留 think_auth_group.rules CSV 权限模型。
6. 保留旧 token / 旧 2FA secret 作为新系统登录凭据。
7. 使用 MVC Controller / Razor / Server Rendered Views。
8. 使用 EF Core。
9. 使用运行时动态扫描和反射式自动注册作为核心机制。
```

***

## 2. 目标技术选型

### 2.1 后端

| 技术            | 选择                                                 |
| ------------- | -------------------------------------------------- |
| Runtime       | .NET 10                                            |
| Web Framework | ASP.NET Core Minimal APIs                          |
| 发布方式          | Native AOT Only                                    |
| 数据访问          | SqlSugar ORM                                |
| 数据库驱动         | MySqlConnector，默认保持 MySQL 迁移成本最低                   |
| JSON          | System.Text.Json Source Generator                  |
| Auth          | JWT Access Token + Refresh Token Rotation          |
| 权限            | RBAC + Permission Code + EndpointFilter            |
| 日志            | Structured Logging + Audit Middleware              |
| 缓存            | MemoryCache MVP，Redis 可作为二期可选项                     |
| 文件            | Local Private Storage MVP，S3 Compatible Storage 可选 |
| API 风格        | RESTful + 后端契约优先，SoybeanAdmin 仅做消费适配               |

### 2.2 前端

| 技术         | 选择                                  |
| ---------- | ----------------------------------- |
| Admin 模板   | SoybeanAdmin                        |
| Framework  | Vue 3                               |
| Build      | Vite                                |
| Language   | TypeScript                          |
| Store      | Pinia                               |
| Router     | Vue Router                          |
| 权限路由       | SoybeanAdmin 动态路由模式                 |
| UI 组件      | 跟随 SoybeanAdmin 版本选择，建议先使用主线模板默认组件库 |
| API Client | 消费后端契约，SoybeanAdmin 请求模块只做传输层封装     |

***

## 3. 架构总览

### 3.1 目标架构

```text
Browser
  ↓
SoybeanAdmin SPA
  ↓
/api
  ↓
ASP.NET Core Minimal APIs (.NET 10 Native AOT)
  ↓
Application Services
  ↓
SqlSugar ORM Repositories
  ↓
MySQL 8 / MySQL 8.4
```

### 3.2 部署架构

```text
Nginx / Gateway
  ├── /admin  -> SoybeanAdmin 静态资源
  ├── /api    -> WeCms.Api Native AOT executable
  └── /files  -> 不直接暴露，文件走 API 授权访问

Database
  └── MySQL

Storage
  └── Private File Storage
```

### 3.3 运行边界

```text
前端只负责 UI 显示、路由、按钮可见性。
后端负责认证、鉴权、权限决策、数据范围、审计。
```

前端权限控制不能作为安全边界，所有敏感操作必须在后端通过 Endpoint 权限码校验。

***

## 4. Native AOT 架构约束

由于后端要求 **只采用 AOT 编译**，系统设计必须从一开始遵守 AOT 约束。

### 4.1 必须采用

```text
Minimal APIs
CreateSlimBuilder
明确 DTO
明确 Endpoint 注册
System.Text.Json Source Generator
SqlSugar ORM 友好查询
显式依赖注入注册
AOT Publish Gate
```

### 4.2 禁止或避免

```text
MVC Controller
Razor View
Session
运行时 Controller 扫描
反射式 JSON 序列化
动态 DTO / dynamic 查询
SELECT *
AutoMapper 运行时映射
大量反射扫描式模块注册
运行时插件加载
未验证 AOT 的第三方库
```

### 4.3 AOT 发布门禁

任何模块完成标准不是 `dotnet run` 成功，而是：

```bash
dotnet publish src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  /p:PublishAot=true
```

并且：

```text
1. 无阻塞 AOT 警告。
2. 无运行时 JSON metadata 缺失。
3. 所有关联 Endpoint 可启动。
4. 基础 API smoke test 通过。
```

***

## 5. 推荐解决方案结构

```text
wecms-next/
  backend/
    src/
      WeCms.Api/
        Program.cs
        appsettings.json
        Json/
        Middleware/
        Filters/
        Endpoints/

      WeCms.Shared/
        Contracts/
        Results/
        Errors/
        Security/
        Pagination/
        Time/

      WeCms.Infrastructure/
        Security/
        Storage/
        Cache/
        Crypto/
        Logging/

      WeCms.Persistence/
        Data/
        Migration/
        Modules/
          System/
          Cms/

      WeCms.Modules.System/
        Auth/
        Users/
        Roles/
        Menus/
        Permissions/
        Settings/
        Files/
        Logs/
        I18n/
        Dicts/
        SecurityCenter/

      WeCms.Modules.Cms/
        Channels/
        Articles/
        Media/
        Pages/

    tests/
      WeCms.Tests.Unit/
      WeCms.Tests.Integration/
      WeCms.Tests.Migration/

  frontend/
    soybean-admin/

  database/
    schema/
    migrations/
    legacy/
    seed/

  docs/
    architecture/
    api/
    migration/
```

### 5.1 模块化单体

第一阶段建议采用 **Modular Monolith**，不要急于微服务化。

```text
模块边界清晰
部署仍为一个 Native AOT API 可执行文件
数据库事务简单
开发与迁移成本更低
后续可以按模块拆服务
```

项目依赖矩阵：

```text
WeCms.Api -> WeCms.Modules.System / WeCms.Modules.Cms / WeCms.Infrastructure / WeCms.Persistence / WeCms.Shared
WeCms.Modules.System -> WeCms.Shared
WeCms.Modules.Cms -> WeCms.Shared
WeCms.Persistence -> WeCms.Shared / WeCms.Modules.System / WeCms.Modules.Cms
WeCms.Infrastructure -> WeCms.Shared
WeCms.Shared -> 不引用其它生产工程
```

`WeCms.Persistence` 是数据库适配器层，负责数据库连接、migration runner、SqlSugar ORM repository 实现。它允许引用 `WeCms.Modules.System` / `WeCms.Modules.Cms`，但只能用于实现模块暴露的 repository port；模块不得反向引用 Persistence，也不得持有 SQL、SqlSugar ORM、MySQLConnector、`DbConnection` 或 `DbTransaction`。

***

## 6. 后端项目配置建议

### 6.1 `WeCms.Api.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <PublishAot>true</PublishAot>
    <IsAotCompatible>true</IsAotCompatible>
    <InvariantGlobalization>false</InvariantGlobalization>

    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <EnableAotAnalyzer>true</EnableAotAnalyzer>
    <WarningsAsErrors>IL2026;IL3050;IL2070;IL2072</WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SqlSugarCore" Version="*" />
    <PackageReference Include="MySqlConnector" Version="*" />
  </ItemGroup>
</Project>
```

> 版本号在实际项目中必须锁定，不建议长期使用 `*`。初始落地时以当前 NuGet 稳定版本锁定，并写入 `Directory.Packages.props`。

### 6.2 Program 骨架

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        WeCmsJsonContext.Default);
});

builder.Services.AddAuthentication(/* JWT */);
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddRateLimiter();

builder.Services.AddWeCmsInfrastructure(builder.Configuration);
builder.Services.AddWeCmsSystemModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("AdminClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapSystemEndpoints();

app.Run();
```

### 6.3 JSON Source Generator

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(ApiResponse<LoginResponse>))]
[JsonSerializable(typeof(ApiResponse<UserProfileResponse>))]
[JsonSerializable(typeof(ApiResponse<PagedResult<UserListItemResponse>>))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(MenuTreeItemResponse[]))]
internal partial class WeCmsJsonContext : JsonSerializerContext
{
}
```

所有请求和响应 DTO 都必须加入 `JsonSerializerContext`。

***

## 7. SqlSugar ORM 使用规范

### 7.1 模块启用

SqlSugar ORM 只允许在 `WeCms.Persistence` 中注册和使用。模块层只能定义 repository port，不得持有 `SqlSugarClient`、`ISqlSugarClient`、连接对象或原始 SQL 文本。

### 7.2 查询规则

必须遵守：

```text
1. 使用明确 DTO / Record / Class。
2. 不使用 dynamic。
3. 不使用 SELECT *。
4. 查询投影必须显式映射到 DTO。
5. 条件、排序和分页必须由后端白名单构造。
6. 所有分页查询必须有限制。
7. 写操作必须由 Service / UseCase 控制事务。
8. 原始 SQL 仅允许在必要场景中使用，且必须参数化并进入 Review。
```

### 7.3 查询示例

```csharp
public sealed record UserListItem(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string Status,
    DateTimeOffset CreatedAt);

public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(
    string? keyword,
    int offset,
    int limit,
    CancellationToken cancellationToken)
{
    var db = _dbContext.GetClient();

    return await db.Queryable<SysUserEntity>()
        .Where(x => x.DeletedAt == null)
        .WhereIF(!string.IsNullOrWhiteSpace(keyword),
            x => x.Username.Contains(keyword!) || x.DisplayName.Contains(keyword!))
        .OrderByDescending(x => x.Id)
        .Skip(offset)
        .Take(limit)
        .Select(x => new UserListItem(
            x.Id,
            x.Username,
            x.DisplayName,
            x.Email,
            x.Status,
            x.CreatedAt))
        .ToListAsync(cancellationToken);
}
```

### 7.4 禁止示例

```csharp
// 禁止
await db.Ado.GetDataTableAsync("select * from sys_user");

// 禁止
var table = request.TableName;
await db.Ado.GetDataTableAsync($"select * from {table}");

// 禁止
await db.QueryableByObject(typeFromRuntime).ToListAsync();
```

***

## 8. API 响应规范：后端契约优先

本项目采用 **Backend Contract First** 原则：

```text
后端 DTO / OpenAPI / 契约测试 / 数据库语义
  ↓
生成或约束 TypeScript 类型
  ↓
SoybeanAdmin service/api 调用
  ↓
页面组件消费数据
```

SoybeanAdmin 是前端模板，不是 API 契约来源。前端一切数据格式以后端为准，不可随意修改。

### 8.1 统一响应结构

后端统一定义：

```csharp
public sealed record ApiResponse<T>(
    string Code,
    string Msg,
    T? Data,
    string? TraceId = null);

public static class ApiCodes
{
    public const string Success = "0000";
    public const string Unauthorized = "0401";
    public const string Forbidden = "0403";
    public const string ValidationError = "1001";
    public const string BusinessError = "2001";
    public const string Conflict = "2009";
    public const string TooManyRequests = "0429";
    public const string SystemError = "5000";
}
```

响应示例：

```json
{
  "code": "0000",
  "msg": "ok",
  "data": {},
  "traceId": "00-xxx"
}
```

前端 `.env` 只允许配置识别码，不允许改变业务结构：

```env
VITE_SERVICE_SUCCESS_CODE=0000
VITE_SERVICE_LOGOUT_CODES=0401
VITE_SERVICE_MODAL_LOGOUT_CODES=0401
VITE_SERVICE_EXPIRED_TOKEN_CODES=0401
```

### 8.2 分页响应

后端分页结构固定为：

```json
{
  "code": "0000",
  "msg": "ok",
  "data": {
    "records": [],
    "page": 1,
    "pageSize": 20,
    "total": 100
  },
  "traceId": "00-xxx"
}
```

C#：

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Records,
    int Page,
    int PageSize,
    long Total);
```

### 8.3 前端适配边界

```text
1. 前端不得为了适配 SoybeanAdmin mock 数据修改后端字段。
2. 前端不得私自重命名后端字段。
3. 前端不得私自改变分页结构。
4. 前端不得私自改变枚举值。
5. 前端不得在 request interceptor 中重塑业务 data。
6. SoybeanAdmin 内部组件需要的 prop 映射，只能在组件绑定层或 route adapter 层完成。
7. generated 类型目录禁止手写修改。
8. 字段变更必须先改后端 DTO，再更新 OpenAPI，再更新前端类型。
```

***

## 9. 认证设计

### 9.1 登录接口

```text
POST /api/auth/login
```

请求：

```json
{
  "username": "admin",
  "password": "******",
  "captchaId": "optional",
  "captchaCode": "optional",
  "totpCode": "optional"
}
```

响应：

```json
{
  "code": "0000",
  "msg": "ok",
  "data": {
    "accessToken": "...",
    "expiresIn": 1800,
    "requiresTwoFactor": false,
    "twoFactorTicket": null
  }
}
```

### 9.2 Token 策略

推荐：

```text
Access Token：短有效期，例如 15~30 分钟。
Refresh Token：长有效期，例如 7~30 天。
Refresh Token Rotation：每次刷新都更新。
Refresh Token 只保存 hash，不保存明文。
```

### 9.3 Token Claim

```json
{
  "sub": "10001",
  "username": "admin",
  "security_stamp": "...",
  "permission_version": "12"
}
```

不要把大量权限码写入 Access Token。权限通过缓存或数据库按 `user_id + permission_version` 加载。

### 9.4 Refresh Token 表

```text
sys_refresh_token
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

### 9.5 密码迁移

旧系统 PHP 密码 hash 不直接重算。新系统登录过程：

```text
1. 读取 sys_user.password_hash。
2. 如果格式为 legacy_php_bcrypt，则使用兼容验证器。
3. 验证成功后改写为新格式 PBKDF2。
4. 更新 password_migrated_at。
```

新格式建议：

```text
wecms.pbkdf2.v1.<iterations>.<salt-base64>.<hash-base64>
```

PBKDF2 可使用 .NET 标准加密 API，AOT 风险低。

***

## 10. 权限模型设计

### 10.1 模型

```text
User
  ↓ many-to-many
Role
  ↓ many-to-many
Permission

Role
  ↓ many-to-many
Menu
```

### 10.2 权限码规范

```text
系统:资源:动作
```

示例：

```text
sys:user:list
sys:user:create
sys:user:update
sys:user:delete
sys:role:assign-permission
sys:menu:sort
sys:setting:update
account:2fa:enable
```

### 10.3 Endpoint 绑定权限码

```csharp
public sealed record PermissionMetadata(string Code);

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return builder
            .RequireAuthorization()
            .WithMetadata(new PermissionMetadata(permissionCode));
    }
}
```

### 10.4 EndpointFilter

```csharp
public sealed class PermissionEndpointFilter(
    IPermissionChecker checker) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var permission = endpoint?.Metadata.GetMetadata<PermissionMetadata>();

        if (permission is null)
        {
            return await next(context);
        }

        var allowed = await checker.HasPermissionAsync(
            context.HttpContext.User,
            permission.Code,
            context.HttpContext.RequestAborted);

        return allowed
            ? await next(context)
            : Results.Forbid();
    }
}
```

### 10.5 超级管理员策略

超级管理员通过 `sys_role.code = 'super_admin'` 角色判定，拥有该角色的用户：

```text
1. 通过 RBAC 权限校验（角色拥有所有权限码）。
2. 不能被非超级管理员修改关键状态。
3. 不能被删除/禁用。
4. 系统必须保证至少一个可登录超级管理员。
```

***

## 11. 菜单与 SoybeanAdmin 动态路由设计

### 11.1 前端路由模式

使用 SoybeanAdmin 动态路由模式：

```env
VITE_AUTH_ROUTE_MODE=dynamic
```

后端提供当前用户路由：

```text
GET /api/auth/routes/constant
GET /api/auth/routes/user
```

也可以按项目模板的请求函数名适配：

```text
fetchGetConstantRoutes
fetchGetUserRoutes
```

### 11.2 `sys_menu` 到 Soybean 路由映射

`sys_menu` 字段：

```text
name
path
component
layout
parent_id
title
i18n_key
icon
sort
hidden
keep_alive
permission_code
```

返回示例：

```json
{
  "name": "system_user",
  "path": "/system/user",
  "component": "view.system_user",
  "meta": {
    "title": "用户管理",
    "i18nKey": "route.system_user",
    "icon": "mdi:account-group-outline",
    "order": 10,
    "keepAlive": true
  }
}
```

> 具体 `component` 字段格式必须以实际 SoybeanAdmin 分支中的 Route 类型为准；迁移初期需要先锁定 SoybeanAdmin 版本和组件库分支，然后建立后端路由 DTO。

### 11.3 菜单权限规则

```text
1. 后端只返回当前用户可访问菜单。
2. 菜单可绑定 permission_code。
3. 按钮权限从 permissions 数组返回。
4. 前端按钮隐藏只为体验。
5. 后端接口权限是最终安全边界。
```

***

## 12. 数据库设计

### 12.1 用户表 `sys_user`

```sql
create table sys_user (
  id bigint primary key auto_increment,
  username varchar(64) not null,
  display_name varchar(128) not null,
  email varchar(128) null,
  phone varchar(32) null,
  avatar_file_id bigint null,
  password_hash varchar(512) not null,
  password_hash_algorithm varchar(64) not null,
  password_migrated_at datetime null,
  status varchar(32) not null,
  security_stamp varchar(64) not null,
  permission_version bigint not null default 1,
  two_factor_enabled tinyint(1) not null default 0,
  two_factor_rebind_required tinyint(1) not null default 0,
  last_login_at datetime null,
  last_login_ip varchar(64) null,
  created_at datetime not null,
  updated_at datetime not null,
  deleted_at datetime null,
  unique key uk_sys_user_username (username)
);
```

### 12.2 角色表 `sys_role`

```sql
create table sys_role (
  id bigint primary key auto_increment,
  code varchar(64) not null,
  name varchar(128) not null,
  status varchar(32) not null,
  sort int not null default 0,
  data_scope varchar(32) not null default 'all',
  remark varchar(512) null,
  legacy_id bigint null,
  created_at datetime not null,
  updated_at datetime not null,
  deleted_at datetime null,
  unique key uk_sys_role_code (code)
);
```

### 12.3 用户角色表 `sys_user_role`

```sql
create table sys_user_role (
  user_id bigint not null,
  role_id bigint not null,
  created_at datetime not null,
  primary key (user_id, role_id)
);
```

### 12.4 菜单表 `sys_menu`

```sql
create table sys_menu (
  id bigint primary key auto_increment,
  parent_id bigint null,
  type varchar(32) not null,
  name varchar(128) not null,
  path varchar(256) null,
  component varchar(256) null,
  title varchar(128) not null,
  i18n_key varchar(128) null,
  icon varchar(128) null,
  sort int not null default 0,
  hidden tinyint(1) not null default 0,
  keep_alive tinyint(1) not null default 0,
  external_url varchar(512) null,
  permission_code varchar(128) null,
  status varchar(32) not null,
  legacy_id bigint null,
  legacy_rule_name varchar(256) null,
  created_at datetime not null,
  updated_at datetime not null,
  deleted_at datetime null,
  key ix_sys_menu_parent_sort (parent_id, sort)
);
```

### 12.5 权限表 `sys_permission`

```sql
create table sys_permission (
  id bigint primary key auto_increment,
  code varchar(128) not null,
  name varchar(128) not null,
  module varchar(64) not null,
  resource varchar(64) not null,
  action varchar(64) not null,
  http_method varchar(16) null,
  route_pattern varchar(256) null,
  status varchar(32) not null,
  legacy_id bigint null,
  legacy_rule_name varchar(256) null,
  created_at datetime not null,
  updated_at datetime not null,
  unique key uk_sys_permission_code (code)
);
```

### 12.6 角色菜单表 `sys_role_menu`

```sql
create table sys_role_menu (
  role_id bigint not null,
  menu_id bigint not null,
  created_at datetime not null,
  primary key (role_id, menu_id)
);
```

### 12.7 角色权限表 `sys_role_permission`

```sql
create table sys_role_permission (
  role_id bigint not null,
  permission_id bigint not null,
  created_at datetime not null,
  primary key (role_id, permission_id)
);
```

### 12.8 其他系统表

```text
sys_refresh_token
sys_setting
sys_file
sys_login_log
sys_audit_log
sys_i18n_message
sys_dict_type
sys_dict_value
sys_security_event
sys_ip_ban
```

***

## 13. 旧表到新表迁移映射

| 旧表                        | 新表                                      | 迁移策略                                       |
| ------------------------- | --------------------------------------- | ------------------------------------------ |
| `think_admin`             | `sys_user`                              | 用户基础资料迁移，密码保留 legacy hash，token 不迁移        |
| `think_auth_group`        | `sys_role`                              | `title` -> `name`，生成 `code`，保存 `legacy_id` |
| `think_auth_group_access` | `sys_user_role`                         | 主来源；若缺失则回退 `think_admin.groupid`           |
| `think_auth_rule`         | `sys_menu`                              | 菜单、目录、按钮按 type/pid/name 拆分                 |
| `think_auth_rule`         | `sys_permission`                        | 操作权限转权限码                                   |
| `think_auth_group.rules`  | `sys_role_menu` / `sys_role_permission` | CSV 解析、去重、校验 ID                            |
| `think_config`            | `sys_setting`                           | 敏感配置重新录入或重新加密                              |
| `think_file`              | `sys_file`                              | 元数据迁移，物理文件分批校验                             |
| `think_log`               | `sys_login_log`                         | 登录日志迁移                                     |
| `think_operate_log`       | `sys_audit_log`                         | 操作日志迁移                                     |
| `think_i18n_message`      | `sys_i18n_message`                      | 直接迁移并规范 key                                |
| `think_enum_type_dict`    | `sys_dict_type`                         | 字典类型迁移                                     |
| `think_enum_value_dict`   | `sys_dict_value`                        | 字典值迁移                                      |

***

## 14. 数据迁移策略

### 14.1 迁移原则

```text
1. 原库只读备份。
2. 新库全量导入。
3. 每张表有迁移脚本。
4. 每个脚本可重复执行或可回滚。
5. 每个阶段输出 row count 与 checksum。
6. 所有 legacy_id 保留，方便追溯。
7. 敏感数据默认不迁移，必须显式批准。
```

### 14.2 迁移流程

```text
1. 冻结旧系统写入窗口。
2. 备份旧数据库和文件目录。
3. 初始化新数据库 schema。
4. 迁移用户、角色、菜单、权限。
5. 迁移系统配置。
6. 迁移文件元数据。
7. 迁移日志、i18n、字典。
8. 校验数据量、关联完整性、权限矩阵。
9. 灰度上线。
10. 切换 DNS / 网关路由。
11. 旧系统只读保留一段时间。
```

### 14.3 权限 CSV 解析

旧数据：

```text
think_auth_group.rules = "1,2,3,4"
```

转换：

```text
1. split by comma。
2. trim。
3. 只保留正整数。
4. distinct。
5. 校验 ID 存在。
6. 根据 rule 类型映射到 menu 或 permission。
7. 写入 sys_role_menu / sys_role_permission。
8. 记录异常 rule id。
```

### 14.4 2FA 迁移策略

默认策略：

```text
不迁移旧 twofa_secret。
不迁移旧 backup code。
迁移 twofa_enabled 为参考字段。
新系统设置 two_factor_rebind_required = true。
用户首次登录后重新绑定。
```

原因：

```text
1. 2FA secret 高敏感。
2. 旧 secret 加密依赖旧 auth_key / SECRET_KEY。
3. 无感迁移风险高。
4. 重新绑定可清理旧 backup code 风险。
```

### 14.5 Token 迁移策略

```text
token 不迁移
token_expire_at 不迁移
Session 不迁移
所有用户切换到新系统后重新登录
```

### 14.6 配置迁移策略

普通配置：直接迁移。

敏感配置：

```text
auth_key
smtp_pass
secret_key
storage secret
```

建议：

```text
1. 不从旧 SQL dump 自动导入。
2. 运维或管理员在新系统配置界面重新录入。
3. 新系统使用新密钥重新加密。
4. 密钥来源于环境变量或密钥管理服务。
```

### 14.7 文件迁移策略

```text
第一阶段：迁移文件元数据，保留 legacy_path。
第二阶段：实现 LegacyFileStorageAdapter，只读旧文件。
第三阶段：后台任务复制文件到新私有存储。
第四阶段：校验 sha256 / size。
第五阶段：切换 file_storage_key。
```

***

## 15. 后端模块规划

### 15.1 Auth 模块

接口：

| Method | Path                        | 权限                     | 说明       |
| ------ | --------------------------- | ---------------------- | -------- |
| POST   | `/api/auth/login`           | anonymous              | 登录       |
| POST   | `/api/auth/refresh`         | anonymous              | 刷新 token |
| POST   | `/api/auth/logout`          | authenticated          | 退出       |
| GET    | `/api/auth/me`              | authenticated          | 当前用户     |
| GET    | `/api/auth/routes/constant` | authenticated          | 常量路由     |
| GET    | `/api/auth/routes/user`     | authenticated          | 用户动态路由   |
| POST   | `/api/auth/2fa/verify`      | authenticated / ticket | 验证 2FA   |
| POST   | `/api/auth/password`        | authenticated          | 修改密码     |

### 15.2 User 模块

| Method | Path                                    | 权限码                       |
| ------ | --------------------------------------- | ------------------------- |
| GET    | `/api/system/users`                     | `sys:user:list`           |
| GET    | `/api/system/users/{id}`                | `sys:user:detail`         |
| POST   | `/api/system/users`                     | `sys:user:create`         |
| PUT    | `/api/system/users/{id}`                | `sys:user:update`         |
| DELETE | `/api/system/users/{id}`                | `sys:user:delete`         |
| PATCH  | `/api/system/users/{id}/status`         | `sys:user:change-status`  |
| PUT    | `/api/system/users/{id}/roles`          | `sys:user:assign-role`    |
| POST   | `/api/system/users/{id}/password/reset` | `sys:user:reset-password` |
| POST   | `/api/system/users/{id}/2fa/reset`      | `sys:user:reset-2fa`      |

### 15.3 Role 模块

| Method | Path                                 | 权限码                          |
| ------ | ------------------------------------ | ---------------------------- |
| GET    | `/api/system/roles`                  | `sys:role:list`              |
| GET    | `/api/system/roles/{id}`             | `sys:role:detail`            |
| POST   | `/api/system/roles`                  | `sys:role:create`            |
| PUT    | `/api/system/roles/{id}`             | `sys:role:update`            |
| DELETE | `/api/system/roles/{id}`             | `sys:role:delete`            |
| PATCH  | `/api/system/roles/{id}/status`      | `sys:role:change-status`     |
| GET    | `/api/system/roles/{id}/menus`       | `sys:role:menu:list`         |
| PUT    | `/api/system/roles/{id}/menus`       | `sys:role:menu:update`       |
| GET    | `/api/system/roles/{id}/permissions` | `sys:role:permission:list`   |
| PUT    | `/api/system/roles/{id}/permissions` | `sys:role:permission:update` |

### 15.4 Menu 模块

| Method | Path                            | 权限码                      |
| ------ | ------------------------------- | ------------------------ |
| GET    | `/api/system/menus/tree`        | `sys:menu:list`          |
| POST   | `/api/system/menus`             | `sys:menu:create`        |
| PUT    | `/api/system/menus/{id}`        | `sys:menu:update`        |
| DELETE | `/api/system/menus/{id}`        | `sys:menu:delete`        |
| PATCH  | `/api/system/menus/{id}/status` | `sys:menu:change-status` |
| PATCH  | `/api/system/menus/sort`        | `sys:menu:sort`          |

### 15.5 Permission 模块

| Method | Path                           | 权限码                   |
| ------ | ------------------------------ | --------------------- |
| GET    | `/api/system/permissions`      | `sys:permission:list` |
| POST   | `/api/system/permissions/sync` | `sys:permission:sync` |

权限同步用于把代码中的权限码注册到数据库。

### 15.6 Setting 模块

| Method | Path                   | 权限码                  |
| ------ | ---------------------- | -------------------- |
| GET    | `/api/system/settings` | `sys:setting:view`   |
| PUT    | `/api/system/settings` | `sys:setting:update` |

### 15.7 File 模块

| Method | Path                              | 权限码                 |
| ------ | --------------------------------- | ------------------- |
| GET    | `/api/system/files`               | `sys:file:list`     |
| POST   | `/api/system/files/upload`        | `sys:file:upload`   |
| GET    | `/api/system/files/{id}/download` | `sys:file:download` |
| GET    | `/api/system/files/{id}/preview`  | `sys:file:preview`  |
| DELETE | `/api/system/files/{id}`          | `sys:file:delete`   |

### 15.8 Log 模块

| Method | Path                     | 权限码                  |
| ------ | ------------------------ | -------------------- |
| GET    | `/api/system/logs/login` | `sys:log:login-list` |
| GET    | `/api/system/logs/audit` | `sys:log:audit-list` |

### 15.9 I18n 模块

| Method | Path                             | 权限码               |
| ------ | -------------------------------- | ----------------- |
| GET    | `/api/system/i18n/messages`      | `sys:i18n:list`   |
| POST   | `/api/system/i18n/messages`      | `sys:i18n:create` |
| PUT    | `/api/system/i18n/messages/{id}` | `sys:i18n:update` |
| DELETE | `/api/system/i18n/messages/{id}` | `sys:i18n:delete` |

### 15.10 Dict 模块

```text
/api/system/dict-types
/api/system/dict-values
```

权限码：

```text
sys:dict-type:list/create/update/delete
sys:dict-value:list/create/update/delete/change-status
```

***

## 16. SoybeanAdmin 前端落地方案

### 16.1 初始化

```bash
pnpm create soybean-admin@latest
```

锁定：

```text
Node 版本
pnpm 版本
SoybeanAdmin 模板版本
组件库分支
```

### 16.2 推荐目录规划

```text
frontend/soybean-admin/src/
  service/
    api/
      auth.ts
      system-user.ts
      system-role.ts
      system-menu.ts
      system-permission.ts
      system-setting.ts
      system-file.ts
      system-log.ts
      system-i18n.ts
      system-dict.ts

  views/
    system/
      user/
      role/
      menu/
      permission/
      setting/
      file/
      log/
      i18n/
      dict/

  stores/
    modules/
      auth.ts
      route.ts
      permission.ts
```

### 16.3 请求适配

后端返回：

```json
{
  "code": "0000",
  "msg": "ok",
  "data": {}
}
```

前端环境变量：

```env
VITE_SERVICE_SUCCESS_CODE=0000
VITE_SERVICE_LOGOUT_CODES=0401
VITE_SERVICE_EXPIRED_TOKEN_CODES=0401
```

### 16.4 动态路由

启用：

```env
VITE_AUTH_ROUTE_MODE=dynamic
```

前端登录后流程：

```text
1. login 获取 accessToken。
2. 调用 /api/auth/me 获取用户、角色、权限码。
3. 调用 /api/auth/routes/user 获取动态路由。
4. SoybeanAdmin 注入路由。
5. 根据 permissions 控制按钮。
```

### 16.5 按钮权限

前端建立：

```ts
export function hasPermission(code: string) {
  return useAuthStore().permissions.includes(code);
}
```

示例：

```vue
<NButton v-if="hasPermission('sys:user:create')" type="primary">
  新增用户
</NButton>
```

后端仍必须在 Endpoint 层检查 `sys:user:create`。

***

## 17. 迁移实施阶段

### 阶段 0：准备与冻结

目标：明确边界，降低迁移风险。

任务：

```text
1. 锁定旧系统代码版本。
2. 备份数据库和文件。
3. 统计旧表行数。
4. 整理旧权限 rule。
5. 清理敏感 dump，不作为公开 seed。
6. 建立新项目仓库。
7. 建立 CI/CD 基线。
```

交付物：

```text
legacy-schema-report.md
legacy-permission-report.md
migration-risk-register.md
```

### 阶段 1：新后端骨架

任务：

```text
1. 创建 .NET 10 Web API AOT 项目。
2. 配置 CreateSlimBuilder。
3. 配置 JSON Source Generator。
4. 配置 SqlSugar ORM / MySqlConnector。
5. 配置 API Response。
6. 配置 Exception Handler。
7. 配置 Health Check。
8. 配置 AOT publish gate。
```

验收：

```text
dotnet publish /p:PublishAot=true 成功
/health 返回正常
/api/version 返回正常
```

### 阶段 2：数据库与迁移框架

任务：

```text
1. 创建 sys_* 新 schema。
2. 编写 legacy -> sys 数据映射脚本。
3. 编写用户、角色、菜单、权限迁移。
4. 编写 row count 校验。
5. 编写权限矩阵校验。
```

验收：

```text
用户数量匹配
角色数量匹配
权限规则映射完整
CSV rules 解析异常为 0 或有明确报告
```

### 阶段 3：Auth 与用户体系

任务：

```text
1. 登录。
2. Refresh Token。
3. Legacy PHP bcrypt 验证。
4. 新密码 hash。
5. 当前用户 /api/auth/me。
6. 退出。
7. 登录日志。
8. 登录失败限制。
```

验收：

```text
旧用户可登录。
旧密码登录后自动 rehash。
禁用用户不可登录。
Refresh token rotation 正常。
```

### 阶段 4：RBAC 与菜单权限

任务：

```text
1. 用户管理。
2. 角色管理。
3. 菜单管理。
4. 权限管理。
5. 角色菜单授权。
6. 角色权限授权。
7. PermissionEndpointFilter。
8. permission_version。
```

验收：

```text
无权限接口返回 403。
前端不显示无权限菜单。
修改角色权限后用户权限刷新。
超级管理员保护有效。
```

### 阶段 5：SoybeanAdmin 集成

任务：

```text
1. 初始化 SoybeanAdmin。
2. 适配 API 响应 code。
3. 适配登录接口。
4. 适配 /api/auth/me。
5. 适配动态路由。
6. 完成系统管理页面。
```

验收：

```text
登录成功。
动态路由加载成功。
菜单树正确。
按钮权限正确。
401/403 正确处理。
```

### 阶段 6：系统基础模块

任务：

```text
1. 设置管理。
2. 文件管理。
3. 日志管理。
4. i18n 管理。
5. 字典管理。
6. 安全中心。
```

验收：

```text
旧模块功能全部在新系统有对应实现。
文件可下载和预览。
配置保存后立即生效。
日志可查询。
```

### 阶段 7：数据最终迁移与灰度

任务：

```text
1. 全量迁移演练。
2. UAT。
3. 性能压测。
4. 安全测试。
5. 切换前备份。
6. 灰度用户访问。
7. 全量切换。
```

验收：

```text
迁移脚本可重复执行。
核心用户可登录。
权限矩阵一致。
业务页面可用。
旧系统只读归档。
```

***

## 18. 测试策略

### 18.1 后端测试

```text
Unit Test：密码、权限、IP 规则、配置校验。
Integration Test：API + MySQL Test Container。
Migration Test：旧表到新表映射。
Permission Test：权限矩阵。
AOT Smoke Test：发布后运行 API 测试。
```

### 18.2 前端测试

```text
Type Check
Lint
Build
Route Test
Permission Button Test
E2E：登录、用户 CRUD、角色授权、菜单授权
```

### 18.3 安全测试

```text
登录暴力尝试
Refresh Token 重放
无权限访问接口
上传危险文件
路径穿越
XSS payload
CSRF 场景，如果使用 Cookie Auth
敏感字段日志脱敏
```

***

## 19. CI/CD 建议

### 19.1 后端 Pipeline

```bash
dotnet restore
dotnet build --configuration Release -warnaserror
dotnet test --configuration Release
dotnet publish src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  /p:PublishAot=true \
  /warnaserror
```

### 19.2 前端 Pipeline

```bash
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint
pnpm build
```

### 19.3 数据库 Pipeline

```text
1. schema lint。
2. migration dry run。
3. legacy sample import。
4. row count validation。
5. permission mapping validation。
```

***

## 20. 风险清单与控制措施

| 风险                   | 等级 | 控制措施                             |
| -------------------- | -: | -------------------------------- |
| Native AOT 第三方库不兼容   |  高 | 每引入一个库必须通过 publish gate          |
| SqlSugar ORM 支持范围限制    |  高 | 禁用 dynamic，SQL 显式化，建立查询规范        |
| SoybeanAdmin 路由格式不匹配 | 中高 | 先锁版本，建立 route DTO 与 mock server  |
| 旧权限 CSV 数据脏          |  高 | 迁移脚本输出异常报告                       |
| 旧密码 hash 验证失败        |  高 | 建立 legacy hash 样本测试              |
| 2FA secret 迁移风险      |  高 | 默认重新绑定                           |
| 文件路径和存储混乱            | 中高 | 文件迁移分阶段，保留 legacy adapter        |
| 配置密钥泄露               |  高 | 敏感配置不自动迁移，重新录入                   |
| 新旧系统切换失败             |  高 | 灰度、只读归档、回滚脚本                     |
| 前后端 API 契约漂移         |  中 | OpenAPI / TypeScript 类型生成 / 契约测试 |

***

## 21. 里程碑建议

### M1：架构基线

```text
后端 AOT 骨架
SqlSugar ORM 连接 MySQL
API 响应结构
CI AOT publish
```

### M2：认证闭环

```text
登录
旧密码兼容
Refresh Token
/api/auth/me
SoybeanAdmin 登录接入
```

### M3：权限闭环

```text
用户
角色
菜单
权限
动态路由
Endpoint 权限校验
```

### M4：系统模块闭环

```text
设置
文件
日志
i18n
字典
安全中心
```

### M5：迁移闭环

```text
旧库迁移
权限矩阵校验
文件迁移
UAT
灰度上线
```

***

## 22. 验收标准

### 22.1 技术验收

```text
1. 后端只以 Native AOT 发布物部署。
2. 不存在 MVC Controller / Razor View。
3. 所有 JSON DTO 进入 Source Generator。
4. 所有 SQL 使用 SqlSugar ORM 规范。
5. CI 中包含 AOT publish。
6. SoybeanAdmin build 成功。
7. 生产环境无外部 CDN 强依赖。
```

### 22.2 安全验收

```text
1. 所有写接口必须认证。
2. 所有敏感接口必须绑定权限码。
3. 非授权用户返回 403。
4. 401 自动退出或刷新 token。
5. Refresh Token 支持吊销。
6. 密码、token、secret 不进入明文日志。
7. 文件访问必须鉴权。
8. 超级管理员保护规则生效。
```

### 22.3 业务验收

```text
1. 旧系统用户可登录新系统。
2. 用户、角色、菜单、权限功能完整。
3. 动态菜单与旧权限映射一致。
4. 设置、文件、日志、i18n、字典模块可用。
5. 数据迁移 row count 与校验报告通过。
```

***

## 23. 开发规范

### 23.1 后端代码规范

```text
1. Endpoint 文件按模块分组。
2. DTO 不复用数据库实体。
3. Repository 只做 SQL，不写业务规则。
4. Service 写业务规则与事务边界。
5. Endpoint 只做输入输出绑定。
6. 权限码集中定义。
7. 错误码集中定义。
8. 所有时间使用 DateTimeOffset 或统一 UTC。
9. 所有外部输入必须验证。
10. 所有写操作记录审计。
```

### 23.2 SQL 规范

```text
1. 禁止 SELECT *。
2. 禁止字符串拼 SQL。
3. 所有查询必须分页或限制。
4. 所有状态字段使用枚举字符串或小型字典。
5. 软删除统一 deleted_at。
6. 所有表有 created_at / updated_at。
7. 重要表保留 legacy_id。
```

### 23.3 前端规范

```text
1. 业务 API 统一放在 service/api。
2. 权限码使用常量。
3. 页面只调用 API，不拼接后端 URL。
4. 按钮权限使用统一 helper / directive。
5. 表格、表单、弹窗遵循 SoybeanAdmin 模板规范。
6. 路由名称与 sys_menu.name 对齐。
```

***

## 24. 推荐实施顺序清单

```text
01. 锁定 SoybeanAdmin 模板版本。
02. 创建 wecms-next 仓库。
03. 创建 .NET 10 AOT API 骨架。
04. 接入 SqlSugar ORM 与 MySQL。
05. 建立 sys_* schema。
06. 建立 API response 与错误码。
07. 实现 Auth 登录与旧密码兼容。
08. 实现用户、角色、菜单、权限。
09. 实现 PermissionEndpointFilter。
10. 初始化 SoybeanAdmin。
11. 对接登录和 /api/auth/me。
12. 对接动态路由。
13. 完成系统管理页面。
14. 编写旧库迁移脚本。
15. 迁移文件元数据与只读 legacy adapter。
16. 完成设置、日志、i18n、字典、安全中心。
17. 执行 UAT 和迁移演练。
18. 灰度上线。
19. 旧系统只读归档。
20. 清理旧系统敏感备份。
```

***

## 25. 最终建议

这次迁移应坚持三条底线：

```text
第一：业务参考旧系统，技术架构不要继承旧系统。
第二：AOT 是硬约束，从第一天就纳入 CI，而不是最后再适配。
第三：权限模型必须重做，不能把 think_auth_group.rules CSV 带入新系统。
```

推荐最终形态：

```text
ASP.NET Core Minimal APIs
+ .NET 10 Native AOT
+ SqlSugar ORM
+ MySQL
+ RBAC Permission Code
+ SoybeanAdmin Dynamic Routes
+ API-first CMS Foundation
```

该方案可以在保留现有 WeCMS 业务资产的同时，彻底摆脱旧后台模板、iframe、CSV 权限、Session token 和 jQuery 页面交互带来的长期技术债。

***

***

***

***

## 26. 后端契约优先补充决议

### 26.1 架构决议

```text
前端一切数据格式以后端为准，不可随意修改。
SoybeanAdmin 是前端 UI 模板，不是 API 契约来源。
后端 DTO / OpenAPI / 契约测试 / 数据库语义是唯一事实源。
```

本项目采用 **Backend Contract First**。数据结构、字段命名、枚举值、错误码、分页结构、菜单 DTO、权限码均以后端定义为准。前端只负责消费、展示、交互与局部 UI 适配。

### 26.2 契约流转方向

```text
后端 DTO / ApiResult / PagedResult / PermissionCode / MenuRoute DTO
  ↓
OpenAPI 契约输出
  ↓
TypeScript generated 类型
  ↓
SoybeanAdmin service/api 封装
  ↓
页面组件消费
```

### 26.3 禁止事项

```text
1. 禁止为了适配 SoybeanAdmin mock 数据修改后端接口。
2. 禁止前端私自重命名后端字段。
3. 禁止前端私自修改分页结构。
4. 禁止前端私自修改枚举值。
5. 禁止页面直接使用 SoybeanAdmin mock 类型作为业务类型。
6. 禁止 request interceptor 重塑业务 data。
7. 禁止前端自定义业务错误码。
8. 禁止前端独立维护一套业务菜单源。
9. 禁止前端私自补充后端未返回的业务权限。
10. 禁止前端以隐藏按钮替代后端权限校验。
```

### 26.4 统一响应结构

后端统一响应结构如下：

```json
{
  "code": "0000",
  "msg": "ok",
  "data": {}
}
```

分页结构如下：

```json
{
  "records": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

规则：

```text
CONTRACT-001：全系统只允许一种 ApiResponse 响应结构。
CONTRACT-002：全系统只允许一种分页结构。
CONTRACT-003：字段变更必须先改后端 DTO，再更新 OpenAPI，再更新前端 generated 类型。
CONTRACT-004：前端组件需要其他 prop 名称时，只能在组件绑定层映射，不能改变接口返回。
CONTRACT-005：后端错误码由 ApiCodes 统一定义，前端不得新增业务错误码。
CONTRACT-006：菜单、权限、路由事实源是后端 /api/auth/me 或 /api/auth/routes/user。
CONTRACT-007：SoybeanAdmin route adapter 只允许存在于前端路由注入层。
```

### 26.5 TypeScript 类型管理

推荐目录：

```text
frontend/soybean-admin/src/service/generated
frontend/soybean-admin/src/service/api
frontend/soybean-admin/src/service/adapters
frontend/soybean-admin/src/constants/permissions.ts
```

规则：

```text
TS-CONTRACT-001：generated 目录由 OpenAPI 或后端契约生成，禁止手写修改。
TS-CONTRACT-002：service/api 只封装请求，不改变后端业务字段结构。
TS-CONTRACT-003：service/adapters 只做 UI 组件边界适配，不做业务契约重定义。
TS-CONTRACT-004：权限码常量必须来自后端权限清单或自动生成文件。
TS-CONTRACT-005：前端 Pull Request 如修改 generated 文件，必须说明对应后端 DTO 变更。
```

***

## 27. 强制性治理规则：安全、性能、WAF、AOT 与上线门禁

本章为新项目硬性治理规则。任何模块设计、编码、Code Review、测试、发布均必须遵守本章规则。违反 P0/P1 规则的代码不得合并，不得进入生产发布。

### 27.1 安全基线规则

新系统安全基线以 OWASP ASVS Level 2 为默认目标，认证、授权、文件上传、配置密钥、审计日志模块按更高强度处理。

```text
SEC-BASE-001：所有接口设计必须对照 OWASP API Security Top 10。
SEC-BASE-002：所有安全验收至少覆盖 ASVS Level 2 核心项。
SEC-BASE-003：认证、授权、文件上传、配置密钥、审计日志必须作为高风险模块处理。
SEC-BASE-004：任何“前端已隐藏按钮，所以后端不校验”的设计直接禁止。
SEC-BASE-005：任何“管理员接口只在内网使用，所以不做安全校验”的设计直接禁止。
SEC-BASE-006：任何“WAF 已拦截，所以业务代码不校验”的设计直接禁止。
SEC-BASE-007：所有输入必须经过后端校验。
SEC-BASE-008：所有输出敏感字段必须经过脱敏或禁止返回。
SEC-BASE-009：默认拒绝，显式允许。
SEC-BASE-010：最小权限原则适用于用户、角色、服务账号、数据库账号和部署账号。
```

### 27.2 API 契约强制规则

```text
API-CONTRACT-001：后端 DTO 是接口字段、字段类型、枚举值、分页结构的唯一事实源。
API-CONTRACT-002：前端不得为了适配 SoybeanAdmin mock 数据修改后端字段。
API-CONTRACT-003：前端不得私自重命名后端字段。
API-CONTRACT-004：前端不得在 request interceptor 中重塑业务 data。
API-CONTRACT-005：TypeScript 类型必须从后端 OpenAPI / DTO 生成或严格同步。
API-CONTRACT-006：generated 类型目录禁止手写修改。
API-CONTRACT-007：全系统只允许一种 ApiResponse 响应结构。
API-CONTRACT-008：全系统只允许一种分页结构。
API-CONTRACT-009：错误码由后端 ApiCodes 统一定义。
API-CONTRACT-010：前端不得自定义业务错误码。
API-CONTRACT-011：接口字段变更必须先改后端 DTO，再更新 OpenAPI，再更新前端类型。
API-CONTRACT-012：接口契约必须纳入契约测试。
```

### 27.3 Endpoint 安全元数据规则

```text
ENDPOINT-001：除 AllowAnonymous 接口外，所有 Endpoint 必须绑定权限码或内部访问策略。
ENDPOINT-002：CI 必须扫描所有 Minimal API Endpoint，发现未绑定权限码的敏感接口直接失败。
ENDPOINT-003：权限码必须使用常量，不允许手写字符串散落在业务代码。
ENDPOINT-004：权限码必须同步进入 sys_permission。
ENDPOINT-005：权限码删除、改名必须有 migration 记录。
ENDPOINT-006：所有写接口必须绑定审计元数据。
ENDPOINT-007：所有上传接口必须绑定 FilePolicy。
ENDPOINT-008：所有导出接口必须绑定 ExportPolicy。
ENDPOINT-009：所有匿名接口必须登记原因。
ENDPOINT-010：所有匿名接口必须单独限流。
```

示例：

```csharp
group.MapPost("/system/users", CreateUser)
    .RequirePermission(Permissions.UserCreate)
    .RequireRateLimiting(RateLimitPolicies.AdminWrite)
    .WithAudit("sys:user:create");
```

### 27.4 认证、Token、Refresh Token 与 2FA 规则

```text
AUTHN-001：Access Token 有效期建议 10~30 分钟。
AUTHN-002：Refresh Token 必须是长随机值，不得是 JWT 明文业务 token。
AUTHN-003：Refresh Token 数据库只保存 hash。
AUTHN-004：Refresh Token 必须支持轮换。
AUTHN-005：Refresh Token 被刷新后旧 token 立即失效。
AUTHN-006：登出必须吊销当前 Refresh Token。
AUTHN-007：修改密码必须吊销该用户所有 Refresh Token。
AUTHN-008：禁用用户必须吊销该用户所有 Refresh Token。
AUTHN-009：修改角色或权限必须更新 permission_version。
AUTHN-010：Access Token 不得携带完整权限列表。
AUTHN-011：Access Token 必须包含 userId、securityStamp、permissionVersion 或同等校验字段。
AUTHN-012：Token 校验失败必须返回统一错误码。
AUTHN-013：2FA secret 不得明文存储。
AUTHN-014：2FA backup code 只保存 hash。
AUTHN-015：2FA 绑定、解绑、重置必须记录安全事件。
AUTHN-016：迁移旧系统时不直接复用旧 2FA secret，默认要求用户重新绑定。
```

### 27.5 登录限流、风控、反暴力规则

```text
AUTHN-RATE-001：登录接口必须按 IP + 用户名双维度限流。
AUTHN-RATE-002：验证码接口必须限流。
AUTHN-RATE-003：Refresh Token 接口必须限流。
AUTHN-RATE-004：连续失败必须延迟响应或临时锁定。
AUTHN-RATE-005：管理员账号登录失败必须记录安全日志。
AUTHN-RATE-006：超级管理员登录失败必须触发高风险事件。
AUTHN-RATE-007：登录成功后必须清理失败计数或按策略衰减。
AUTHN-RATE-008：登录失败提示不得泄露账号是否存在。
AUTHN-RATE-009：高风险 IP 可进入临时黑名单。
AUTHN-RATE-010：限流策略必须支持环境差异化配置。
```

接口建议：

| 接口                         | 限流策略                   |
| -------------------------- | ---------------------- |
| `/api/auth/login`          | IP + username，滑动窗口     |
| `/api/auth/refresh`        | IP + token fingerprint |
| `/api/auth/captcha`        | IP 限制                  |
| `/api/system/users/*` 写操作  | 用户级限流                  |
| `/api/system/files/upload` | 用户 + IP + 文件大小综合限制     |
| `/api/system/logs/export`  | 低频限流                   |

### 27.6 RBAC、对象级授权与数据范围规则

RBAC 只能证明“是否能执行动作”，不能证明“是否能操作具体对象”。所有带 id 的接口必须进行对象级授权。

```text
AUTHZ-001：除 AllowAnonymous 接口外，所有 Endpoint 必须绑定权限码或内部访问策略。
AUTHZ-002：前端隐藏按钮不能替代后端权限校验。
AUTHZ-003：权限码必须可审计、可同步、可迁移。
AUTHZ-004：角色权限修改必须刷新关联用户 permission_version。
AUTHZ-005：超级管理员绕过权限检查必须记录 highRisk 审计。

AUTHZ-OBJ-001：所有带 id 的查询、修改、删除接口必须做对象级授权。
AUTHZ-OBJ-002：用户不能删除自己。
AUTHZ-OBJ-003：用户不能禁用自己。
AUTHZ-OBJ-004：用户不能删除最后一个超级管理员。
AUTHZ-OBJ-005：用户不能移除自己的最后一个可登录角色。
AUTHZ-OBJ-006：非超级管理员不能修改超级管理员账号。
AUTHZ-OBJ-007：非超级管理员不能授予自己未拥有的权限。
AUTHZ-OBJ-008：文件下载必须校验文件归属、业务模块权限或可见范围。
AUTHZ-OBJ-009：日志查询必须按权限过滤敏感字段。
AUTHZ-OBJ-010：批量操作必须对每个对象执行授权或用安全查询边界限制对象集合。
```

数据权限预留值：

```text
all       全部数据
dept      本部门
dept_tree 本部门及下级
self      仅本人
custom    自定义范围
```

```text
DATA-AUTH-001：列表查询必须经过 DataScope 过滤。
DATA-AUTH-002：详情查询必须经过 DataScope 校验。
DATA-AUTH-003：导出数据必须经过 DataScope 校验。
DATA-AUTH-004：统计接口必须经过 DataScope 校验。
DATA-AUTH-005：超级管理员绕过数据权限必须记录审计日志。
```

### 27.7 输入验证与 Mass Assignment 防护规则

```text
INPUT-001：所有请求 DTO 必须显式定义字段。
INPUT-002：禁止使用 Dictionary<string, object> 承接业务写入请求。
INPUT-003：禁止将前端请求对象直接映射为数据库实体。
INPUT-004：所有创建、更新请求必须使用字段白名单。
INPUT-005：排序字段必须白名单。
INPUT-006：筛选字段必须白名单。
INPUT-007：枚举值必须后端校验。
INPUT-008：字符串长度必须后端校验。
INPUT-009：富文本必须经过清洗策略。
INPUT-010：后端校验失败必须返回统一 fieldErrors。
```

### 27.8 SQL、SqlSugar ORM 强制规则

```text
SQLSUGAR-001：禁止 dynamic 查询/返回。
SQLSUGAR-002：禁止 SELECT *。
SQLSUGAR-003：禁止拼接 SQL 参数。
SQLSUGAR-004：排序字段必须白名单映射。
SQLSUGAR-005：分页参数必须后端校验。
SQLSUGAR-006：所有 SQL 参数必须命名参数化。
SQLSUGAR-007：Repository 只允许返回 DTO / Record，不返回 dynamic。
SQLSUGAR-008：事务必须由 Service 层控制。
SQLSUGAR-009：Repository 不得直接提交业务事务。
SQLSUGAR-010：所有写 SQL 必须返回影响行数并校验。
SQLSUGAR-011：删除必须默认软删除。
SQLSUGAR-012：批量操作必须限制最大数量。
SQLSUGAR-013：所有迁移 SQL 必须可重复演练。
SQLSUGAR-014：所有 Repository 方法必须接收 CancellationToken。
SQLSUGAR-015：所有 SQL CommandTimeout 必须显式配置。
SQLSUGAR-016：禁止循环中执行 N+1 查询。
SQLSUGAR-017：禁止无 where 条件更新或删除。
SQLSUGAR-018：所有慢 SQL 必须进入性能日志。
```

### 27.9 Native AOT 与第三方库准入规则

```text
AOT-LIB-001：任何新增 NuGet 包必须通过 Native AOT publish。
AOT-LIB-002：任何新增 NuGet 包必须说明是否使用反射、动态代理、表达式编译、运行时扫描。
AOT-LIB-003：禁止引入依赖 MVC Controller 的库。
AOT-LIB-004：禁止引入依赖 Newtonsoft.Json 动态序列化的业务路径。
AOT-LIB-005：禁止使用运行时 Assembly 扫描注册 Endpoint。
AOT-LIB-006：禁止使用动态代理 AOP 框架。
AOT-LIB-007：禁止使用 runtime code generation。
AOT-LIB-008：所有 JSON DTO 必须加入 JsonSerializerContext。
AOT-LIB-009：所有 Endpoint 输入输出类型必须可被 Source Generator 处理。
AOT-LIB-010：CI 必须执行 linux-x64 AOT publish。
AOT-LIB-011：AOT warning 不允许忽略。
AOT-LIB-012：依赖升级必须重新执行 AOT publish gate。
```

### 27.10 WAF 与边界防护规则

WAF 是边界防护，不是业务安全边界。

推荐链路：

```text
Internet
  ↓
CDN / Cloud WAF，可选
  ↓
Nginx + ModSecurity / Coraza + OWASP CRS
  ↓
ASP.NET Core AppGuard Middleware
  ↓
Endpoint 权限 / 输入验证 / 业务校验
  ↓
Database
```

```text
WAF-001：生产环境 API 前必须有反向代理层。
WAF-002：生产环境建议启用 Nginx + ModSecurity/Coraza + OWASP CRS。
WAF-003：WAF 只负责边界过滤，不允许替代后端认证、授权、输入验证。
WAF-004：WAF 初次上线必须先使用 Detection Only 模式。
WAF-005：WAF 规则进入 Blocking 模式前必须完成误报调优。
WAF-006：WAF 误报必须进入白名单审查流程，不允许开发人员私自绕过。
WAF-007：WAF 拦截事件必须进入安全日志。
WAF-008：WAF 配置必须纳入版本管理。
WAF-009：WAF 规则升级必须走灰度环境验证。
WAF-010：健康检查、静态资源、OpenAPI 文档访问规则必须单独配置。

WAF-NO-001：不得依赖 WAF 防止越权。
WAF-NO-002：不得依赖 WAF 防止 Mass Assignment。
WAF-NO-003：不得依赖 WAF 防止权限绕过。
WAF-NO-004：不得依赖 WAF 防止业务逻辑漏洞。
WAF-NO-005：不得在应用层自研复杂正则 WAF 替代专业边界 WAF。
```

### 27.11 AppGuard 应用层轻量防护规则

AppGuard 只负责轻量边界校验，不替代 WAF 与业务校验。

```text
APPGUARD-001：AppGuard 只能做轻量边界校验。
APPGUARD-002：复杂攻击检测交给 WAF。
APPGUARD-003：AppGuard 拒绝请求必须返回统一错误结构。
APPGUARD-004：AppGuard 拒绝请求必须记录 requestId、ip、path、reason。
APPGUARD-005：必须限制请求体大小。
APPGUARD-006：必须校验 Content-Type 白名单。
APPGUARD-007：必须限制 JSON 深度。
APPGUARD-008：必须限制 Header 长度。
APPGUARD-009：必须限制上传 multipart body 大小。
APPGUARD-010：明显非法路径必须快速拒绝。
```

### 27.12 性能预算与容量规则

```text
PERF-001：每个接口必须有性能预算。
PERF-002：没有性能预算的接口不能进入验收。
PERF-003：所有列表接口必须分页。
PERF-004：所有列表接口默认 pageSize 不超过 20。
PERF-005：所有列表接口最大 pageSize 不超过 100。
PERF-006：导出接口不得复用普通列表接口的大 pageSize。
PERF-007：排序字段必须白名单。
PERF-008：筛选字段必须白名单。
PERF-009：禁止前端传任意 SQL 字段名排序。
PERF-010：高频接口必须压测。
```

推荐预算：

| 类型             |  P95 目标 |   P99 目标 | 说明         |
| -------------- | ------: | -------: | ---------- |
| 登录             | ≤ 500ms | ≤ 1000ms | 包含密码校验     |
| `/api/auth/me` | ≤ 120ms |  ≤ 300ms | 应使用权限/菜单缓存 |
| 普通列表           | ≤ 200ms |  ≤ 500ms | 必须分页       |
| 普通详情           | ≤ 100ms |  ≤ 300ms | 主键查询       |
| 写操作            | ≤ 300ms |  ≤ 800ms | 包含审计日志     |
| 日志查询           | ≤ 500ms | ≤ 1500ms | 必须索引和时间范围  |
| 导出             |    异步任务 |     异步任务 | 禁止同步大导出    |

### 27.13 缓存与一致性规则

```text
CACHE-AUTHZ-001：权限缓存 key 必须包含 userId + permissionVersion。
CACHE-AUTHZ-002：修改用户角色必须更新 permission_version。
CACHE-AUTHZ-003：修改角色权限必须更新关联用户 permission_version。
CACHE-AUTHZ-004：修改菜单权限必须更新关联用户 permission_version。
CACHE-AUTHZ-005：禁用角色必须更新关联用户 permission_version。
CACHE-AUTHZ-006：权限缓存不得超过 30 分钟。
CACHE-AUTHZ-007：超级管理员权限缓存仍必须受 security_stamp 控制。

CACHE-CONFIG-001：系统配置缓存必须按 config_version 失效。
CACHE-CONFIG-002：敏感配置不得以明文进入缓存。
CACHE-CONFIG-003：修改配置必须记录审计日志。
CACHE-CONFIG-004：配置缓存刷新失败不得吞异常。

CACHE-OUTPUT-001：认证用户接口默认禁止 OutputCache。
CACHE-OUTPUT-002：用户态菜单、权限、个人信息接口禁止共享输出缓存。
CACHE-OUTPUT-003：可缓存接口必须显式标记 CachePolicy。
CACHE-OUTPUT-004：所有缓存接口必须定义缓存时长。
CACHE-OUTPUT-005：涉及权限、角色、菜单、配置变更后必须清理相关缓存。
```

### 27.14 文件上传与文件访问规则

```text
FILE-001：上传文件必须存储在非 WebRoot 私有目录或对象存储。
FILE-002：文件名必须由系统生成，不使用用户原始文件名作为存储名。
FILE-003：原始文件名只作为展示字段保存。
FILE-004：扩展名必须白名单。
FILE-005：MIME 必须白名单。
FILE-006：图片必须验证真实格式。
FILE-007：可执行扩展名一律禁止。
FILE-008：双扩展名必须拒绝，例如 a.php.jpg。
FILE-009：上传大小必须按业务类型限制。
FILE-010：文件下载必须鉴权。
FILE-011：文件预览必须鉴权。
FILE-012：文件路径不得由前端传入。
FILE-013：禁止把物理路径返回给前端。
FILE-014：文件下载必须使用 fileId 或 storageKey。
FILE-015：文件删除必须软删除并记录审计。
FILE-016：高风险文件类型必须异步扫描。
FILE-017：Office、PDF 默认 attachment 下载，不默认 inline。
FILE-018：头像、图片建议重编码或生成缩略图。
FILE-RATE-001：单用户上传频率限制。
FILE-RATE-002：单 IP 上传频率限制。
FILE-RATE-003：单用户每日上传总量限制。
FILE-RATE-004：单文件最大大小限制。
FILE-RATE-005：单请求 multipart body 最大大小限制。
```

### 27.15 审计日志、安全日志与脱敏规则

建议日志表拆分：

```text
sys_login_log
sys_security_event
sys_audit_log
sys_request_log
sys_job_log
sys_file_log
```

```text
AUDIT-001：所有写操作必须记录审计日志。
AUDIT-002：所有权限变更必须记录审计日志。
AUDIT-003：所有角色授权必须记录变更前后差异。
AUDIT-004：所有用户状态变更必须记录审计日志。
AUDIT-005：所有登录失败必须记录安全事件。
AUDIT-006：所有 2FA 绑定、解绑、重置必须记录安全事件。
AUDIT-007：所有文件上传、下载、删除必须记录审计日志。
AUDIT-008：所有配置变更必须记录审计日志。
AUDIT-009：超级管理员操作必须标记 highRisk=true。
AUDIT-010：审计日志不得被普通管理员删除。

LOG-REDACT-001：日志禁止记录 password。
LOG-REDACT-002：日志禁止记录 accessToken。
LOG-REDACT-003：日志禁止记录 refreshToken。
LOG-REDACT-004：日志禁止记录 twoFactorSecret。
LOG-REDACT-005：日志禁止记录 backupCode 明文。
LOG-REDACT-006：日志禁止记录 smtpPassword 明文。
LOG-REDACT-007：手机号、邮箱、身份证、IP 可按策略脱敏。
LOG-REDACT-008：请求体日志默认关闭，只允许白名单接口开启。
LOG-REDACT-009：生产环境禁止记录完整响应 body。
```

### 27.16 部署、反向代理与真实 IP 规则

```text
PROXY-001：生产环境必须配置可信反向代理列表。
PROXY-002：ASP.NET Core 只信任 KnownProxies / KnownNetworks 中的 Forwarded Headers。
PROXY-003：不得直接信任任意客户端传入的 X-Forwarded-For。
PROXY-004：真实 IP 获取必须封装为 ClientIpProvider。
PROXY-005：限流、审计、安全日志必须使用 ClientIpProvider。
PROXY-006：Nginx 必须限制 client_max_body_size。
PROXY-007：Nginx 必须限制 header/body timeout。
PROXY-008：Nginx 必须只暴露 /admin 静态资源和 /api。
PROXY-009：后端 Kestrel 不直接暴露公网。
PROXY-010：健康检查接口不得暴露敏感信息。
```

### 27.17 Cookie、CORS、CSRF 规则

```text
COOKIE-001：Refresh Token 不得存 localStorage。
COOKIE-002：Refresh Token Cookie 必须 HttpOnly。
COOKIE-003：生产环境 Cookie 必须 Secure。
COOKIE-004：SameSite 策略必须明确配置。
COOKIE-005：如果使用 Cookie Auth，所有写接口必须有 CSRF 防护。
COOKIE-006：如果使用 Bearer Token，前端不得把 refreshToken 暴露给 JS。

CORS-001：生产环境禁止 AllowAnyOrigin。
CORS-002：生产环境禁止 AllowAnyHeader + AllowAnyMethod 无限制组合。
CORS-003：CORS Origin 必须白名单。
CORS-004：后台管理端域名与 API 域名必须明确登记。
CORS-005：本地开发 CORS 与生产 CORS 分离配置。
```

### 27.18 HTTP 安全头规则

```text
HEADER-001：必须启用 Strict-Transport-Security。
HEADER-002：必须启用 X-Content-Type-Options: nosniff。
HEADER-003：必须启用 X-Frame-Options 或 frame-ancestors CSP。
HEADER-004：必须启用 Referrer-Policy。
HEADER-005：必须启用 Content-Security-Policy。
HEADER-006：生产环境禁止外部不受控 CDN。
HEADER-007：OpenAPI 文档生产环境默认关闭或仅管理员可访问。
```

CSP 方向：

```text
default-src 'self';
script-src 'self';
style-src 'self' 'unsafe-inline';
img-src 'self' data: blob:;
connect-src 'self' https://api.example.com;
frame-ancestors 'none';
```

### 27.19 SoybeanAdmin 前端约束规则

```text
FE-001：SoybeanAdmin 只作为 UI 模板，不作为 API 契约来源。
FE-002：禁止业务页面直接使用 mock 数据类型。
FE-003：禁止业务页面私自定义与后端不一致的 DTO。
FE-004：service/generated 目录禁止手写修改。
FE-005：service/api 只封装请求，不改业务字段结构。
FE-006：route adapter 只允许把后端菜单 DTO 映射为 SoybeanAdmin 路由对象。
FE-007：按钮权限只能消费后端 permissions 数组。
FE-008：前端隐藏按钮不代表拥有权限。
FE-009：所有 401 / 403 行为由统一 request handler 处理。
FE-010：禁止在页面内硬编码后端 URL。
FE-011：禁止在页面内硬编码权限码，必须使用权限常量。
FE-012：禁止使用 v-html 渲染未清洗内容。
FE-013：富文本内容必须经过后端和前端双重清洗策略。
```

### 27.20 监控、健康检查与可观测性规则

```text
OBS-001：必须提供 /health/live。
OBS-002：必须提供 /health/ready。
OBS-003：ready 检查必须包含数据库连通性。
OBS-004：ready 检查可包含 Redis / Storage 连通性。
OBS-005：健康检查不得泄露连接串、异常堆栈、服务器路径。
OBS-006：每个请求必须有 requestId。
OBS-007：响应 Header 建议包含 X-Request-Id。
OBS-008：日志必须包含 requestId、userId、ip、path、statusCode、elapsedMs。
OBS-009：高风险安全事件必须可告警。
OBS-010：慢接口必须可告警。
```

### 27.21 备份、恢复与数据保护规则

```text
BACKUP-001：数据库必须有每日备份。
BACKUP-002：备份必须加密。
BACKUP-003：备份必须定期恢复演练。
BACKUP-004：迁移前必须做全量备份。
BACKUP-005：迁移脚本必须可重复执行。
BACKUP-006：迁移脚本必须输出 row count 校验。
BACKUP-007：迁移脚本必须输出权限矩阵校验。
BACKUP-008：文件存储必须有备份策略。
BACKUP-009：配置密钥不得进入普通 SQL dump。
BACKUP-010：生产 SQL dump 必须脱敏后才允许进入测试环境。
```

### 27.22 供应链安全规则

```text
SUPPLY-001：后端必须提交 lock file。
SUPPLY-002：前端必须使用 frozen lockfile 安装。
SUPPLY-003：新增依赖必须经过 License、维护状态、安全风险评审。
SUPPLY-004：生产构建不得依赖外部不受控 CDN。
SUPPLY-005：依赖漏洞扫描必须进入 CI。
SUPPLY-006：高危漏洞不得发布生产。
SUPPLY-007：构建产物必须可追溯到 commit。
SUPPLY-008：生产制品必须有 checksum。
```

### 27.23 CI/CD 发布门禁规则

后端门禁：

```text
CI-BE-001：dotnet build -warnaserror 必须通过。
CI-BE-002：dotnet test 必须通过。
CI-BE-003：dotnet publish /p:PublishAot=true 必须通过。
CI-BE-004：Native AOT warning 不得被忽略。
CI-BE-005：Endpoint 权限扫描必须通过。
CI-BE-006：JsonSerializerContext 覆盖扫描必须通过。
CI-BE-007：SQL 规范扫描必须通过。
CI-BE-008：敏感词日志扫描必须通过。
CI-BE-009：依赖漏洞扫描必须通过。
CI-BE-010：迁移脚本 dry run 必须通过。
```

前端门禁：

```text
CI-FE-001：pnpm install --frozen-lockfile 必须通过。
CI-FE-002：typecheck 必须通过。
CI-FE-003：lint 必须通过。
CI-FE-004：build 必须通过。
CI-FE-005：generated 类型不得手写修改。
CI-FE-006：禁止提交 mock 数据作为正式接口契约。
CI-FE-007：禁止新增外部 CDN 依赖。
CI-FE-008：依赖漏洞扫描必须通过。
```

安全门禁：

```text
CI-SEC-001：新增接口必须有权限码或 AllowAnonymous 标记。
CI-SEC-002：AllowAnonymous 接口必须登记原因。
CI-SEC-003：新增写接口必须有审计标记。
CI-SEC-004：新增上传接口必须有 FilePolicy。
CI-SEC-005：新增导出接口必须有 ExportPolicy。
CI-SEC-006：新增列表接口必须有分页限制。
CI-SEC-007：新增敏感字段必须有脱敏策略。
```

### 27.24 新增模块准入清单

任何新模块进入开发前，必须回答：

```text
1. 是否定义 API DTO？
2. 是否定义 OpenAPI 契约？
3. 是否定义权限码？
4. 是否定义对象级授权？
5. 是否定义数据范围？
6. 是否定义审计行为？
7. 是否定义输入校验？
8. 是否定义分页和性能预算？
9. 是否定义 SQL 索引？
10. 是否定义缓存失效？
11. 是否定义日志脱敏？
12. 是否定义文件上传策略？
13. 是否定义导入导出策略？
14. 是否通过 AOT publish？
15. 是否通过前端契约生成？
```

### 27.25 项目章程级最终硬约束

```text
1. 后端契约优先。
2. SoybeanAdmin 只是 UI 模板。
3. Endpoint 必须有权限元数据。
4. 对象级授权必须执行。
5. WAF 只做边界，不替代业务安全。
6. 所有写操作必须审计。
7. 所有列表必须分页。
8. 所有 SQL 必须显式字段和参数化。
9. 所有上传必须私有存储和鉴权访问。
10. 所有日志必须脱敏。
11. 所有发布必须通过 Native AOT gate。
12. 所有新增模块必须通过准入清单。
```

***

## 28. 高级工程治理与平台化规范

本章解决的问题不是“能否安全上线”，而是新 CMS 平台能否长期维护、稳定演进、灰度发布、可回滚、可审计、可运维。第 27 章偏安全与上线门禁，本章偏平台级治理和工程生命周期。

### 28.1 API 生命周期与版本治理规则

正式 API 必须具备生命周期管理。任何接口都不应在无版本、无契约、无废弃策略的情况下长期演进。

```text
API-LIFE-001：正式 API 必须带版本前缀，例如 /api/v1。
API-LIFE-002：同一版本 API 不允许破坏性变更。
API-LIFE-003：字段新增属于兼容变更，字段删除、改名、类型变化属于破坏性变更。
API-LIFE-004：破坏性变更必须进入下一个 API 主版本。
API-LIFE-005：API 字段废弃必须先标记 deprecated，不允许直接删除。
API-LIFE-006：OpenAPI 文档必须作为契约快照纳入版本管理。
API-LIFE-007：CI 必须执行 OpenAPI diff，发现破坏性变更必须失败。
API-LIFE-008：前端 generated 类型必须来自当前后端契约，不允许手写覆盖。
API-LIFE-009：接口下线必须有迁移期、替代接口和下线日期。
API-LIFE-010：禁止一个接口返回多种结构。
API-LIFE-011：API 版本、前端版本、后端版本必须能在发布记录中关联。
API-LIFE-012：内部 API 和公开 API 必须明确区分。
```

版本规则：

```text
1. API 主版本用于破坏性变更。
2. 后端应用版本采用 SemVer。
3. 前端静态资源版本必须可追溯到 commit。
4. OpenAPI 契约变更必须有变更说明。
```

### 28.2 错误模型与异常处理规则

新系统必须有统一异常处理边界。生产环境不得泄露内部堆栈、SQL、物理路径、连接串。

```text
ERROR-001：全局必须只有一个异常处理中间件。
ERROR-002：业务异常、验证异常、认证异常、授权异常、系统异常必须分类映射。
ERROR-003：生产环境错误响应不得包含堆栈、SQL、物理路径、连接串。
ERROR-004：错误响应必须包含 requestId / traceId。
ERROR-005：错误码必须在后端统一登记。
ERROR-006：错误码不得由前端私自定义。
ERROR-007：字段验证错误必须支持 fieldErrors。
ERROR-008：系统异常必须记录日志，业务可预期异常按规则降低日志级别。
ERROR-009：敏感异常必须转换为通用错误消息。
ERROR-010：404、405、415、429、500 必须统一响应格式。
ERROR-011：异常日志必须携带 userId、ip、path、method、requestId。
ERROR-012：重复业务异常不得刷屏告警，应按规则采样或降级。
```

推荐错误结构：

```json
{
  "code": "1001",
  "msg": "参数验证失败",
  "data": null,
  "traceId": "00-xxx",
  "fieldErrors": {
    "username": ["用户名不能为空"]
  }
}
```

### 28.3 事务边界规则

CMS 后台很多操作不是单表 CRUD。创建用户、授权角色、修改权限、上传文件、刷新缓存都必须有明确事务边界。

```text
TX-001：事务只能由 Service / UseCase 层开启。
TX-002：Repository 不允许自行提交业务事务。
TX-003：跨多个 Repository 的写操作必须在同一事务中完成。
TX-004：权限变更、角色变更、菜单变更必须和 permission_version 更新放在同一事务。
TX-005：文件元数据写入成功但文件保存失败时必须补偿回滚。
TX-006：审计日志可以异步写入，但关键安全事件必须保证落库。
TX-007：事务中不得执行外部 HTTP 调用。
TX-008：事务中不得执行长耗时文件扫描。
TX-009：事务超时必须显式配置。
TX-010：事务失败必须返回统一错误码。
TX-011：跨服务或跨存储操作必须定义补偿策略。
TX-012：批量写入必须限制单事务大小。
```

### 28.4 幂等规则

重复提交、网络重试、任务重试和并发刷新 Token 都可能导致重复写入。关键写操作必须具备幂等或重复提交保护。

```text
IDEMP-001：创建类接口如涉及重复提交，必须支持 Idempotency-Key。
IDEMP-002：导入、导出、批量删除、批量授权必须支持幂等或重复提交保护。
IDEMP-003：Refresh Token 轮换必须防止并发重复刷新。
IDEMP-004：文件上传完成确认必须防止重复入库。
IDEMP-005：后台任务重试必须保证幂等。
IDEMP-006：支付、订单类模块未来接入时必须单独定义幂等键。
IDEMP-007：Idempotency-Key 必须绑定 userId、method、path、requestHash。
IDEMP-008：幂等记录必须有过期时间和清理任务。
```

### 28.5 并发控制规则

后台多个管理员同时编辑同一用户、角色、菜单、配置时，必须防止后提交覆盖先提交。

```text
CONCURRENCY-001：用户、角色、菜单、配置、权限等关键表必须有 row_version 或 updated_at 并发控制。
CONCURRENCY-002：编辑页面提交时必须携带 rowVersion。
CONCURRENCY-003：rowVersion 不匹配必须返回并发冲突错误。
CONCURRENCY-004：角色权限保存必须防止两个管理员互相覆盖。
CONCURRENCY-005：菜单树排序必须防止并发覆盖。
CONCURRENCY-006：系统配置保存必须防止覆盖别人刚修改的值。
CONCURRENCY-007：并发冲突响应必须提示前端刷新最新数据。
CONCURRENCY-008：批量操作必须使用稳定条件，禁止基于过期列表盲目提交。
```

### 28.6 业务不变量与状态机规则

业务不变量控制“系统不能进入错误状态”。这些规则必须沉入后端领域服务或业务 Guard，不得只依赖前端提示。

```text
INVARIANT-001：系统必须至少保留一个可登录超级管理员。
INVARIANT-002：用户不能禁用自己。
INVARIANT-003：用户不能删除自己。
INVARIANT-004：非超级管理员不能修改超级管理员。
INVARIANT-005：非超级管理员不能授予自己未拥有的权限。
INVARIANT-006：角色 code 创建后不得随意修改。
INVARIANT-007：权限 code 创建后不得随意修改，只能停用或迁移。
INVARIANT-008：菜单树不得形成循环。
INVARIANT-009：菜单层级必须有最大深度限制。
INVARIANT-010：菜单 component 必须在前端白名单中。
INVARIANT-011：系统内置角色不得删除。
INVARIANT-012：系统内置权限不得删除。
INVARIANT-013：系统内置配置不得删除。
INVARIANT-014：字典 typeCode 创建后不得随意修改。
INVARIANT-015：已被业务引用的字典项不得硬删除。
INVARIANT-016：禁用角色前必须检查是否会导致用户无法登录。
INVARIANT-017：删除菜单前必须处理子菜单和角色菜单引用。
INVARIANT-018：停用权限前必须检查关联 Endpoint 和角色授权。
```

### 28.7 数据生命周期与隐私治理规则

每类数据必须明确生命周期、删除策略、保留周期、脱敏策略和导出策略。

```text
DATA-LIFE-001：每张业务表必须明确是否支持软删除。
DATA-LIFE-002：软删除字段统一为 deleted_at、deleted_by。
DATA-LIFE-003：默认查询必须排除 deleted_at 不为空的数据。
DATA-LIFE-004：恢复软删除数据必须记录审计日志。
DATA-LIFE-005：硬删除只能用于清理任务或合规删除流程。
DATA-LIFE-006：用户、角色、权限、菜单、配置默认禁止硬删除。
DATA-LIFE-007：日志表必须定义保留周期。
DATA-LIFE-008：Refresh Token 必须定期清理过期记录。
DATA-LIFE-009：验证码、临时文件、导出文件必须定期清理。
DATA-LIFE-010：测试环境不得使用未脱敏生产数据。
DATA-LIFE-011：导出的 Excel / CSV 必须按权限过滤字段。
DATA-LIFE-012：敏感字段必须定义脱敏展示规则。
DATA-LIFE-013：敏感配置不得出现在普通数据库 dump。
DATA-LIFE-014：后台管理员查看敏感信息必须记录审计。
```

数据分类：

| 等级           | 示例                       | 规则              |
| ------------ | ------------------------ | --------------- |
| Public       | 菜单标题、公开配置                | 可普通展示           |
| Internal     | 角色名称、操作日志摘要              | 需要登录            |
| Confidential | 邮箱、手机号、IP、用户资料           | 脱敏展示            |
| Secret       | Token、2FA Secret、SMTP 密码 | 不返回前端、不写日志      |
| Critical     | 加密主密钥、签名密钥               | 只在密钥管理系统或环境安全通道 |

### 28.8 数据库迁移与零停机发布规则

SqlSugar ORM 项目高度依赖 SQL 纪律。所有数据库变更必须版本化、可审计、可演练。

```text
DBMIG-001：所有数据库结构变更必须通过 migration 脚本。
DBMIG-002：migration 脚本必须进入版本管理。
DBMIG-003：禁止开发人员手工改生产数据库结构。
DBMIG-004：生产 migration 必须先在 staging 演练。
DBMIG-005：migration 执行前必须备份。
DBMIG-006：migration 必须记录执行版本、时间、执行人、checksum。
DBMIG-007：破坏性变更必须分阶段执行。
DBMIG-008：字段删除必须先废弃，再观察，再删除。
DBMIG-009：字段改名必须采用新增字段、双写、回填、切读、删除旧字段流程。
DBMIG-010：大表加索引必须评估锁表风险。
DBMIG-011：大表数据回填必须分批。
DBMIG-012：migration 必须可重复 dry run。
DBMIG-013：migration 必须输出影响表、影响行数和耗时。
DBMIG-014：失败 migration 必须可定位、可中断、可恢复。
```

推荐流程：

```text
Expand -> Migrate -> Contract
```

示例：

```text
1. 新增 new_column。
2. 应用双写 old_column + new_column。
3. 后台任务回填 new_column。
4. 应用切读 new_column。
5. 观察稳定。
6. 删除 old_column。
```

### 28.9 发布、灰度、回滚与 Feature Flag 规则

发布不是上传新文件，而是一套可灰度、可观测、可回滚的流程。

```text
RELEASE-001：所有生产发布必须有版本号。
RELEASE-002：所有生产发布必须有变更说明。
RELEASE-003：所有生产发布必须有回滚方案。
RELEASE-004：涉及数据库 migration 的发布必须单独标记。
RELEASE-005：数据库已迁移后的回滚必须有数据兼容策略。
RELEASE-006：高风险功能必须使用 Feature Flag。
RELEASE-007：Feature Flag 默认关闭，灰度验证后逐步开启。
RELEASE-008：Feature Flag 必须有负责人、说明、过期时间。
RELEASE-009：过期 Feature Flag 必须清理。
RELEASE-010：灰度发布必须能按用户、角色、租户或环境控制。
RELEASE-011：生产发布必须保留上一版本可执行文件。
RELEASE-012：静态前端发布必须保留上一版本构建产物。
RELEASE-013：Native AOT 后端发布必须记录 runtime、rid、commit、checksum。
RELEASE-014：前端发布必须记录 build hash 和资源版本。
```

### 28.10 后台高风险操作风控规则

后台最大的风险之一是管理员误操作或高权限账号被滥用。高风险操作必须具备额外确认、审计和可选二次验证。

```text
RISK-OP-001：删除用户、禁用用户、重置密码、重置 2FA 属于高风险操作。
RISK-OP-002：授予超级管理员、修改角色权限、删除角色属于高风险操作。
RISK-OP-003：修改登录安全配置、关闭 2FA、修改 SMTP 密码属于高风险操作。
RISK-OP-004：批量删除、批量导入、批量授权属于高风险操作。
RISK-OP-005：高风险操作必须二次确认。
RISK-OP-006：高风险操作必须记录完整审计。
RISK-OP-007：超级管理员高风险操作必须标记 highRisk=true。
RISK-OP-008：可选支持二次验证，例如重新输入密码或 TOTP。
RISK-OP-009：可选支持四眼审批，尤其是权限和配置变更。
RISK-OP-010：高风险操作失败次数异常必须进入安全事件。
RISK-OP-011：高风险操作必须返回明确结果，不允许静默失败。
RISK-OP-012：高风险操作前端文案必须清楚说明影响范围。
```

可预留表：

```text
sys_risk_operation
sys_operation_approval
```

### 28.11 导入、导出与批量操作规则

导入、导出、批量操作是 CMS 后台最常见的数据破坏与数据泄露入口，必须单独治理。

导入规则：

```text
IMPORT-001：导入文件必须走上传安全规则。
IMPORT-002：导入文件必须限制格式、大小、行数、列数。
IMPORT-003：导入必须先预检，不允许直接写库。
IMPORT-004：导入预检必须返回错误行号和字段。
IMPORT-005：导入执行必须有任务 ID。
IMPORT-006：导入执行必须记录操作人、文件 ID、影响行数。
IMPORT-007：导入失败必须支持错误报告下载。
IMPORT-008：导入不得允许前端传入系统字段，例如 id、created_at、permission_version。
IMPORT-009：导入必须做字段白名单。
IMPORT-010：导入必须限制批量写入事务大小。
IMPORT-011：导入前必须进行权限和数据范围校验。
IMPORT-012：导入模板必须由后端生成或版本化维护。
```

导出规则：

```text
EXPORT-001：导出必须经过权限和数据范围校验。
EXPORT-002：导出大数据必须异步任务处理。
EXPORT-003：导出文件必须有过期时间。
EXPORT-004：导出文件下载必须鉴权。
EXPORT-005：导出字段必须白名单。
EXPORT-006：导出敏感字段必须脱敏或要求额外权限。
EXPORT-007：CSV/Excel 导出必须防公式注入。
EXPORT-008：导出任务必须限流。
EXPORT-009：导出必须记录审计日志。
EXPORT-010：导出文件不得长期保存在公开目录。
```

批量操作规则：

```text
BATCH-001：批量操作必须限制最大数量。
BATCH-002：批量操作必须记录成功数、失败数和失败原因。
BATCH-003：批量操作必须可审计。
BATCH-004：批量删除默认软删除。
BATCH-005：批量授权必须记录变更前后差异。
```

### 28.12 后台任务、队列与调度规则

长耗时工作不得阻塞 HTTP 请求。导入、导出、文件扫描、邮件发送、日志清理、备份检查等必须任务化。

```text
JOB-001：长耗时任务不得在 HTTP 请求中同步执行。
JOB-002：导入、导出、文件扫描、批量通知必须异步任务化。
JOB-003：任务必须有唯一 jobId。
JOB-004：任务必须记录状态：pending、running、success、failed、cancelled。
JOB-005：任务必须记录创建人、开始时间、结束时间、错误信息。
JOB-006：任务重试必须设置最大次数。
JOB-007：任务重试必须指数退避或固定退避。
JOB-008：任务失败后必须进入可查询状态。
JOB-009：任务不得无限重试。
JOB-010：任务执行必须支持取消。
JOB-011：多实例部署时调度任务必须有分布式锁。
JOB-012：任务处理必须幂等。
JOB-013：任务参数不得保存明文 secret。
JOB-014：任务执行日志必须可按 jobId 查询。
JOB-015：任务处理器必须可观测，包括耗时、成功率、失败率。
```

建议预留：

```text
sys_job
sys_job_log
```

### 28.13 多租户预留规则

即使 MVP 不立刻实现多租户，也必须避免数据库、缓存、文件、审计设计把未来演进路径堵死。

```text
TENANT-001：每张表必须标记为 global 表或 tenant-scoped 表。
TENANT-002：tenant-scoped 表未来必须包含 tenant_id。
TENANT-003：系统配置必须区分全局配置和租户配置。
TENANT-004：角色、菜单、权限默认是全局还是租户级必须明确。
TENANT-005：文件存储 key 必须预留租户隔离前缀。
TENANT-006：缓存 key 必须预留 tenantId 维度。
TENANT-007：审计日志必须预留 tenantId。
TENANT-008：后台超级管理员跨租户操作必须记录高风险审计。
TENANT-009：禁止前端传入 tenantId 作为唯一可信来源。
TENANT-010：TenantContext 必须由后端解析。
TENANT-011：多租户启用前必须完成数据隔离威胁建模。
TENANT-012：跨租户查询默认禁止，除非显式使用平台级权限。
```

### 28.14 可观测性、SLO 与告警规则

系统必须可观测。没有指标、追踪和日志的系统，不具备稳定运营能力。

```text
OBS-SLO-001：必须定义核心接口 SLO。
OBS-SLO-002：必须采集 API 延迟 P50/P95/P99。
OBS-SLO-003：必须采集 4xx、5xx、429、403、401 比例。
OBS-SLO-004：必须采集登录失败次数。
OBS-SLO-005：必须采集 WAF 拦截次数。
OBS-SLO-006：必须采集数据库连接池使用情况。
OBS-SLO-007：必须采集慢 SQL 数量。
OBS-SLO-008：必须采集后台任务失败率。
OBS-SLO-009：必须采集文件上传失败率。
OBS-SLO-010：必须采集缓存命中率。
OBS-SLO-011：必须采集权限缓存刷新失败次数。
OBS-SLO-012：必须采集导入导出任务耗时和失败率。
```

告警规则：

```text
ALERT-001：5xx 错误率超过阈值必须告警。
ALERT-002：登录失败异常增长必须告警。
ALERT-003：超级管理员登录失败必须告警或进入高风险事件。
ALERT-004：WAF 拦截突然升高必须告警。
ALERT-005：数据库连接池耗尽必须告警。
ALERT-006：后台任务连续失败必须告警。
ALERT-007：磁盘空间不足必须告警。
ALERT-008：备份失败必须告警。
ALERT-009：审计日志写入失败必须告警。
ALERT-010：权限缓存刷新失败必须告警。
```

### 28.15 事故响应与 Runbook 规则

必须提前定义事故响应流程，而不是故障发生后临时处理。

```text
INCIDENT-001：必须定义安全事件等级。
INCIDENT-002：必须定义生产故障等级。
INCIDENT-003：必须定义响应人和升级路径。
INCIDENT-004：必须为登录异常、权限异常、数据误删、备份失败、WAF 大量拦截提供 Runbook。
INCIDENT-005：必须有强制下线用户流程。
INCIDENT-006：必须有吊销全部 Refresh Token 流程。
INCIDENT-007：必须有禁用某角色流程。
INCIDENT-008：必须有恢复误删用户/菜单/角色流程。
INCIDENT-009：必须有回滚前端静态资源流程。
INCIDENT-010：必须有回滚后端 Native AOT 可执行文件流程。
INCIDENT-011：重大事故必须有复盘报告。
INCIDENT-012：复盘报告必须产生修复任务。
```

Runbook 至少覆盖：

```text
1. 管理员账号泄露。
2. 超级管理员误删。
3. 权限配置错误导致全员无权限。
4. 数据库 migration 失败。
5. WAF 误拦截登录。
6. 文件上传被滥用。
7. 导出任务拖垮数据库。
8. Redis / 缓存不可用。
9. 磁盘满。
10. SMTP 密码泄露。
```

### 28.16 前端状态、缓存与权限展示规则

SoybeanAdmin 前端必须遵守后端契约，不得形成第二套业务事实源。

```text
FE-STATE-001：用户信息、菜单、权限只能来自 /api/auth/me 或后端指定接口。
FE-STATE-002：前端不得自行生成权限。
FE-STATE-003：前端不得缓存过期权限超过 token 生命周期。
FE-STATE-004：登出必须清理 Pinia store、路由、tabs、缓存页面。
FE-STATE-005：切换用户后必须重置动态路由。
FE-STATE-006：401 必须进入重新登录流程。
FE-STATE-007：403 必须显示无权限页面，不允许静默失败。
FE-STATE-008：前端表单校验只是体验，后端校验才是事实。
FE-STATE-009：前端不得绕过后端分页直接一次性拉全量数据。
FE-STATE-010：前端不得在 localStorage 保存敏感权限矩阵以外的 secret。
```

路由规则：

```text
FE-ROUTE-001：动态路由事实源是后端菜单 DTO。
FE-ROUTE-002：前端只做 route adapter。
FE-ROUTE-003：后端 component key 必须在前端组件白名单中。
FE-ROUTE-004：未知 component key 必须降级到 404 或错误页面。
FE-ROUTE-005：路由 meta.permissionCode 不允许前端私自修改。
```

### 28.17 可访问性与后台体验规则

后台核心页面应满足基本可访问性要求，避免纯视觉交互造成操作障碍。

```text
A11Y-001：后台核心页面应满足 WCAG 2.2 AA 的主要交互要求。
A11Y-002：所有按钮必须有可识别文本或 aria-label。
A11Y-003：表单错误必须关联字段。
A11Y-004：弹窗必须支持键盘关闭和焦点管理。
A11Y-005：菜单必须支持键盘导航。
A11Y-006：颜色不能作为唯一状态表达。
A11Y-007：危险操作确认按钮文案必须清晰。
A11Y-008：表格空状态、加载状态、错误状态必须统一。
A11Y-009：深色模式和主题色必须满足基本对比度。
A11Y-010：后台应支持响应式最小宽度策略。
```

### 28.18 架构决策记录 ADR 规则

迁移项目中所有重大技术取舍必须可追溯。

```text
ADR-001：重大技术决策必须写 ADR。
ADR-002：ADR 必须包含背景、决策、替代方案、影响、状态。
ADR-003：已接受 ADR 不得随意推翻。
ADR-004：推翻 ADR 必须新建 supersede ADR。
ADR-005：新依赖、新认证方式、新数据库策略、新部署策略必须有 ADR。
ADR-006：AOT 不兼容变更必须有 ADR 和替代方案。
ADR-007：安全、性能、数据迁移、前后端契约相关决策必须写 ADR。
ADR-008：ADR 必须随代码仓库版本管理。
```

推荐目录：

```text
docs/adr/
  0001-use-dotnet10-native-aot.md
  0002-use-sqlsugar-orm.md
  0003-backend-contract-first.md
  0004-use-soybeanadmin-as-ui-template.md
```

### 28.19 威胁建模与安全评审规则

安全不是上线前扫描一次，而是每个模块设计阶段就必须考虑攻击面。

```text
THREAT-001：每个新模块必须做轻量威胁建模。
THREAT-002：威胁建模必须覆盖认证、授权、输入、输出、文件、日志、数据范围。
THREAT-003：所有带 id 的接口必须列出对象级授权策略。
THREAT-004：所有写接口必须列出 Mass Assignment 防护策略。
THREAT-005：所有导出接口必须列出数据泄露防护策略。
THREAT-006：所有上传接口必须列出恶意文件防护策略。
THREAT-007：所有高风险操作必须列出误操作和恶意操作场景。
THREAT-008：安全评审未通过不得合并。
THREAT-009：AllowAnonymous 接口必须单独安全评审。
THREAT-010：新增外部依赖必须评估供应链风险。
```

### 28.20 供应链安全增强规则

生产制品必须可追溯、可验证、可复现，依赖必须可审计。

```text
SUPPLY-PLUS-001：必须生成后端 SBOM。
SUPPLY-PLUS-002：必须生成前端 SBOM。
SUPPLY-PLUS-003：生产制品必须有 checksum。
SUPPLY-PLUS-004：生产制品必须可追溯到 commit。
SUPPLY-PLUS-005：CI 构建环境必须固定。
SUPPLY-PLUS-006：依赖升级必须经过自动化测试。
SUPPLY-PLUS-007：高危漏洞依赖不得发布。
SUPPLY-PLUS-008：新增 NuGet 包必须审核 AOT 兼容性、License、维护状态。
SUPPLY-PLUS-009：新增 npm 包必须审核 License、维护状态、下载来源。
SUPPLY-PLUS-010：禁止直接复制未知来源安全代码。
SUPPLY-PLUS-011：构建密钥不得写入仓库、镜像或构建日志。
SUPPLY-PLUS-012：发布包必须保留构建日志和依赖清单。
```

### 28.21 SqlSugar ORM 深度约束规则

SqlSugar ORM 不支持所有 SqlSugar ORM 动态能力，因此必须从编码规范上规避不兼容模式。

```text
sqlsugar-orm-PLUS-001：所有查询方法必须使用具体 DTO 泛型。
sqlsugar-orm-PLUS-002：禁止使用 dynamic 参数和 dynamic 返回。
sqlsugar-orm-PLUS-003：禁止运行时决定返回类型。
sqlsugar-orm-PLUS-004：禁止多映射复杂魔法查询进入核心路径。
sqlsugar-orm-PLUS-005：多表聚合优先使用显式 DTO。
sqlsugar-orm-PLUS-006：所有 SQL 文件或 SQL 字符串必须纳入 Review。
sqlsugar-orm-PLUS-007：SQL 变更必须有 explain 或索引评估。
sqlsugar-orm-PLUS-008：Repository 方法必须有集成测试覆盖。
sqlsugar-orm-PLUS-009：Repository 方法必须验证 AOT publish。
sqlsugar-orm-PLUS-010：SqlSugar ORM warning 不允许忽略。
sqlsugar-orm-PLUS-011：禁止非泛型传入运行时 Type 的查询模式。
sqlsugar-orm-PLUS-012：查询 DTO 字段必须与 SQL alias 明确对应。
```

### 28.22 代理、真实 IP 与边界规则

反向代理是安全边界的一部分。真实 IP、Host、Forwarded Headers 必须严格处理。

```text
EDGE-001：应用必须配置 AllowedHosts。
EDGE-002：生产必须启用 HTTPS。
EDGE-003：HTTP 到 HTTPS 重定向必须在代理和应用层配置一致。
EDGE-004：Forwarded Headers 只信任 KnownProxies / KnownNetworks。
EDGE-005：禁止直接信任客户端 X-Forwarded-For。
EDGE-006：Nginx 必须设置 request body 大小限制。
EDGE-007：Nginx 必须设置 header 超时、body 超时、keepalive 策略。
EDGE-008：Kestrel 不直接暴露公网。
EDGE-009：管理后台域名与 API 域名必须明确配置。
EDGE-010：OpenAPI 文档生产环境默认关闭或仅管理员可见。
EDGE-011：后台管理端建议单独域名和访问策略。
EDGE-012：源站不得绕过 WAF / Nginx 直接访问。
```

### 28.23 HTTP Logging 与日志采样规则

HTTP Logging 必须谨慎，尤其是请求体和响应体。生产环境默认关闭 body 日志。

```text
HTTPLOG-001：生产环境默认不记录请求 body。
HTTPLOG-002：生产环境默认不记录响应 body。
HTTPLOG-003：调试 body 日志必须有白名单接口。
HTTPLOG-004：body 日志必须有大小限制。
HTTPLOG-005：body 日志必须经过脱敏。
HTTPLOG-006：认证、Token、密码、2FA 接口禁止 body 日志。
HTTPLOG-007：文件上传接口禁止 body 日志。
HTTPLOG-008：日志采样策略必须可配置。
HTTPLOG-009：traceId 必须贯穿应用日志、审计日志、WAF 日志。
HTTPLOG-010：生产环境 Debug 日志默认关闭。
HTTPLOG-011：安全事件日志不得采样丢弃。
HTTPLOG-012：审计日志不得采样丢弃。
```

### 28.24 不建议引入的过度设计

为了避免规则本身拖慢项目，以下内容不建议 MVP 强制引入：

```text
1. 不建议一开始强制微服务拆分。
2. 不建议一开始强制事件溯源 Event Sourcing。
3. 不建议一开始强制 CQRS 全量拆分。
4. 不建议一开始强制复杂审批流。
5. 不建议一开始强制多租户落地，除非商业目标明确。
6. 不建议应用层自研复杂 WAF。
7. 不建议为了规则而把每个简单 CRUD 都过度设计。
8. 不建议前端为了追求模板一致而反向约束后端契约。
```

当前最适合的工程路线：

```text
模块化单体
明确契约
强权限
强审计
强迁移
可灰度
可回滚
可观测
可平台化
```

### 28.25 本章最终结论

```text
第 27 章解决“能不能安全上线”；
第 28 章解决“能不能长期维护、灰度演进、稳定运营、避免业务数据出错”。
```

新增模块必须同时满足：

```text
1. API 生命周期可管理。
2. 错误模型统一。
3. 事务边界清楚。
4. 幂等与并发可控。
5. 业务不变量沉入后端。
6. 数据生命周期明确。
7. 数据库迁移可演练。
8. 发布可灰度可回滚。
9. 高风险操作可审计可风控。
10. 导入导出可控。
11. 后台任务可观测。
12. 多租户路径不被堵死。
13. 事故响应有 Runbook。
14. 前端状态不产生第二事实源。
15. 安全评审和 ADR 可追溯。
```

***

***

## 29. 基础系统能力与交付闭环要求

### 29.1 章节目标

第 27 章解决安全、性能、WAF、AOT 与上线门禁；第 28 章解决高级工程治理、版本演进、事务、幂等、灰度、事故响应等长期治理问题。

第 29 章用于回答基础 CMS 系统落地时必须明确的问题：

```text
1. 基础系统第一版到底交付哪些模块。
2. 新系统第一次部署如何初始化、如何创建超级管理员、如何恢复管理员账号。
3. 系统配置、密钥、账号找回、验证码、组织架构、通知、任务、CMS 内容模块如何落地。
4. SoybeanAdmin 前端必须交付哪些页面、按钮、路由和权限展示能力。
5. 表结构、API、权限矩阵、错误码、字典、演示数据、运维手册如何形成交付闭环。
```

本章是基础系统建设蓝图，所有模块开发、测试、验收、上线和运维接管均应以本章为交付依据。

***

### 29.2 基础系统 MVP 功能边界

#### 29.2.1 P0：基础系统必须交付

P0 功能是新系统可被称为“基础 CMS 后台”的最低交付边界，不得裁剪。

```text
BASE-P0-001：登录、退出、刷新 Token 必须完成。
BASE-P0-002：当前用户接口 /api/auth/me 必须完成。
BASE-P0-003：用户管理必须完成。
BASE-P0-004：角色管理必须完成。
BASE-P0-005：菜单管理必须完成。
BASE-P0-006：权限管理必须完成。
BASE-P0-007：角色分配菜单必须完成。
BASE-P0-008：角色分配权限必须完成。
BASE-P0-009：用户分配角色必须完成。
BASE-P0-010：系统配置必须完成。
BASE-P0-011：字典管理必须完成。
BASE-P0-012：文件上传、下载、预览、删除必须完成。
BASE-P0-013：登录日志必须完成。
BASE-P0-014：操作审计日志必须完成。
BASE-P0-015：安全事件日志必须完成。
BASE-P0-016：2FA 绑定、验证、禁用、重置必须完成。
BASE-P0-017：修改密码必须完成。
BASE-P0-018：管理员重置用户密码必须完成。
BASE-P0-019：SoybeanAdmin 动态路由必须完成。
BASE-P0-020：按钮权限控制必须完成。
BASE-P0-021：OpenAPI 契约生成必须完成。
BASE-P0-022：前端 TypeScript 类型生成必须完成。
BASE-P0-023：基础初始化与种子数据必须完成。
BASE-P0-024：Native AOT 发布门禁必须完成。
```

#### 29.2.2 P1：基础系统建议交付

P1 功能是基础系统从“可用”进入“可运营、可维护”的必要能力。

```text
BASE-P1-001：组织架构必须规划，建议第一版交付。
BASE-P1-002：部门管理建议第一版交付。
BASE-P1-003：岗位管理建议第一版交付。
BASE-P1-004：数据权限建议第一版交付，至少完成模型预留。
BASE-P1-005：通知公告建议第一版交付。
BASE-P1-006：邮件发送配置建议第一版交付。
BASE-P1-007：站内信建议第一版交付。
BASE-P1-008：后台任务与任务日志建议第一版交付。
BASE-P1-009：安全中心建议第一版交付。
BASE-P1-010：在线用户 / 会话管理建议第一版交付。
BASE-P1-011：文件引用关系建议第一版交付。
BASE-P1-012：导入导出基础能力建议第一版交付。
BASE-P1-013：系统维护页面建议第一版交付。
```

#### 29.2.3 P2：CMS 业务增强

P2 功能是 CMS 业务能力，不一定全部纳入系统底座第一阶段，但必须在架构和表设计中预留扩展路径。

```text
BASE-P2-001：栏目管理。
BASE-P2-002：文章管理。
BASE-P2-003：单页管理。
BASE-P2-004：标签管理。
BASE-P2-005：媒体库。
BASE-P2-006：内容发布。
BASE-P2-007：内容审核。
BASE-P2-008：内容版本。
BASE-P2-009：内容回收站。
BASE-P2-010：SEO 配置。
BASE-P2-011：公开站点 API。
BASE-P2-012：内容预览。
```

#### 29.2.4 MVP 边界硬约束

```text
BASE-SCOPE-001：基础系统 MVP 不能只实现用户、角色、菜单、权限。
BASE-SCOPE-002：配置、字典、文件、日志、安全中心、初始化能力必须纳入基础交付。
BASE-SCOPE-003：如果系统对外命名为 CMS，必须至少定义栏目、文章、媒体、页面的后续建设边界。
BASE-SCOPE-004：没有初始化、没有超级管理员恢复、没有权限矩阵的系统不得进入生产验收。
```

***

### 29.3 初始化安装、种子数据与超级管理员规则

新系统第一次部署必须能够完成数据库初始化、基础数据写入、超级管理员创建、权限同步和健康检查。

#### 29.3.1 初始化命令

Native AOT Only 场景下，建议使用同一个可执行文件提供受控任务入口：

```bash
WeCms.Api --task migrate-up
WeCms.Api --task seed-base
WeCms.Api --task sync-permissions
WeCms.Api --task reset-admin
WeCms.Api --task health-check
```

也可以提供独立 CLI 项目，但该项目同样必须通过 Native AOT 发布验证。

#### 29.3.2 初始化硬规则

```text
BOOT-001：系统必须提供数据库初始化脚本。
BOOT-002：系统必须提供基础种子数据脚本。
BOOT-003：系统必须初始化超级管理员角色。
BOOT-004：系统必须初始化超级管理员账号。
BOOT-005：系统必须初始化基础菜单。
BOOT-006：系统必须初始化基础权限码。
BOOT-007：系统必须初始化基础系统配置。
BOOT-008：系统必须初始化基础字典。
BOOT-009：系统必须提供权限同步命令。
BOOT-010：系统必须提供重建超级管理员的安全工具。
BOOT-011：重建超级管理员工具只能在受控环境执行。
BOOT-012：初始化密码不得写死在代码或 SQL 文件中。
BOOT-013：首次登录必须强制修改初始密码。
BOOT-014：首次登录可强制绑定 2FA。
BOOT-015：初始化完成后必须记录系统事件。
BOOT-016：初始化脚本必须可重复执行，重复执行不得破坏已有生产数据。
BOOT-017：基础种子数据与演示种子数据必须分离。
BOOT-018：生产环境不得导入 demo seed。
BOOT-019：初始化过程必须输出执行报告。
BOOT-020：初始化失败必须安全退出，不得留下半初始化账号或半初始化权限矩阵。
```

#### 29.3.3 基础种子数据范围

```text
1. super_admin 角色。
2. security_admin 角色。
3. system_admin 角色。
4. content_admin 角色。
5. viewer 角色。
6. 系统管理菜单。
7. 用户管理菜单。
8. 角色管理菜单。
9. 菜单管理菜单。
10. 权限管理菜单。
11. 配置管理菜单。
12. 字典管理菜单。
13. 文件管理菜单。
14. 日志管理菜单。
15. 安全中心菜单。
16. 基础权限码。
17. 基础字典类型。
18. 基础系统配置。
```

***

### 29.4 系统配置、密钥与加密材料治理

系统配置必须区分普通配置、敏感配置、运行时配置、构建期配置和密钥材料。

#### 29.4.1 配置分类

| 类型    | 示例                          | 存储建议                | 是否可从后台编辑 |
| ----- | --------------------------- | ------------------- | -------- |
| 普通配置  | 站点标题、分页默认值                  | `sys_setting`       | 可以       |
| 敏感配置  | SMTP 密码、第三方 Secret          | Secret Store / 加密引用 | 仅脱敏展示    |
| 运行时配置 | 日志级别、限流阈值                   | 配置文件 / 环境变量 / 数据库   | 部分可以     |
| 构建期配置 | AOT 发布目标、前端构建参数             | CI/CD               | 不可后台编辑   |
| 密钥材料  | JWT Key、Data Protection Key | Secret Store / 安全卷  | 不可直接展示   |

#### 29.4.2 密钥治理规则

```text
SECRET-001：任何 secret 不得写入 Git。
SECRET-002：任何 secret 不得写入普通 appsettings.json。
SECRET-003：开发环境可使用 user-secrets 或本地安全配置。
SECRET-004：生产环境必须使用环境变量、密钥服务或受控 Secret Store。
SECRET-005：JWT 签名密钥必须支持轮换。
SECRET-006：Refresh Token 哈希 pepper 如存在，必须作为 secret 管理。
SECRET-007：SMTP 密码必须加密保存或托管到 Secret Store。
SECRET-008：Data Protection Key 必须持久化。
SECRET-009：Data Protection Key 不得随容器销毁而丢失。
SECRET-010：密钥轮换必须有 Runbook。
SECRET-011：密钥泄露必须有应急吊销流程。
SECRET-012：配置变更必须审计。
SECRET-013：敏感配置读取必须有权限控制。
SECRET-014：敏感配置返回前端时必须脱敏。
SECRET-015：敏感配置不得进入普通 SQL dump。
SECRET-016：敏感配置不得进入日志、审计详情、错误响应。
SECRET-017：配置缓存中不得保存明文 secret。
SECRET-018：密钥引用必须可以定位 provider、key、version。
```

#### 29.4.3 建议表结构

```text
sys_setting_group
  id, code, name, sort, status, remark, created_at, updated_at

sys_setting
  id, group_id, key, value, value_type, is_sensitive, is_system,
  description, sort, status, created_at, updated_at, row_version

sys_secret_reference
  id, setting_key, provider, secret_key, secret_version,
  description, created_at, updated_at

sys_setting_change_log
  id, setting_key, old_value_masked, new_value_masked,
  changed_by, changed_at, reason, request_id
```

***

### 29.5 账号自助、密码找回与会话管理

基础后台必须支持账号安全闭环，包括忘记密码、密码重置、会话管理、强制下线和账号解锁。

#### 29.5.1 必备接口

```text
POST   /api/auth/password/forgot
POST   /api/auth/password/reset
GET    /api/auth/sessions
DELETE /api/auth/sessions/{id}
DELETE /api/auth/sessions
POST   /api/system/users/{id}/force-logout
POST   /api/system/users/{id}/unlock
POST   /api/system/users/{id}/password/reset
```

#### 29.5.2 账号安全规则

```text
ACCOUNT-001：密码找回接口不得泄露账号是否存在。
ACCOUNT-002：密码找回 token 必须使用安全随机数。
ACCOUNT-003：密码找回 token 数据库只保存 hash。
ACCOUNT-004：密码找回 token 必须有短有效期。
ACCOUNT-005：密码找回 token 使用后必须立即失效。
ACCOUNT-006：密码找回成功后必须吊销旧 refresh token。
ACCOUNT-007：密码找回成功后必须记录安全事件。
ACCOUNT-008：管理员重置密码必须要求用户下次登录修改密码。
ACCOUNT-009：管理员重置密码必须吊销用户所有会话。
ACCOUNT-010：用户必须能查看自己的活跃会话。
ACCOUNT-011：用户必须能吊销自己的其他会话。
ACCOUNT-012：管理员必须能强制下线指定用户。
ACCOUNT-013：超级管理员被强制下线必须记录高风险审计。
ACCOUNT-014：账号锁定必须记录锁定原因、锁定时间和解锁人。
ACCOUNT-015：账号解锁必须记录审计日志。
ACCOUNT-016：密码重置链接不得在日志中记录完整 URL。
ACCOUNT-017：同一账号短时间内密码找回请求必须限流。
ACCOUNT-018：同一 IP 短时间内密码找回请求必须限流。
```

#### 29.5.3 建议表结构

```text
sys_password_reset_token
  id, user_id, token_hash, expires_at, used_at, created_ip,
  user_agent, created_at

sys_user_session
  id, user_id, refresh_token_id, device_name, ip, user_agent,
  last_seen_at, revoked_at, revoked_reason, created_at

sys_account_lock
  id, user_id, reason, locked_until, failed_count,
  locked_by, unlocked_by, unlocked_at, created_at
```

***

### 29.6 验证码与人机校验规则

#### 29.6.1 必备接口

```text
GET  /api/auth/captcha
POST /api/auth/captcha/verify
```

#### 29.6.2 验证码规则

```text
CAPTCHA-001：登录失败达到阈值后启用验证码。
CAPTCHA-002：密码找回必须启用验证码或等效人机校验。
CAPTCHA-003：验证码必须有有效期。
CAPTCHA-004：验证码使用后必须失效。
CAPTCHA-005：验证码错误次数必须限流。
CAPTCHA-006：验证码不得长期存储明文答案。
CAPTCHA-007：验证码接口必须防刷。
CAPTCHA-008：验证码策略必须可配置。
CAPTCHA-009：验证码错误不得泄露内部实现细节。
CAPTCHA-010：验证码验证失败必须进入安全计数。
```

***

### 29.7 组织架构、部门、岗位与数据权限支撑

如果系统启用 `data_scope`，必须提供组织架构支撑，否则 `dept`、`dept_tree`、`custom` 等数据权限无法真正落地。

#### 29.7.1 建议模块

```text
Dept       部门管理
Post       岗位管理
UserDept   用户部门关系
RoleDept   角色自定义数据范围
```

#### 29.7.2 建议表结构

```text
sys_dept
  id, parent_id, code, name, leader_user_id, phone, email,
  sort, status, created_at, updated_at, deleted_at, row_version

sys_post
  id, code, name, sort, status, remark, created_at, updated_at, deleted_at

sys_user_dept
  user_id, dept_id, is_primary, created_at

sys_role_dept
  role_id, dept_id, created_at
```

#### 29.7.3 必备接口

```text
GET    /api/system/depts/tree
POST   /api/system/depts
PUT    /api/system/depts/{id}
DELETE /api/system/depts/{id}
PATCH  /api/system/depts/{id}/status
PATCH  /api/system/depts/sort

GET    /api/system/posts
POST   /api/system/posts
PUT    /api/system/posts/{id}
DELETE /api/system/posts/{id}
PATCH  /api/system/posts/{id}/status
```

#### 29.7.4 组织架构规则

```text
ORG-001：如果启用 data_scope，必须启用组织架构模块。
ORG-002：部门树不得形成循环。
ORG-003：部门树必须限制最大深度。
ORG-004：禁用部门时必须检查下级部门和用户。
ORG-005：删除部门必须检查用户引用。
ORG-006：岗位 code 创建后不得随意修改。
ORG-007：用户可属于一个主部门，也可扩展多部门。
ORG-008：数据权限过滤必须基于后端 CurrentUserContext。
ORG-009：前端传入 deptId 不得作为可信数据权限来源。
ORG-010：跨部门查询必须记录数据权限上下文。
ORG-011：部门删除默认软删除。
ORG-012：部门排序必须使用事务并处理并发覆盖。
ORG-013：角色自定义数据范围必须保存到 sys_role_dept。
ORG-014：超级管理员绕过数据权限必须进入高风险审计上下文。
```

***

### 29.8 通知、公告、邮件与站内信模块

旧 ThinkPHP 系统中已有 `notice`、`mail_notify`、`msg_sender` 等相关数据表或业务痕迹。新系统应将其重构为通知中心能力。

#### 29.8.1 建议模块

```text
Notice              通知公告
Message             站内信
MailTemplate        邮件模板
MailOutbox          邮件发送箱
NotificationEvent   通知事件
```

#### 29.8.2 建议表结构

```text
sys_notice
  id, title, content, status, target_type, target_value,
  published_at, withdrawn_at, created_by, updated_by,
  created_at, updated_at, deleted_at

sys_message
  id, title, content, type, source_module, source_id,
  sender_id, created_at

sys_message_receiver
  id, message_id, receiver_user_id, read_at, deleted_at, created_at

sys_mail_template
  id, code, name, subject_template, body_template,
  variables_json, status, created_at, updated_at

sys_mail_outbox
  id, template_code, to_email, subject, body, status,
  retry_count, next_retry_at, sent_at, error_message, created_at

sys_notification_event
  id, event_type, source_module, source_id, payload_json,
  status, created_at
```

#### 29.8.3 必备接口

```text
GET  /api/system/notices
POST /api/system/notices
PUT  /api/system/notices/{id}
POST /api/system/notices/{id}/publish
POST /api/system/notices/{id}/withdraw

GET  /api/system/messages
POST /api/system/messages/{id}/read
POST /api/system/messages/read-all

GET  /api/system/mail-templates
PUT  /api/system/mail-templates/{id}
GET  /api/system/mail-outbox
POST /api/system/mail-outbox/{id}/retry
```

#### 29.8.4 通知规则

```text
NOTIFY-001：公告必须支持草稿、发布、下架。
NOTIFY-002：公告必须支持接收范围。
NOTIFY-003：站内信必须支持已读/未读。
NOTIFY-004：系统通知必须记录来源模块。
NOTIFY-005：邮件发送必须走 outbox 表。
NOTIFY-006：邮件发送失败必须支持重试。
NOTIFY-007：邮件模板变量必须白名单。
NOTIFY-008：邮件正文不得泄露敏感 token。
NOTIFY-009：通知发送必须记录审计或发送日志。
NOTIFY-010：批量通知必须异步任务化。
NOTIFY-011：邮件模板修改必须记录审计。
NOTIFY-012：站内信删除只影响接收方视图，不物理删除原消息。
```

***

### 29.9 后台任务、系统维护与清理任务

第 28 章定义任务治理规则，本节定义基础系统第一版必须具备的维护任务。

#### 29.9.1 基础维护任务清单

```text
MAINT-JOB-001：清理过期 refresh token。
MAINT-JOB-002：清理过期 password reset token。
MAINT-JOB-003：清理验证码记录。
MAINT-JOB-004：清理临时上传文件。
MAINT-JOB-005：清理过期导出文件。
MAINT-JOB-006：清理过期安全封禁记录。
MAINT-JOB-007：归档登录日志。
MAINT-JOB-008：归档操作日志。
MAINT-JOB-009：检查邮件 outbox 重试。
MAINT-JOB-010：检查失败任务重试。
MAINT-JOB-011：生成系统健康快照。
MAINT-JOB-012：权限缓存一致性检查。
```

#### 29.9.2 建议表结构

```text
sys_job
  id, code, name, type, status, cron_expression,
  last_run_at, next_run_at, enabled, created_at, updated_at

sys_job_log
  id, job_id, job_code, status, started_at, finished_at,
  elapsed_ms, error_message, result_json, created_at

sys_system_maintenance_log
  id, action, target, status, operator_id, detail_json,
  created_at, request_id
```

#### 29.9.3 系统维护规则

```text
MAINT-001：系统必须提供任务列表页面。
MAINT-002：系统必须提供任务执行日志。
MAINT-003：系统必须提供手动触发受控任务能力。
MAINT-004：高风险维护任务必须二次确认。
MAINT-005：缓存清理必须按 namespace 控制。
MAINT-006：禁止提供“一键清空全部缓存”给普通管理员。
MAINT-007：日志清理必须保留最小审计周期。
MAINT-008：系统维护任务失败必须告警或进入安全事件。
MAINT-009：清理任务必须支持 dry run。
MAINT-010：清理任务必须记录影响行数。
```

***

### 29.10 CMS 内容基础模块

如果系统命名为 CMS，新项目必须至少定义基础内容模型。第一阶段可以先完成系统管理底座，但表结构、权限码和接口规划必须预留 CMS 内容模块。

#### 29.10.1 基础 CMS 模块

```text
Channel     栏目管理
Article     文章管理
Page        单页管理
Tag         标签管理
Media       媒体库
Link        友情链接
RecycleBin  内容回收站
```

#### 29.10.2 建议表结构

```text
cms_channel
  id, parent_id, name, slug, description, sort, status,
  seo_title, seo_keywords, seo_description,
  created_by, updated_by, created_at, updated_at, deleted_at

cms_article
  id, channel_id, title, slug, summary, cover_file_id,
  status, author_id, published_at, offline_at,
  view_count, sort, created_by, updated_by,
  created_at, updated_at, deleted_at, row_version

cms_article_content
  article_id, content_html, content_text, content_markdown,
  updated_at

cms_article_tag
  article_id, tag_id, created_at

cms_tag
  id, name, slug, status, created_at, updated_at

cms_page
  id, title, slug, content_html, status, published_at,
  seo_title, seo_keywords, seo_description,
  created_by, updated_by, created_at, updated_at, deleted_at

cms_media
  id, file_id, folder_id, title, alt_text, description,
  status, created_by, created_at, updated_at

cms_link
  id, title, url, logo_file_id, sort, status, created_at, updated_at

cms_content_revision
  id, content_type, content_id, revision_no, snapshot_json,
  created_by, created_at

cms_content_publish_log
  id, content_type, content_id, action, reason,
  operator_id, operated_at

cms_content_recycle
  id, content_type, content_id, deleted_by, deleted_at,
  restore_by, restored_at, purge_at
```

#### 29.10.3 内容状态

```text
draft
pending_review
published
offline
rejected
archived
deleted
```

#### 29.10.4 CMS 权限码

```text
cms:channel:list
cms:channel:create
cms:channel:update
cms:channel:delete
cms:article:list
cms:article:create
cms:article:update
cms:article:delete
cms:article:publish
cms:article:offline
cms:article:review
cms:article:restore
cms:page:list
cms:page:create
cms:page:update
cms:page:publish
cms:media:list
cms:media:upload
cms:media:delete
```

#### 29.10.5 CMS 规则

```text
CMS-001：栏目树不得形成循环。
CMS-002：栏目必须支持排序。
CMS-003：文章必须归属栏目。
CMS-004：文章标题、slug、状态必须有明确约束。
CMS-005：已发布内容修改必须生成新版本或重新发布记录。
CMS-006：文章删除默认进入回收站。
CMS-007：回收站恢复必须记录审计。
CMS-008：内容发布必须记录发布人和发布时间。
CMS-009：内容下架必须记录原因。
CMS-010：内容正文必须经过 XSS 清洗策略。
CMS-011：富文本中的附件必须建立引用关系。
CMS-012：删除媒体前必须检查引用关系。
CMS-013：公开内容 API 与后台管理 API 必须分离。
CMS-014：公开内容 API 不得返回后台字段。
CMS-015：公开内容 API 必须有缓存策略。
```

***

### 29.11 内容发布、版本、草稿与回收站规则

```text
CONTENT-STATE-001：内容状态必须由后端状态机控制。
CONTENT-STATE-002：前端不得直接传任意状态值。
CONTENT-STATE-003：draft 只能由作者或有权限用户编辑。
CONTENT-STATE-004：published 内容修改必须生成版本记录。
CONTENT-STATE-005：offline 内容重新发布必须记录发布日志。
CONTENT-STATE-006：deleted 内容只进入回收站，不立即物理删除。
CONTENT-STATE-007：回收站清理必须是维护任务。
CONTENT-STATE-008：内容审核功能可后续开启，但状态字段必须预留。
CONTENT-STATE-009：内容状态流转失败必须返回明确错误码。
CONTENT-STATE-010：内容发布、下架、恢复必须记录审计。
```

建议状态流转：

```text
draft -> pending_review -> published -> offline
      -> published
pending_review -> rejected -> draft
published -> deleted
offline -> deleted
deleted -> draft
```

***

### 29.12 媒体库、附件引用与资源治理

#### 29.12.1 建议表结构

```text
sys_file_reference
  id, file_id, ref_module, ref_type, ref_id,
  field_name, created_by, created_at

cms_media_folder
  id, parent_id, name, sort, created_by, created_at, updated_at

cms_media_usage
  id, media_id, content_type, content_id, usage_type, created_at
```

#### 29.12.2 媒体治理规则

```text
MEDIA-001：文件必须区分 private、public、protected。
MEDIA-002：后台上传默认 private。
MEDIA-003：CMS 公开图片必须通过受控公开资源策略生成。
MEDIA-004：富文本插入图片必须登记 file_reference。
MEDIA-005：删除文件必须检查引用关系。
MEDIA-006：未引用临时文件必须定期清理。
MEDIA-007：媒体库必须支持文件夹或标签分类。
MEDIA-008：媒体库必须支持 alt 文本。
MEDIA-009：图片应生成缩略图。
MEDIA-010：公开媒体 URL 不得暴露物理路径。
MEDIA-011：公开媒体访问必须支持缓存策略。
MEDIA-012：受保护媒体访问必须鉴权或使用短期签名。
```

***

### 29.13 站点配置、SEO 与公开访问规则

#### 29.13.1 建议配置项

```text
site_title
site_description
site_keywords
site_logo_file_id
site_favicon_file_id
site_copyright
site_icp
site_timezone
site_default_locale
site_public_base_url
site_admin_base_url
seo_title_template
seo_description_template
```

#### 29.13.2 建议表结构

```text
cms_site
  id, code, name, domain, status, created_at, updated_at

cms_site_setting
  id, site_id, key, value, value_type, is_sensitive,
  created_at, updated_at

cms_seo_setting
  id, site_id, route_pattern, title_template,
  keywords_template, description_template, created_at, updated_at
```

#### 29.13.3 站点与公开访问规则

```text
SITE-001：系统配置和站点配置必须区分。
SITE-002：后台管理配置不得暴露给公开内容 API。
SITE-003：SEO 字段必须支持页面级覆盖。
SITE-004：slug 必须唯一或在栏目范围内唯一。
SITE-005：公开 API 必须有缓存和限流。
SITE-006：公开 API 不使用后台权限模型，但必须有公开访问策略。
SITE-007：预览接口必须鉴权或使用短期签名。
SITE-008：公开 API 不得返回 created_by、updated_by、deleted_at 等后台字段。
SITE-009：站点域名配置变更必须记录审计。
SITE-010：公开 API 与管理 API 必须使用不同路由分组。
```

***

### 29.14 API 文档、OpenAPI 与前端类型生成

后端契约优先必须落地为可执行的生成链路。

#### 29.14.1 契约产物

```text
artifacts/openapi/wecms-api-v1.json
frontend/src/service/generated
docs/api/error-codes.md
docs/api/permission-codes.md
docs/api/api-changelog.md
```

#### 29.14.2 OpenAPI 规则

```text
OPENAPI-001：后端必须生成 OpenAPI 文档。
OPENAPI-002：OpenAPI 必须参与 CI。
OPENAPI-003：OpenAPI 文件必须作为构建产物。
OPENAPI-004：前端 TypeScript 类型必须由 OpenAPI 生成。
OPENAPI-005：OpenAPI diff 发现破坏性变更必须失败。
OPENAPI-006：生产环境 OpenAPI UI 默认关闭或仅管理员可访问。
OPENAPI-007：每个 Endpoint 必须有 summary。
OPENAPI-008：每个 Endpoint 必须定义响应类型。
OPENAPI-009：每个错误码必须有文档。
OPENAPI-010：每个权限码必须有文档。
OPENAPI-011：每个分页接口必须在 OpenAPI 中体现分页结构。
OPENAPI-012：每个上传接口必须体现文件大小和类型限制。
```

***

### 29.15 SoybeanAdmin 页面交付清单

#### 29.15.1 P0 页面

```text
FE-PAGE-001：登录页必须完成。
FE-PAGE-002：2FA 验证页必须完成。
FE-PAGE-003：个人中心必须完成。
FE-PAGE-004：修改密码页必须完成。
FE-PAGE-005：2FA 绑定页必须完成。
FE-PAGE-006：用户管理页必须完成。
FE-PAGE-007：角色管理页必须完成。
FE-PAGE-008：菜单管理页必须完成。
FE-PAGE-009：权限管理页必须完成。
FE-PAGE-010：系统配置页必须完成。
FE-PAGE-011：字典管理页必须完成。
FE-PAGE-012：文件管理页必须完成。
FE-PAGE-013：登录日志页必须完成。
FE-PAGE-014：操作日志页必须完成。
FE-PAGE-015：安全事件页必须完成。
FE-PAGE-016：无权限页必须完成。
FE-PAGE-017：404 页必须完成。
FE-PAGE-018：500 页必须完成。
```

#### 29.15.2 P1 页面

```text
FE-PAGE-P1-001：部门管理页。
FE-PAGE-P1-002：岗位管理页。
FE-PAGE-P1-003：通知公告页。
FE-PAGE-P1-004：站内信页。
FE-PAGE-P1-005：邮件模板页。
FE-PAGE-P1-006：任务管理页。
FE-PAGE-P1-007：系统维护页。
FE-PAGE-P1-008：在线会话页。
FE-PAGE-P1-009：安全策略页。
FE-PAGE-P1-010：CMS 栏目管理页。
FE-PAGE-P1-011：CMS 文章管理页。
FE-PAGE-P1-012：CMS 媒体库页。
```

#### 29.15.3 页面交付验收规则

```text
FE-DELIVERY-001：每个页面必须有加载态、空状态、错误态。
FE-DELIVERY-002：每个列表页必须支持分页。
FE-DELIVERY-003：每个搜索项必须与后端查询 DTO 对齐。
FE-DELIVERY-004：每个写操作按钮必须绑定权限码。
FE-DELIVERY-005：每个高风险操作必须二次确认。
FE-DELIVERY-006：每个表单字段必须与后端 DTO 对齐。
FE-DELIVERY-007：每个页面不得直接使用 mock 数据作为正式数据源。
FE-DELIVERY-008：页面不得私自改后端字段名。
```

***

### 29.16 权限矩阵与按钮级权限清单

权限矩阵必须成为交付产物。

#### 29.16.1 权限矩阵文件

```text
docs/security/permission-matrix.md
```

#### 29.16.2 权限矩阵格式

| 模块   | 页面   | 动作 | 权限码                          | API                                      |
| ---- | ---- | -- | ---------------------------- | ---------------------------------------- |
| 用户管理 | 用户列表 | 新增 | `sys:user:create`            | `POST /api/system/users`                 |
| 用户管理 | 用户列表 | 编辑 | `sys:user:update`            | `PUT /api/system/users/{id}`             |
| 用户管理 | 用户列表 | 删除 | `sys:user:delete`            | `DELETE /api/system/users/{id}`          |
| 角色管理 | 角色列表 | 授权 | `sys:role:assign-permission` | `PUT /api/system/roles/{id}/permissions` |
| 菜单管理 | 菜单树  | 排序 | `sys:menu:sort`              | `PATCH /api/system/menus/sort`           |

#### 29.16.3 权限矩阵规则

```text
PERM-MATRIX-001：每个菜单必须登记对应权限码。
PERM-MATRIX-002：每个按钮必须登记对应权限码。
PERM-MATRIX-003：每个写接口必须登记对应权限码。
PERM-MATRIX-004：权限矩阵必须进入版本管理。
PERM-MATRIX-005：权限矩阵变更必须触发权限同步。
PERM-MATRIX-006：前端按钮权限必须来自权限矩阵和后端 permissions。
PERM-MATRIX-007：权限矩阵与 sys_permission 不一致时 CI 必须失败。
PERM-MATRIX-008：权限码废弃必须标注替代权限码和迁移方案。
```

***

### 29.17 错误码、字典、枚举与状态码清单

#### 29.17.1 必备文档

```text
docs/api/error-codes.md
docs/domain/enums.md
docs/domain/status-codes.md
docs/domain/dictionaries.md
```

#### 29.17.2 错误码分段

```text
0        success
40000    validation
40100    authentication
40300    authorization
40400    resource
40900    conflict
42900    rate limit
50000    system
60000    file
70000    cms
80000    migration
90000    integration
```

#### 29.17.3 清单规则

```text
CATALOG-001：错误码必须分段。
CATALOG-002：错误码不得重复。
CATALOG-003：错误码不得被前端私自定义。
CATALOG-004：枚举值必须以后端定义为准。
CATALOG-005：字典 code 创建后不得随意修改。
CATALOG-006：状态流转必须由后端定义。
CATALOG-007：前端只渲染枚举和状态，不决定状态含义。
CATALOG-008：字典项被业务引用后不得硬删除。
CATALOG-009：枚举新增必须同步 OpenAPI 和前端类型。
CATALOG-010：状态码含义变更必须走破坏性变更流程。
```

***

### 29.18 验收样例数据、测试账号与演示数据

#### 29.18.1 Seed 分类

```text
seed/base    系统必须数据
seed/demo    演示环境数据
seed/test    自动化测试数据
```

#### 29.18.2 建议测试账号

```text
superadmin
security_admin
system_admin
content_admin
content_editor
viewer
no_permission_user
locked_user
twofa_user
```

#### 29.18.3 Seed 规则

```text
SEED-001：base seed 只包含系统必须数据。
SEED-002：demo seed 只用于演示环境。
SEED-003：test seed 只用于自动化测试。
SEED-004：生产环境不得导入 demo seed。
SEED-005：demo 用户密码必须强制首次修改或标记仅演示。
SEED-006：seed 数据不得包含真实 token、真实 secret、真实手机号、真实邮箱。
SEED-007：测试账号权限必须覆盖管理员、普通用户、只读用户、无权限用户。
SEED-008：test seed 必须可重复执行。
SEED-009：demo seed 必须可一键清理。
SEED-010：seed 执行结果必须输出报告。
```

***

### 29.19 运维手册、部署手册与恢复手册

#### 29.19.1 必备文档

```text
docs/ops/deployment.md
docs/ops/configuration.md
docs/ops/backup-restore.md
docs/ops/admin-recovery.md
docs/ops/token-revoke.md
docs/ops/waf-tuning.md
docs/ops/migration.md
docs/ops/troubleshooting.md
docs/ops/incident-response.md
```

#### 29.19.2 运维文档规则

```text
OPS-DOC-001：生产部署必须有部署手册。
OPS-DOC-002：配置项必须有配置手册。
OPS-DOC-003：备份恢复必须有操作手册。
OPS-DOC-004：超级管理员恢复必须有操作手册。
OPS-DOC-005：全员 token 吊销必须有操作手册。
OPS-DOC-006：WAF 误杀处理必须有操作手册。
OPS-DOC-007：数据库 migration 失败必须有处理手册。
OPS-DOC-008：系统不可登录必须有处理手册。
OPS-DOC-009：权限配置错误必须有恢复手册。
OPS-DOC-010：文档必须随版本更新。
OPS-DOC-011：每次生产发布必须确认运维文档是否需要更新。
OPS-DOC-012：恢复手册必须经过演练。
```

***

### 29.20 基础系统表结构补全清单

本节列出基础系统必须设计或明确迁移策略的表。详细字段以数据库迁移脚本为准，但不得缺失以下表级能力。

#### 29.20.1 系统认证与权限表

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_menu
sys_role_permission
sys_refresh_token
sys_password_reset_token
sys_user_session
sys_account_lock
```

#### 29.20.2 配置、文件、日志、安全表

```text
sys_setting_group
sys_setting
sys_setting_change_log
sys_secret_reference
sys_file
sys_file_reference
sys_login_log
sys_audit_log
sys_security_event
sys_ip_ban
sys_i18n_message
sys_dict_type
sys_dict_value
```

#### 29.20.3 组织、通知、任务表

```text
sys_dept
sys_post
sys_user_dept
sys_role_dept
sys_notice
sys_message
sys_message_receiver
sys_mail_template
sys_mail_outbox
sys_notification_event
sys_job
sys_job_log
sys_system_maintenance_log
```

#### 29.20.4 CMS 内容表

```text
cms_channel
cms_article
cms_article_content
cms_article_tag
cms_tag
cms_page
cms_media
cms_media_folder
cms_media_usage
cms_link
cms_content_revision
cms_content_publish_log
cms_content_recycle
cms_site
cms_site_setting
cms_seo_setting
```

#### 29.20.5 表设计规则

```text
TABLE-001：所有业务表必须有 created_at。
TABLE-002：所有可更新表必须有 updated_at。
TABLE-003：所有软删除表必须有 deleted_at。
TABLE-004：所有需要审计的表必须有 created_by / updated_by。
TABLE-005：所有关键配置表必须有 row_version。
TABLE-006：所有迁移自旧系统的数据建议保留 legacy_id。
TABLE-007：所有字典、角色、权限、菜单 code 必须唯一。
TABLE-008：所有表必须明确是否支持软删除。
TABLE-009：所有表必须明确是否 tenant-scoped。
TABLE-010：所有表必须明确索引设计。
```

***

### 29.21 API 完整端点清单

#### 29.21.1 Auth

```text
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
GET    /api/auth/me
POST   /api/auth/password
POST   /api/auth/password/forgot
POST   /api/auth/password/reset
GET    /api/auth/sessions
DELETE /api/auth/sessions/{id}
DELETE /api/auth/sessions
GET    /api/auth/captcha
POST   /api/auth/2fa/setup
POST   /api/auth/2fa/enable
POST   /api/auth/2fa/disable
POST   /api/auth/2fa/verify
POST   /api/auth/2fa/backup-codes/regenerate
```

#### 29.21.2 System User / Role / Menu / Permission

```text
GET    /api/system/users
GET    /api/system/users/{id}
POST   /api/system/users
PUT    /api/system/users/{id}
DELETE /api/system/users/{id}
PATCH  /api/system/users/{id}/status
PUT    /api/system/users/{id}/roles
POST   /api/system/users/{id}/password/reset
POST   /api/system/users/{id}/force-logout
POST   /api/system/users/{id}/unlock

GET    /api/system/roles
GET    /api/system/roles/{id}
POST   /api/system/roles
PUT    /api/system/roles/{id}
DELETE /api/system/roles/{id}
PATCH  /api/system/roles/{id}/status
GET    /api/system/roles/{id}/menus
PUT    /api/system/roles/{id}/menus
GET    /api/system/roles/{id}/permissions
PUT    /api/system/roles/{id}/permissions

GET    /api/system/menus/tree
POST   /api/system/menus
PUT    /api/system/menus/{id}
DELETE /api/system/menus/{id}
PATCH  /api/system/menus/{id}/status
PATCH  /api/system/menus/sort

GET    /api/system/permissions
POST   /api/system/permissions/sync
```

#### 29.21.3 Setting / Dict / File / Logs / Security

```text
GET    /api/system/settings
PUT    /api/system/settings/{key}
GET    /api/system/setting-groups
POST   /api/system/setting-groups
PUT    /api/system/setting-groups/{id}

GET    /api/system/dict-types
POST   /api/system/dict-types
PUT    /api/system/dict-types/{id}
DELETE /api/system/dict-types/{id}
GET    /api/system/dict-values
POST   /api/system/dict-values
PUT    /api/system/dict-values/{id}
DELETE /api/system/dict-values/{id}

GET    /api/system/files
POST   /api/system/files/upload
GET    /api/system/files/{id}/download
GET    /api/system/files/{id}/preview
DELETE /api/system/files/{id}

GET    /api/system/logs/login
GET    /api/system/logs/audit
GET    /api/system/logs/security
GET    /api/system/security/events
GET    /api/system/security/ip-bans
POST   /api/system/security/ip-bans
DELETE /api/system/security/ip-bans/{id}
```

#### 29.21.4 Organization

```text
GET    /api/system/depts/tree
POST   /api/system/depts
PUT    /api/system/depts/{id}
DELETE /api/system/depts/{id}
PATCH  /api/system/depts/{id}/status
PATCH  /api/system/depts/sort

GET    /api/system/posts
POST   /api/system/posts
PUT    /api/system/posts/{id}
DELETE /api/system/posts/{id}
PATCH  /api/system/posts/{id}/status
```

#### 29.21.5 Notification / Job / Maintenance

```text
GET    /api/system/notices
POST   /api/system/notices
PUT    /api/system/notices/{id}
POST   /api/system/notices/{id}/publish
POST   /api/system/notices/{id}/withdraw

GET    /api/system/messages
POST   /api/system/messages/{id}/read
POST   /api/system/messages/read-all

GET    /api/system/mail-templates
PUT    /api/system/mail-templates/{id}
GET    /api/system/mail-outbox
POST   /api/system/mail-outbox/{id}/retry

GET    /api/system/jobs
GET    /api/system/jobs/{id}/logs
POST   /api/system/jobs/{id}/run
PATCH  /api/system/jobs/{id}/status
GET    /api/system/maintenance/health-snapshot
POST   /api/system/maintenance/cache/clear
```

#### 29.21.6 CMS

```text
GET    /api/cms/channels/tree
POST   /api/cms/channels
PUT    /api/cms/channels/{id}
DELETE /api/cms/channels/{id}
PATCH  /api/cms/channels/sort

GET    /api/cms/articles
GET    /api/cms/articles/{id}
POST   /api/cms/articles
PUT    /api/cms/articles/{id}
DELETE /api/cms/articles/{id}
POST   /api/cms/articles/{id}/publish
POST   /api/cms/articles/{id}/offline
POST   /api/cms/articles/{id}/restore

GET    /api/cms/pages
GET    /api/cms/pages/{id}
POST   /api/cms/pages
PUT    /api/cms/pages/{id}
DELETE /api/cms/pages/{id}
POST   /api/cms/pages/{id}/publish

GET    /api/cms/tags
POST   /api/cms/tags
PUT    /api/cms/tags/{id}
DELETE /api/cms/tags/{id}

GET    /api/cms/media
POST   /api/cms/media/upload
PUT    /api/cms/media/{id}
DELETE /api/cms/media/{id}

GET    /api/cms/recycle-bin
POST   /api/cms/recycle-bin/{id}/restore
DELETE /api/cms/recycle-bin/{id}
```

***

### 29.22 基础系统最终验收清单

基础系统验收必须覆盖功能、架构、安全、性能、数据、前端、运维七个维度。

#### 29.22.1 功能验收

```text
ACCEPT-FUNC-001：P0 模块全部可用。
ACCEPT-FUNC-002：初始化后可以登录超级管理员。
ACCEPT-FUNC-003：用户、角色、菜单、权限闭环可用。
ACCEPT-FUNC-004：动态路由和按钮权限可用。
ACCEPT-FUNC-005：配置、字典、文件、日志、安全中心可用。
ACCEPT-FUNC-006：密码找回、会话管理、2FA 可用。
ACCEPT-FUNC-007：基础 CMS 内容模块至少完成表、权限码和 API 规划。
```

#### 29.22.2 架构验收

```text
ACCEPT-ARCH-001：后端使用 Minimal APIs。
ACCEPT-ARCH-002：不得使用 MVC Controller。
ACCEPT-ARCH-003：AOT publish 通过。
ACCEPT-ARCH-004：SqlSugar ORM 约束通过。
ACCEPT-ARCH-005：OpenAPI 生成通过。
ACCEPT-ARCH-006：前端类型由后端契约生成。
```

#### 29.22.3 安全验收

```text
ACCEPT-SEC-001：所有 Endpoint 有权限码或 AllowAnonymous 登记。
ACCEPT-SEC-002：所有带 id 的接口完成对象级授权。
ACCEPT-SEC-003：所有写操作有审计。
ACCEPT-SEC-004：上传、导出、登录、刷新、验证码有限流。
ACCEPT-SEC-005：日志脱敏通过。
ACCEPT-SEC-006：WAF / AppGuard 规则可运行。
```

#### 29.22.4 性能验收

```text
ACCEPT-PERF-001：核心接口满足 P95 预算。
ACCEPT-PERF-002：所有列表分页。
ACCEPT-PERF-003：慢 SQL 记录可用。
ACCEPT-PERF-004：权限、菜单、配置缓存策略生效。
```

#### 29.22.5 数据验收

```text
ACCEPT-DATA-001：迁移 row count 校验通过。
ACCEPT-DATA-002：权限矩阵校验通过。
ACCEPT-DATA-003：旧权限路径到新权限码映射校验通过。
ACCEPT-DATA-004：文件元数据和存储文件校验通过。
ACCEPT-DATA-005：敏感 seed 数据检查通过。
```

#### 29.22.6 前端验收

```text
ACCEPT-FE-001：SoybeanAdmin 登录流程可用。
ACCEPT-FE-002：动态路由由后端菜单生成。
ACCEPT-FE-003：按钮权限以后端 permissions 为准。
ACCEPT-FE-004：前端不得使用 mock 契约。
ACCEPT-FE-005：generated 类型未被手写修改。
```

#### 29.22.7 运维验收

```text
ACCEPT-OPS-001：部署手册完成。
ACCEPT-OPS-002：配置手册完成。
ACCEPT-OPS-003：备份恢复手册完成。
ACCEPT-OPS-004：超级管理员恢复手册完成。
ACCEPT-OPS-005：WAF 调优手册完成。
ACCEPT-OPS-006：迁移失败回滚手册完成。
ACCEPT-OPS-007：健康检查和告警可用。
```

***

### 29.23 本章最终结论

第 29 章将基础系统从“架构治理方案”补全为“可交付系统蓝图”。

最终交付必须同时满足：

```text
1. 有基础功能边界。
2. 有初始化和超级管理员恢复能力。
3. 有配置、密钥、账号、验证码、安全中心闭环。
4. 有组织架构和数据权限支撑。
5. 有通知、任务、维护、日志清理能力。
6. 有 CMS 栏目、文章、媒体、页面的基础模型。
7. 有 OpenAPI、前端类型、权限矩阵、错误码、字典清单。
8. 有 SoybeanAdmin 页面交付清单。
9. 有样例数据、测试账号、运维手册。
10. 有表结构、API 端点、验收清单。
```

第 27、28、29 章共同形成最终约束：

```text
第 27 章：系统如何安全、可控、可发布。
第 28 章：系统如何长期治理、灰度演进、稳定运营。
第 29 章：基础 CMS 系统到底交付什么、如何初始化、如何验收、如何运维接管。
```

***

## 30. 工程落地执行计划与交付工件

前面章节已经完成架构、AOT、SqlSugar ORM、后端契约优先、安全、WAF、性能、CI/CD、高级治理和基础系统能力闭环设计。本章进一步把这些规则转换为**可开发、可测试、可迁移、可验收、可交接的工程交付工件**。

本章不是新增抽象原则，而是规定开发团队必须产出的具体内容：

```text
1. 数据库详细设计。
2. API 详细契约清单。
3. ThinkPHP 旧系统到新系统的数据迁移映射。
4. 权限矩阵与菜单按钮清单。
5. SoybeanAdmin 页面与路由实现清单。
6. ASP.NET Core Minimal APIs 后端代码骨架。
7. OpenAPI 与 TypeScript 类型生成流程。
8. 测试用例矩阵。
9. 实施里程碑与 WBS。
10. 上线验收清单。
11. 旧系统冻结、迁移与切换策略。
```

***

### 30.1 工程落地总原则

```text
ENG-001：所有开发任务必须能追溯到数据库表、API 契约、权限码、前端页面和测试用例。
ENG-002：没有数据库详细设计的模块不得进入后端开发。
ENG-003：没有 API 契约的模块不得进入前端开发。
ENG-004：没有权限矩阵的模块不得进入联调。
ENG-005：没有测试用例矩阵的模块不得进入验收。
ENG-006：没有迁移映射和校验 SQL 的旧数据不得进入正式迁移。
ENG-007：没有 AOT publish 验证的后端变更不得进入发布候选版本。
ENG-008：没有 OpenAPI diff 的契约变更不得合并。
ENG-009：没有 SoybeanAdmin 页面清单的前端模块不得进入页面开发。
ENG-010：没有上线验收清单和回滚方案的版本不得发布生产。
```

所有工程交付物必须进入版本管理：

```text
docs/database/schema-design.md
docs/api/api-contracts.md
docs/migration/legacy-mapping.md
docs/security/permission-matrix.md
docs/frontend/soybean-pages.md
docs/testing/test-matrix.md
docs/release/wbs.md
docs/release/acceptance-checklist.md
docs/release/cutover-plan.md
```

***

### 30.2 交付工件分级

#### 30.2.1 P0：立刻补齐的核心工件

```text
1. 数据库详细设计。
2. API 详细契约清单。
3. ThinkPHP 到新系统迁移映射。
4. 权限矩阵。
5. 后端代码骨架。
6. SoybeanAdmin 页面清单。
```

P0 工件必须在主体开发前完成。P0 工件未完成时，只允许做技术验证和工程骨架搭建，不允许大规模业务开发。

#### 30.2.2 P1：主体开发期间同步补齐的工件

```text
1. OpenAPI 与前端类型生成流程。
2. 测试用例矩阵。
3. 实施里程碑与任务拆分。
4. 上线验收清单。
```

P1 工件必须在系统联调前完成。

#### 30.2.3 P2：上线前必须补齐的工件

```text
1. 旧系统冻结与切换策略。
2. 运维 Runbook 详细步骤。
3. 安全测试脚本清单。
4. 性能压测方案。
5. 数据库索引优化说明。
```

P2 工件必须在生产灰度前完成。

***

### 30.3 附录 A：数据库详细设计

数据库设计必须从“表名清单”升级为“字段级、索引级、约束级、迁移级”设计。所有表必须说明：

```text
1. 字段名。
2. 字段类型。
3. 是否必填。
4. 默认值。
5. 字段说明。
6. 主键、唯一索引、普通索引。
7. 软删除字段。
8. 并发控制字段。
9. 审计字段。
10. 数据保留规则。
11. 是否迁移自旧系统。
12. 是否包含敏感数据。
```

#### 30.3.1 通用字段规范

除纯关系表外，业务表默认包含：

| 字段            | 类型                 | 说明           |
| ------------- | ------------------ | ------------ |
| `id`          | `bigint`           | 主键           |
| `created_at`  | `datetime(3)`      | 创建时间         |
| `created_by`  | `bigint null`      | 创建人          |
| `updated_at`  | `datetime(3)`      | 更新时间         |
| `updated_by`  | `bigint null`      | 更新人          |
| `deleted_at`  | `datetime(3) null` | 软删除时间        |
| `deleted_by`  | `bigint null`      | 软删除人         |
| `row_version` | `bigint`           | 并发版本         |
| `legacy_id`   | `bigint null`      | 旧系统 ID，迁移期使用 |

规则：

```text
DB-DESIGN-001：默认业务表必须支持软删除。
DB-DESIGN-002：关键表必须包含 row_version。
DB-DESIGN-003：迁移表必须保留 legacy_id。
DB-DESIGN-004：唯一索引必须考虑 deleted_at。
DB-DESIGN-005：时间字段统一使用 UTC 或统一配置的服务器时间策略。
DB-DESIGN-006：金额、容量、计数等数值字段必须明确单位。
DB-DESIGN-007：状态字段必须有枚举说明。
DB-DESIGN-008：敏感字段必须标注加密、哈希或脱敏规则。
```

#### 30.3.2 核心系统表详细设计

##### sys\_user

| 字段                     | 类型                  | 必填 | 默认值          | 说明                 |
| ---------------------- | ------------------- | -: | ------------ | ------------------ |
| `id`                   | `bigint`            |  是 | auto         | 用户主键               |
| `legacy_id`            | `bigint null`       |  否 | null         | 旧 `think_admin.id` |
| `username`             | `varchar(64)`       |  是 | -            | 登录名，唯一             |
| `display_name`         | `varchar(128)`      |  是 | -            | 显示名称               |
| `email`                | `varchar(128) null` |  否 | null         | 邮箱，脱敏展示            |
| `phone`                | `varchar(32) null`  |  否 | null         | 手机号，脱敏展示           |
| `avatar_file_id`       | `bigint null`       |  否 | null         | 头像文件 ID            |
| `password_hash`        | `varchar(255)`      |  是 | -            | 密码哈希               |
| `password_algo`        | `varchar(32)`       |  是 | `legacy_php` | 密码算法标识             |
| `must_change_password` | `tinyint`           |  是 | `0`          | 是否强制修改密码           |
| `status`               | `tinyint`           |  是 | `1`          | 1 正常，0 禁用，2 锁定     |
| `security_stamp`       | `varchar(64)`       |  是 | -            | 安全戳                |
| `permission_version`   | `int`               |  是 | `1`          | 权限版本               |
| `last_login_at`        | `datetime(3) null`  |  否 | null         | 最后登录时间             |
| `last_login_ip`        | `varchar(64) null`  |  否 | null         | 最后登录 IP            |
| `two_factor_enabled`   | `tinyint`           |  是 | `0`          | 是否启用 2FA           |
| `created_at`           | `datetime(3)`       |  是 | current      | 创建时间               |
| `updated_at`           | `datetime(3)`       |  是 | current      | 更新时间               |
| `deleted_at`           | `datetime(3) null`  |  否 | null         | 软删除时间              |
| `row_version`          | `bigint`            |  是 | `1`          | 并发版本               |

索引：

```text
pk_sys_user(id)
uk_sys_user_username(username, deleted_at)
idx_sys_user_status(status)
idx_sys_user_legacy_id(legacy_id)
idx_sys_user_deleted_at(deleted_at)
```

约束：

```text
1. 用户名唯一。
2. 默认查询必须排除 deleted_at is not null。
3. 禁止禁用、删除最后一个超级管理员。
4. 密码哈希不得返回前端。
5. security_stamp 变更必须吊销会话。
6. permission_version 变更必须刷新权限缓存。
```

##### sys\_role

| 字段            | 类型                  | 必填 | 默认值     | 说明                      |
| ------------- | ------------------- | -: | ------- | ----------------------- |
| `id`          | `bigint`            |  是 | auto    | 角色主键                    |
| `legacy_id`   | `bigint null`       |  否 | null    | 旧 `think_auth_group.id` |
| `code`        | `varchar(64)`       |  是 | -       | 角色编码，唯一                 |
| `name`        | `varchar(128)`      |  是 | -       | 角色名称                    |
| `status`      | `tinyint`           |  是 | `1`     | 状态                      |
| `sort`        | `int`               |  是 | `0`     | 排序                      |
| `data_scope`  | `varchar(32)`       |  是 | `self`  | 数据范围                    |
| `is_system`   | `tinyint`           |  是 | `0`     | 是否系统内置                  |
| `remark`      | `varchar(500) null` |  否 | null    | 备注                      |
| `created_at`  | `datetime(3)`       |  是 | current | 创建时间                    |
| `updated_at`  | `datetime(3)`       |  是 | current | 更新时间                    |
| `deleted_at`  | `datetime(3) null`  |  否 | null    | 软删除时间                   |
| `row_version` | `bigint`            |  是 | `1`     | 并发版本                    |

索引：

```text
uk_sys_role_code(code, deleted_at)
idx_sys_role_status(status)
idx_sys_role_legacy_id(legacy_id)
```

约束：

```text
1. 系统内置角色不得删除。
2. 角色 code 创建后不得随意修改。
3. 修改角色权限必须更新关联用户 permission_version。
```

##### sys\_user\_role

| 字段           | 类型            | 必填 | 默认值     | 说明    |
| ------------ | ------------- | -: | ------- | ----- |
| `user_id`    | `bigint`      |  是 | -       | 用户 ID |
| `role_id`    | `bigint`      |  是 | -       | 角色 ID |
| `created_at` | `datetime(3)` |  是 | current | 创建时间  |
| `created_by` | `bigint null` |  否 | null    | 创建人   |

索引：

```text
pk_sys_user_role(user_id, role_id)
idx_sys_user_role_role_id(role_id)
```

约束：

```text
1. 用户角色关系必须引用存在且未删除的用户和角色。
2. 修改用户角色必须更新该用户 permission_version。
```

##### sys\_menu

| 字段                | 类型                  | 必填 | 默认值     | 说明                             |
| ----------------- | ------------------- | -: | ------- | ------------------------------ |
| `id`              | `bigint`            |  是 | auto    | 菜单主键                           |
| `legacy_id`       | `bigint null`       |  否 | null    | 旧 `think_auth_rule.id`         |
| `parent_id`       | `bigint`            |  是 | `0`     | 父级 ID                          |
| `type`            | `varchar(32)`       |  是 | -       | `catalog/menu/button/external` |
| `title`           | `varchar(128)`      |  是 | -       | 菜单标题                           |
| `name`            | `varchar(128) null` |  否 | null    | 路由名称                           |
| `path`            | `varchar(255) null` |  否 | null    | 路由路径                           |
| `component`       | `varchar(255) null` |  否 | null    | SoybeanAdmin 组件 key            |
| `icon`            | `varchar(128) null` |  否 | null    | 图标                             |
| `permission_code` | `varchar(128) null` |  否 | null    | 权限码                            |
| `sort`            | `int`               |  是 | `0`     | 排序                             |
| `hidden`          | `tinyint`           |  是 | `0`     | 是否隐藏                           |
| `keep_alive`      | `tinyint`           |  是 | `0`     | 是否缓存                           |
| `status`          | `tinyint`           |  是 | `1`     | 状态                             |
| `is_system`       | `tinyint`           |  是 | `0`     | 系统内置                           |
| `created_at`      | `datetime(3)`       |  是 | current | 创建时间                           |
| `updated_at`      | `datetime(3)`       |  是 | current | 更新时间                           |
| `deleted_at`      | `datetime(3) null`  |  否 | null    | 软删除时间                          |
| `row_version`     | `bigint`            |  是 | `1`     | 并发版本                           |

索引：

```text
idx_sys_menu_parent_id(parent_id)
idx_sys_menu_permission_code(permission_code)
idx_sys_menu_sort(sort)
idx_sys_menu_legacy_id(legacy_id)
```

约束：

```text
1. 菜单树不得形成循环。
2. 菜单层级必须有限制。
3. component 必须在前端白名单中。
4. button 类型不参与动态路由，只参与按钮权限。
```

##### sys\_permission

| 字段              | 类型                  | 必填 | 默认值     | 说明       |
| --------------- | ------------------- | -: | ------- | -------- |
| `id`            | `bigint`            |  是 | auto    | 权限主键     |
| `legacy_id`     | `bigint null`       |  否 | null    | 旧权限规则 ID |
| `code`          | `varchar(128)`      |  是 | -       | 权限码，唯一   |
| `name`          | `varchar(128)`      |  是 | -       | 权限名称     |
| `module`        | `varchar(64)`       |  是 | -       | 模块       |
| `resource`      | `varchar(64)`       |  是 | -       | 资源       |
| `action`        | `varchar(64)`       |  是 | -       | 动作       |
| `http_method`   | `varchar(16) null`  |  否 | null    | HTTP 方法  |
| `route_pattern` | `varchar(255) null` |  否 | null    | 路由模板     |
| `status`        | `tinyint`           |  是 | `1`     | 状态       |
| `is_system`     | `tinyint`           |  是 | `1`     | 系统权限     |
| `created_at`    | `datetime(3)`       |  是 | current | 创建时间     |
| `updated_at`    | `datetime(3)`       |  是 | current | 更新时间     |

索引：

```text
uk_sys_permission_code(code)
idx_sys_permission_module(module)
idx_sys_permission_route(http_method, route_pattern)
```

约束：

```text
1. 权限 code 创建后不得随意修改。
2. 权限同步命令只能新增或停用，不能静默删除。
3. 每个受保护 Endpoint 必须绑定权限码。
```

##### sys\_role\_menu / sys\_role\_permission

| 表                     | 字段                         | 说明     |
| --------------------- | -------------------------- | ------ |
| `sys_role_menu`       | `role_id`, `menu_id`       | 角色可见菜单 |
| `sys_role_permission` | `role_id`, `permission_id` | 角色拥有权限 |

索引：

```text
pk_sys_role_menu(role_id, menu_id)
pk_sys_role_permission(role_id, permission_id)
idx_sys_role_menu_menu_id(menu_id)
idx_sys_role_permission_permission_id(permission_id)
```

约束：

```text
1. 修改角色菜单必须更新关联用户 permission_version。
2. 修改角色权限必须更新关联用户 permission_version。
3. 保存角色权限时必须记录变更前后差异。
```

##### sys\_refresh\_token

| 字段                     | 类型                  | 必填 | 默认值     | 说明               |
| ---------------------- | ------------------- | -: | ------- | ---------------- |
| `id`                   | `bigint`            |  是 | auto    | 主键               |
| `user_id`              | `bigint`            |  是 | -       | 用户 ID            |
| `token_hash`           | `varchar(255)`      |  是 | -       | Refresh Token 哈希 |
| `token_family`         | `varchar(64)`       |  是 | -       | 轮换族 ID           |
| `device_id`            | `varchar(64) null`  |  否 | null    | 设备 ID            |
| `user_agent`           | `varchar(500) null` |  否 | null    | UA               |
| `created_ip`           | `varchar(64) null`  |  否 | null    | 创建 IP            |
| `expires_at`           | `datetime(3)`       |  是 | -       | 过期时间             |
| `revoked_at`           | `datetime(3) null`  |  否 | null    | 吊销时间             |
| `replaced_by_token_id` | `bigint null`       |  否 | null    | 被哪个 token 替换     |
| `created_at`           | `datetime(3)`       |  是 | current | 创建时间             |

索引：

```text
idx_sys_refresh_token_user_id(user_id)
idx_sys_refresh_token_family(token_family)
idx_sys_refresh_token_expires_at(expires_at)
```

约束：

```text
1. 数据库只保存 refresh token hash。
2. 刷新时必须轮换。
3. 修改密码、禁用用户、强制下线必须吊销相关 token。
```

##### sys\_user\_session

用于展示在线设备和支持管理员强制下线。

| 字段             | 类型                  | 说明     |
| -------------- | ------------------- | ------ |
| `id`           | `bigint`            | 主键     |
| `user_id`      | `bigint`            | 用户 ID  |
| `session_key`  | `varchar(128)`      | 会话标识   |
| `device_name`  | `varchar(128) null` | 设备名称   |
| `ip`           | `varchar(64) null`  | IP     |
| `user_agent`   | `varchar(500) null` | UA     |
| `last_seen_at` | `datetime(3)`       | 最后活跃时间 |
| `revoked_at`   | `datetime(3) null`  | 吊销时间   |

##### sys\_password\_reset\_token

| 字段           | 类型                 | 说明         |
| ------------ | ------------------ | ---------- |
| `id`         | `bigint`           | 主键         |
| `user_id`    | `bigint`           | 用户 ID      |
| `token_hash` | `varchar(255)`     | token hash |
| `expires_at` | `datetime(3)`      | 过期时间       |
| `used_at`    | `datetime(3) null` | 使用时间       |
| `created_ip` | `varchar(64) null` | 创建 IP      |
| `created_at` | `datetime(3)`      | 创建时间       |

##### sys\_setting\_group / sys\_setting / sys\_setting\_change\_log

| 表                        | 说明     |
| ------------------------ | ------ |
| `sys_setting_group`      | 配置分组   |
| `sys_setting`            | 系统配置项  |
| `sys_setting_change_log` | 配置变更日志 |

`sys_setting` 关键字段：

| 字段             | 类型             | 说明                               |
| -------------- | -------------- | -------------------------------- |
| `key`          | `varchar(128)` | 配置键，唯一                           |
| `value`        | `text null`    | 配置值，敏感配置不得明文保存                   |
| `value_type`   | `varchar(32)`  | string/int/bool/json/secret\_ref |
| `is_sensitive` | `tinyint`      | 是否敏感                             |
| `group_code`   | `varchar(64)`  | 分组                               |
| `version`      | `int`          | 配置版本                             |

##### sys\_file / sys\_file\_reference

| 表                    | 说明       |
| -------------------- | -------- |
| `sys_file`           | 文件元数据    |
| `sys_file_reference` | 文件业务引用关系 |

`sys_file` 关键字段：

| 字段              | 类型             | 说明                       |
| --------------- | -------------- | ------------------------ |
| `id`            | `bigint`       | 文件 ID                    |
| `storage_key`   | `varchar(255)` | 存储 key，不返回物理路径           |
| `original_name` | `varchar(255)` | 原始文件名                    |
| `extension`     | `varchar(32)`  | 扩展名                      |
| `mime_type`     | `varchar(128)` | MIME                     |
| `size_bytes`    | `bigint`       | 文件大小                     |
| `sha256`        | `varchar(64)`  | 文件哈希                     |
| `visibility`    | `varchar(32)`  | private/protected/public |
| `status`        | `tinyint`      | 状态                       |
| `created_by`    | `bigint`       | 上传人                      |

##### sys\_login\_log / sys\_audit\_log / sys\_security\_event

| 表                    | 说明      |
| -------------------- | ------- |
| `sys_login_log`      | 登录日志    |
| `sys_audit_log`      | 写操作审计日志 |
| `sys_security_event` | 安全事件日志  |

所有日志必须包含：

```text
request_id、user_id、username、ip、user_agent、path、method、status、elapsed_ms、created_at
```

敏感字段必须脱敏，生产环境默认不记录请求体和响应体。

##### sys\_dict\_type / sys\_dict\_value

用于系统枚举、状态、业务字典。

| 表                | 说明   |
| ---------------- | ---- |
| `sys_dict_type`  | 字典类型 |
| `sys_dict_value` | 字典值  |

约束：

```text
1. dict_type code 创建后不得随意修改。
2. 已被业务引用的字典值不得硬删除。
3. 前端只渲染字典，不决定业务含义。
```

##### sys\_dept / sys\_post / sys\_user\_dept / sys\_role\_dept

用于组织架构和数据权限。

| 表               | 说明          |
| --------------- | ----------- |
| `sys_dept`      | 部门树         |
| `sys_post`      | 岗位          |
| `sys_user_dept` | 用户部门关系      |
| `sys_role_dept` | 角色自定义数据权限部门 |

##### sys\_notice / sys\_message / sys\_message\_receiver

用于公告、站内信和通知。

##### sys\_mail\_template / sys\_mail\_outbox

用于邮件模板和发送箱。发送邮件必须走 outbox，失败支持重试。

##### sys\_job / sys\_job\_log

用于后台任务、导入导出、清理任务、邮件重试等。

#### 30.3.3 CMS 业务表详细设计清单

必须补全以下 CMS 表：

```text
cms_channel
cms_article
cms_article_content
cms_article_tag
cms_tag
cms_page
cms_media
cms_content_revision
cms_content_publish_log
cms_content_recycle
cms_site_setting
```

##### cms\_channel

| 字段                | 类型                  | 说明     |
| ----------------- | ------------------- | ------ |
| `id`              | `bigint`            | 栏目 ID  |
| `parent_id`       | `bigint`            | 父栏目    |
| `name`            | `varchar(128)`      | 栏目名称   |
| `slug`            | `varchar(128)`      | URL 标识 |
| `sort`            | `int`               | 排序     |
| `status`          | `tinyint`           | 状态     |
| `seo_title`       | `varchar(255) null` | SEO 标题 |
| `seo_description` | `varchar(500) null` | SEO 描述 |

##### cms\_article

| 字段              | 类型                   | 说明                              |
| --------------- | -------------------- | ------------------------------- |
| `id`            | `bigint`             | 文章 ID                           |
| `channel_id`    | `bigint`             | 栏目 ID                           |
| `title`         | `varchar(255)`       | 标题                              |
| `slug`          | `varchar(255)`       | URL 标识                          |
| `summary`       | `varchar(1000) null` | 摘要                              |
| `cover_file_id` | `bigint null`        | 封面                              |
| `status`        | `varchar(32)`        | draft/published/offline/deleted |
| `author_id`     | `bigint`             | 作者                              |
| `published_at`  | `datetime(3) null`   | 发布时间                            |
| `row_version`   | `bigint`             | 并发版本                            |

##### cms\_article\_content

| 字段             | 类型              | 说明           |
| -------------- | --------------- | ------------ |
| `article_id`   | `bigint`        | 文章 ID        |
| `content_html` | `longtext`      | HTML 正文，必须清洗 |
| `content_text` | `longtext null` | 纯文本摘要        |
| `content_json` | `json null`     | 编辑器结构化内容     |

##### cms\_content\_revision / cms\_content\_publish\_log

用于内容版本和发布记录。已发布内容修改必须生成版本记录。

#### 30.3.4 表结构交付要求

```text
DB-DELIVER-001：每张表必须有建表 SQL。
DB-DELIVER-002：每张表必须有索引说明。
DB-DELIVER-003：每张表必须有字段注释。
DB-DELIVER-004：每张表必须标注是否迁移旧数据。
DB-DELIVER-005：每张表必须标注是否包含敏感字段。
DB-DELIVER-006：每张表必须有最小测试数据。
DB-DELIVER-007：每个 migration 必须有 checksum。
```

***

### 30.4 附录 B：API 详细契约清单

每个接口必须明确：

```text
1. HTTP Method。
2. Path。
3. 权限码。
4. 是否登录。
5. 是否审计。
6. 是否限流。
7. 是否幂等。
8. Request DTO。
9. Response DTO。
10. 错误码。
11. 对象级授权规则。
12. 数据权限规则。
```

#### 30.4.1 API 契约模板

```markdown
### 创建用户

POST /api/v1/system/users

权限码：sys:user:create
审计：是
限流：AdminWrite
对象级授权：非超级管理员不得创建超级管理员用户

Request:
{
  "username": "editor",
  "displayName": "内容编辑",
  "email": "editor@example.com",
  "phone": "60123456789",
  "roleIds": [2, 3],
  "status": 1
}

Response:
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": 10001
  }
}

规则：
1. username 必须唯一。
2. roleIds 必须存在且启用。
3. 创建成功必须初始化 security_stamp 和 permission_version。
4. 创建成功必须记录审计日志。
```

#### 30.4.2 Auth API 清单

| Method   | Path                                       | 权限            | 说明           |
| -------- | ------------------------------------------ | ------------- | ------------ |
| `POST`   | `/api/v1/auth/login`                       | Anonymous     | 登录           |
| `POST`   | `/api/v1/auth/refresh`                     | Anonymous     | 刷新 Token     |
| `POST`   | `/api/v1/auth/logout`                      | Authenticated | 退出           |
| `GET`    | `/api/v1/auth/me`                          | Authenticated | 当前用户信息、菜单、权限 |
| `POST`   | `/api/v1/auth/password`                    | Authenticated | 修改密码         |
| `POST`   | `/api/v1/auth/password/forgot`             | Anonymous     | 忘记密码         |
| `POST`   | `/api/v1/auth/password/reset`              | Anonymous     | 重置密码         |
| `GET`    | `/api/v1/auth/sessions`                    | Authenticated | 当前用户会话       |
| `DELETE` | `/api/v1/auth/sessions/{id}`               | Authenticated | 吊销指定会话       |
| `DELETE` | `/api/v1/auth/sessions`                    | Authenticated | 吊销全部会话       |
| `GET`    | `/api/v1/auth/captcha`                     | Anonymous     | 获取验证码        |
| `POST`   | `/api/v1/auth/2fa/setup`                   | Authenticated | 初始化 2FA      |
| `POST`   | `/api/v1/auth/2fa/enable`                  | Authenticated | 启用 2FA       |
| `POST`   | `/api/v1/auth/2fa/disable`                 | Authenticated | 关闭 2FA       |
| `POST`   | `/api/v1/auth/2fa/verify`                  | Authenticated | 验证 2FA       |
| `POST`   | `/api/v1/auth/2fa/backup-codes/regenerate` | Authenticated | 重新生成备份码      |

#### 30.4.3 System API 清单

##### 用户管理

| Method   | Path                                       | 权限码                       | 审计 |
| -------- | ------------------------------------------ | ------------------------- | -- |
| `GET`    | `/api/v1/system/users`                     | `sys:user:list`           | 否  |
| `GET`    | `/api/v1/system/users/{id}`                | `sys:user:detail`         | 否  |
| `POST`   | `/api/v1/system/users`                     | `sys:user:create`         | 是  |
| `PUT`    | `/api/v1/system/users/{id}`                | `sys:user:update`         | 是  |
| `DELETE` | `/api/v1/system/users/{id}`                | `sys:user:delete`         | 是  |
| `PATCH`  | `/api/v1/system/users/{id}/status`         | `sys:user:update-status`  | 是  |
| `POST`   | `/api/v1/system/users/{id}/password/reset` | `sys:user:reset-password` | 是  |
| `PUT`    | `/api/v1/system/users/{id}/roles`          | `sys:user:assign-role`    | 是  |
| `POST`   | `/api/v1/system/users/{id}/force-logout`   | `sys:user:force-logout`   | 是  |
| `POST`   | `/api/v1/system/users/{id}/unlock`         | `sys:user:unlock`         | 是  |

##### 角色管理

| Method   | Path                                    | 权限码                          |
| -------- | --------------------------------------- | ---------------------------- |
| `GET`    | `/api/v1/system/roles`                  | `sys:role:list`              |
| `GET`    | `/api/v1/system/roles/{id}`             | `sys:role:detail`            |
| `POST`   | `/api/v1/system/roles`                  | `sys:role:create`            |
| `PUT`    | `/api/v1/system/roles/{id}`             | `sys:role:update`            |
| `DELETE` | `/api/v1/system/roles/{id}`             | `sys:role:delete`            |
| `GET`    | `/api/v1/system/roles/{id}/menus`       | `sys:role:menu:list`         |
| `PUT`    | `/api/v1/system/roles/{id}/menus`       | `sys:role:menu:update`       |
| `GET`    | `/api/v1/system/roles/{id}/permissions` | `sys:role:permission:list`   |
| `PUT`    | `/api/v1/system/roles/{id}/permissions` | `sys:role:permission:update` |
| `GET`    | `/api/v1/system/roles/{id}/depts`       | `sys:role:data-scope:list`   |
| `PUT`    | `/api/v1/system/roles/{id}/depts`       | `sys:role:data-scope:update` |

##### 菜单与权限

| Method   | Path                               | 权限码                      |
| -------- | ---------------------------------- | ------------------------ |
| `GET`    | `/api/v1/system/menus/tree`        | `sys:menu:list`          |
| `POST`   | `/api/v1/system/menus`             | `sys:menu:create`        |
| `PUT`    | `/api/v1/system/menus/{id}`        | `sys:menu:update`        |
| `DELETE` | `/api/v1/system/menus/{id}`        | `sys:menu:delete`        |
| `PATCH`  | `/api/v1/system/menus/{id}/status` | `sys:menu:update-status` |
| `PATCH`  | `/api/v1/system/menus/sort`        | `sys:menu:sort`          |
| `GET`    | `/api/v1/system/permissions`       | `sys:permission:list`    |
| `POST`   | `/api/v1/system/permissions/sync`  | `sys:permission:sync`    |

##### 组织架构、字典、配置、文件、日志、安全中心

| 模块    | API 前缀                            | 典型权限                      |
| ----- | --------------------------------- | ------------------------- |
| 部门    | `/api/v1/system/depts`            | `sys:dept:*`              |
| 岗位    | `/api/v1/system/posts`            | `sys:post:*`              |
| 字典    | `/api/v1/system/dicts`            | `sys:dict:*`              |
| 配置    | `/api/v1/system/settings`         | `sys:setting:*`           |
| 文件    | `/api/v1/system/files`            | `sys:file:*`              |
| 登录日志  | `/api/v1/system/logs/login`       | `sys:log:login:list`      |
| 操作日志  | `/api/v1/system/logs/audit`       | `sys:log:audit:list`      |
| 安全事件  | `/api/v1/system/security/events`  | `sys:security:event:list` |
| IP 封禁 | `/api/v1/system/security/ip-bans` | `sys:security:ip-ban:*`   |
| 系统任务  | `/api/v1/system/jobs`             | `sys:job:*`               |

#### 30.4.4 CMS API 清单

| Method   | Path                                | 权限码                   | 说明   |
| -------- | ----------------------------------- | --------------------- | ---- |
| `GET`    | `/api/v1/cms/channels/tree`         | `cms:channel:list`    | 栏目树  |
| `POST`   | `/api/v1/cms/channels`              | `cms:channel:create`  | 创建栏目 |
| `PUT`    | `/api/v1/cms/channels/{id}`         | `cms:channel:update`  | 编辑栏目 |
| `DELETE` | `/api/v1/cms/channels/{id}`         | `cms:channel:delete`  | 删除栏目 |
| `GET`    | `/api/v1/cms/articles`              | `cms:article:list`    | 文章列表 |
| `GET`    | `/api/v1/cms/articles/{id}`         | `cms:article:detail`  | 文章详情 |
| `POST`   | `/api/v1/cms/articles`              | `cms:article:create`  | 创建文章 |
| `PUT`    | `/api/v1/cms/articles/{id}`         | `cms:article:update`  | 编辑文章 |
| `DELETE` | `/api/v1/cms/articles/{id}`         | `cms:article:delete`  | 删除文章 |
| `POST`   | `/api/v1/cms/articles/{id}/publish` | `cms:article:publish` | 发布文章 |
| `POST`   | `/api/v1/cms/articles/{id}/offline` | `cms:article:offline` | 下架文章 |
| `POST`   | `/api/v1/cms/articles/{id}/restore` | `cms:article:restore` | 恢复文章 |
| `GET`    | `/api/v1/cms/pages`                 | `cms:page:list`       | 单页列表 |
| `POST`   | `/api/v1/cms/pages`                 | `cms:page:create`     | 创建单页 |
| `PUT`    | `/api/v1/cms/pages/{id}`            | `cms:page:update`     | 编辑单页 |
| `POST`   | `/api/v1/cms/pages/{id}/publish`    | `cms:page:publish`    | 发布单页 |
| `GET`    | `/api/v1/cms/media`                 | `cms:media:list`      | 媒体列表 |
| `POST`   | `/api/v1/cms/media/upload`          | `cms:media:upload`    | 上传媒体 |
| `DELETE` | `/api/v1/cms/media/{id}`            | `cms:media:delete`    | 删除媒体 |

***

### 30.5 附录 C：ThinkPHP 到新系统数据迁移映射

迁移映射必须包含：

```text
旧表、旧字段、新表、新字段、转换规则、是否迁移、是否脱敏、是否人工确认、迁移后校验 SQL。
```

#### 30.5.1 用户表迁移

| 旧表            | 旧字段                  | 新表                            | 新字段             | 规则               |
| ------------- | -------------------- | ----------------------------- | --------------- | ---------------- |
| `think_admin` | `id`                 | `sys_user`                    | `legacy_id`     | 保留旧 ID           |
| `think_admin` | `username`           | `sys_user`                    | `username`      | 原样迁移，冲突需人工处理     |
| `think_admin` | `realname`           | `sys_user`                    | `display_name`  | 原样迁移             |
| `think_admin` | `password`           | `sys_user`                    | `password_hash` | 原样保留旧 hash，登录后升级 |
| `think_admin` | `groupid`            | `sys_user_role`               | `role_id`       | 映射旧角色 ID         |
| `think_admin` | `status`             | `sys_user`                    | `status`        | 状态映射             |
| `think_admin` | `portrait`           | `sys_file` / `avatar_file_id` | 头像              | 先迁文件，再回填         |
| `think_admin` | `token`              | -                             | -               | 不迁移              |
| `think_admin` | `token_expire_at`    | -                             | -               | 不迁移              |
| `think_admin` | `twofa_secret`       | -                             | -               | 默认不迁移，要求重新绑定     |
| `think_admin` | `twofa_backup_codes` | -                             | -               | 不迁移              |

#### 30.5.2 角色与权限迁移

| 旧表                        | 旧字段        | 新表                                      | 新字段           | 规则               |
| ------------------------- | ---------- | --------------------------------------- | ------------- | ---------------- |
| `think_auth_group`        | `id`       | `sys_role`                              | `legacy_id`   | 保留旧 ID           |
| `think_auth_group`        | `title`    | `sys_role`                              | `name`        | 原样迁移             |
| `think_auth_group`        | `rules`    | `sys_role_permission` / `sys_role_menu` | -             | 按 rule id 拆分     |
| `think_auth_group_access` | `uid`      | `sys_user_role`                         | `user_id`     | 映射用户             |
| `think_auth_group_access` | `group_id` | `sys_user_role`                         | `role_id`     | 映射角色             |
| `think_auth_rule`         | `id`       | `sys_menu` / `sys_permission`           | `legacy_id`   | 按 type 与 name 拆分 |
| `think_auth_rule`         | `name`     | `sys_permission`                        | `legacy_code` | 保留旧路径            |
| `think_auth_rule`         | `title`    | `sys_menu` / `sys_permission`           | `title/name`  | 原样迁移             |
| `think_auth_rule`         | `pid`      | `sys_menu`                              | `parent_id`   | 菜单树映射            |
| `think_auth_rule`         | `css`      | `sys_menu`                              | `icon`        | 图标转换             |

#### 30.5.3 配置、文件、日志与通知迁移

| 旧表                      | 新表                                      | 规则                      |
| ----------------------- | --------------------------------------- | ----------------------- |
| `think_config`          | `sys_setting`                           | 敏感配置转为 secret\_ref 或加密值 |
| `think_file`            | `sys_file`                              | 迁移文件元数据，校验真实文件存在性       |
| `think_log`             | `sys_login_log`                         | 登录日志迁移，可按保留周期裁剪         |
| `think_operate_log`     | `sys_audit_log`                         | 操作日志迁移，敏感字段脱敏           |
| `think_i18n_message`    | `sys_i18n_message`                      | 多语言文案迁移                 |
| `think_enum_type_dict`  | `sys_dict_type`                         | 字典类型迁移                  |
| `think_enum_value_dict` | `sys_dict_value`                        | 字典值迁移                   |
| `think_notice`          | `sys_notice`                            | 公告迁移                    |
| `think_mail_notify`     | `sys_mail_template` / `sys_mail_outbox` | 按语义拆分                   |
| `think_msg_sender`      | `sys_message_sender`                    | 发送器配置迁移                 |

#### 30.5.4 迁移校验要求

```text
MIG-VERIFY-001：迁移前必须输出旧表记录数。
MIG-VERIFY-002：迁移后必须输出新表记录数。
MIG-VERIFY-003：用户数量必须校验。
MIG-VERIFY-004：角色数量必须校验。
MIG-VERIFY-005：用户角色关系数量必须校验。
MIG-VERIFY-006：菜单数量必须校验。
MIG-VERIFY-007：权限数量必须校验。
MIG-VERIFY-008：角色权限关系数量必须校验。
MIG-VERIFY-009：文件数量必须校验。
MIG-VERIFY-010：异常数据必须输出 CSV 报告。
MIG-VERIFY-011：token、2FA secret、backup code 默认不得迁移。
MIG-VERIFY-012：迁移后必须能用至少一个超级管理员登录。
```

***

### 30.6 附录 D：权限矩阵与菜单按钮清单

权限矩阵是后端权限码、前端按钮权限、测试用例和权限种子数据的共同来源。

#### 30.6.1 权限矩阵格式

| 模块   | 菜单   | 动作   | 权限码                       | API                                             | 前端按钮   |
| ---- | ---- | ---- | ------------------------- | ----------------------------------------------- | ------ |
| 用户管理 | 用户列表 | 查看   | `sys:user:list`           | `GET /api/v1/system/users`                      | 页面访问   |
| 用户管理 | 用户列表 | 新增   | `sys:user:create`         | `POST /api/v1/system/users`                     | 新增按钮   |
| 用户管理 | 用户列表 | 编辑   | `sys:user:update`         | `PUT /api/v1/system/users/{id}`                 | 编辑按钮   |
| 用户管理 | 用户列表 | 删除   | `sys:user:delete`         | `DELETE /api/v1/system/users/{id}`              | 删除按钮   |
| 用户管理 | 用户列表 | 重置密码 | `sys:user:reset-password` | `POST /api/v1/system/users/{id}/password/reset` | 重置密码按钮 |

#### 30.6.2 必须覆盖的权限域

```text
1. 系统管理。
2. 用户管理。
3. 角色管理。
4. 菜单管理。
5. 权限管理。
6. 安全中心。
7. 日志管理。
8. 配置管理。
9. 字典管理。
10. 文件管理。
11. 通知公告。
12. 组织架构。
13. 后台任务。
14. 系统维护。
15. CMS 栏目。
16. CMS 文章。
17. CMS 页面。
18. CMS 媒体库。
```

规则：

```text
PERM-DELIVER-001：每个菜单必须登记对应权限码。
PERM-DELIVER-002：每个按钮必须登记对应权限码。
PERM-DELIVER-003：每个写接口必须登记对应权限码。
PERM-DELIVER-004：权限矩阵必须进入版本管理。
PERM-DELIVER-005：权限矩阵变更必须触发权限同步。
PERM-DELIVER-006：前端按钮权限必须来自后端 permissions。
PERM-DELIVER-007：权限矩阵必须覆盖自动化测试用例。
```

***

### 30.7 附录 E：SoybeanAdmin 页面与路由实现清单

前端使用 SoybeanAdmin，但所有菜单、权限、路由数据以后端为准。SoybeanAdmin 只提供 UI、布局、主题、路由承载和组件能力。

#### 30.7.1 页面清单格式

| 页面   | 路由                       | 组件                                      | 权限码                       | API                              |
| ---- | ------------------------ | --------------------------------------- | ------------------------- | -------------------------------- |
| 用户管理 | `/system/user`           | `views/system/user/index.vue`           | `sys:user:list`           | `/api/v1/system/users`           |
| 角色管理 | `/system/role`           | `views/system/role/index.vue`           | `sys:role:list`           | `/api/v1/system/roles`           |
| 菜单管理 | `/system/menu`           | `views/system/menu/index.vue`           | `sys:menu:list`           | `/api/v1/system/menus/tree`      |
| 权限管理 | `/system/permission`     | `views/system/permission/index.vue`     | `sys:permission:list`     | `/api/v1/system/permissions`     |
| 配置管理 | `/system/setting`        | `views/system/setting/index.vue`        | `sys:setting:list`        | `/api/v1/system/settings`        |
| 字典管理 | `/system/dict`           | `views/system/dict/index.vue`           | `sys:dict:list`           | `/api/v1/system/dicts`           |
| 文件管理 | `/system/file`           | `views/system/file/index.vue`           | `sys:file:list`           | `/api/v1/system/files`           |
| 操作日志 | `/system/log/audit`      | `views/system/log/audit/index.vue`      | `sys:log:audit:list`      | `/api/v1/system/logs/audit`      |
| 安全事件 | `/system/security/event` | `views/system/security/event/index.vue` | `sys:security:event:list` | `/api/v1/system/security/events` |
| 文章管理 | `/cms/article`           | `views/cms/article/index.vue`           | `cms:article:list`        | `/api/v1/cms/articles`           |

#### 30.7.2 前端目录约束

```text
src/service/generated
src/service/api/system
src/service/api/cms
src/service/adapters
src/store/modules/auth
src/store/modules/route
src/store/modules/permission
src/views/system/user
src/views/system/role
src/views/system/menu
src/views/system/permission
src/views/system/setting
src/views/system/file
src/views/cms/channel
src/views/cms/article
src/views/cms/media
```

规则：

```text
FE-DELIVER-001：每个页面必须登记路由、组件、权限码和 API。
FE-DELIVER-002：每个页面必须登记表格列、筛选项、按钮、弹窗表单。
FE-DELIVER-003：每个页面必须定义加载、空状态、错误状态。
FE-DELIVER-004：页面不得直接使用 SoybeanAdmin mock 类型。
FE-DELIVER-005：页面不得私自修改后端 DTO 字段名。
FE-DELIVER-006：动态路由 adapter 只能映射后端菜单 DTO，不得创造业务菜单。
```

***

### 30.8 附录 F：ASP.NET Core 后端代码骨架与模块划分

后端采用模块化单体结构，不使用 MVC Controller，不使用运行时扫描注册业务 Endpoint。

```text
src/
  WeCms.Api/
    Program.cs
    appsettings.json
    Json/
      WeCmsJsonContext.cs
    Middleware/
      ExceptionMiddleware.cs
      AuditMiddleware.cs
      AppGuardMiddleware.cs
    Extensions/
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs

  WeCms.Modules.System/
    Auth/
      AuthEndpoints.cs
      AuthService.cs
      IAuthRepository.cs
      AuthDtos.cs
    Users/
      UserEndpoints.cs
      UserService.cs
      IUserRepository.cs
      UserDtos.cs
    Roles/
    Menus/
    Permissions/
    Settings/
    Files/
    Logs/
    Security/
    Dicts/
    Depts/
    Posts/

  WeCms.Modules.Cms/
    Channels/
    Articles/
    Media/
    Pages/

  WeCms.Persistence/
    Data/
      DbConnectionFactory.cs
      UnitOfWork.cs
      DbTransactionFacade.cs
    Migration/
      DbMigrationRunner.cs
    Modules/
      System/
        Auth/
          AuthRepository.cs
        Users/
          UserRepository.cs
      Cms/

  WeCms.Infrastructure/
    Cache/
    Security/
    Storage/
    Mail/
    Jobs/
    OpenApi/

  WeCms.Shared/
    ApiResult.cs
    PagedResult.cs
    ApiCodes.cs
    Permissions.cs
    CurrentUser.cs
    Clock.cs
    Errors/
    Validation/
```

依赖矩阵以 `WeCms.Persistence` 为唯一数据库适配器层：`WeCms.Persistence -> WeCms.Shared / WeCms.Modules.System / WeCms.Modules.Cms` 合法，但只能实现模块暴露的 repository port。`WeCms.Modules.* -> WeCms.Persistence`、模块内 SQL、模块直接引用 SqlSugar ORM / MySqlConnector 均为阻断项。

#### 30.8.1 后端分层规则

```text
CODE-BE-001：Endpoint 只负责 HTTP 绑定、参数绑定和结果返回。
CODE-BE-002：Service / UseCase 负责业务规则、事务和对象级授权。
CODE-BE-003：Repository 实现只允许位于 WeCms.Persistence，且只负责 SQL 和数据映射，不包含业务判断。
CODE-BE-004：事务只能由 Service / UseCase 控制。
CODE-BE-005：DTO 不跨模块随意复用。
CODE-BE-006：所有 DTO 必须加入 JsonSerializerContext。
CODE-BE-007：所有 Endpoint 必须显式注册，不使用运行时扫描。
CODE-BE-008：所有模块必须有独立权限码常量。
CODE-BE-009：所有 Repository 方法必须支持 CancellationToken。
CODE-BE-010：所有 SQL 必须参数化，排序字段必须白名单。
CODE-BE-011：System/Cms 模块只能定义 repository port，不得引用 Persistence 实现。
```

#### 30.8.2 Endpoint 示例规范

```csharp
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/system/users", GetUsers)
            .RequirePermission(Permissions.UserList)
            .WithAudit("sys:user:list")
            .RequireRateLimiting(RateLimitPolicies.AdminRead);

        group.MapPost("/system/users", CreateUser)
            .RequirePermission(Permissions.UserCreate)
            .WithAudit("sys:user:create")
            .RequireRateLimiting(RateLimitPolicies.AdminWrite);

        return group;
    }
}
```

***

### 30.9 附录 G：OpenAPI 与前端类型生成流程

前端一切数据格式以后端为准，必须建立稳定的契约生成链路。

```text
后端 DTO
  ↓
OpenAPI JSON
  ↓
CI 保存契约快照
  ↓
前端生成 TypeScript 类型
  ↓
前端 service/api 使用 generated 类型
  ↓
页面组件消费类型
```

#### 30.9.1 推荐命令

```bash
dotnet run --project src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-v1.json
pnpm openapi:generate
pnpm typecheck
pnpm build
```

#### 30.9.2 契约规则

```text
OPENAPI-DELIVER-001：OpenAPI 是前后端契约交付物。
OPENAPI-DELIVER-002：前端 generated 类型禁止手写。
OPENAPI-DELIVER-003：OpenAPI diff 失败不得合并。
OPENAPI-DELIVER-004：后端 DTO 变更必须同步生成前端类型。
OPENAPI-DELIVER-005：SoybeanAdmin mock 类型不得进入正式业务页面。
OPENAPI-DELIVER-006：OpenAPI 文件必须作为构建产物保存。
OPENAPI-DELIVER-007：生产环境 OpenAPI UI 默认关闭或仅管理员可访问。
```

***

### 30.10 附录 H：测试用例矩阵

测试矩阵必须覆盖功能、安全、权限、迁移、AOT、性能和前端行为。

| 编号          | 模块      | 场景                       | 预期                     |
| ----------- | ------- | ------------------------ | ---------------------- |
| `AUTH-001`  | 登录      | 正确账号密码登录                 | 返回 token               |
| `AUTH-002`  | 登录      | 错误密码连续 5 次               | 触发限流或验证码               |
| `AUTH-003`  | 登录      | 禁用用户登录                   | 返回认证失败                 |
| `AUTH-004`  | Refresh | 使用已轮换 refresh token      | 拒绝并记录安全事件              |
| `AUTHZ-001` | 权限      | 无 `sys:user:list` 访问用户列表 | 返回 403                 |
| `AUTHZ-002` | 对象授权    | 普通管理员编辑超级管理员             | 返回 403                 |
| `AUTHZ-003` | 数据权限    | 用户查看非授权部门数据              | 返回空或 403               |
| `USER-001`  | 用户      | 创建用户                     | 用户、角色关系、审计日志均正确        |
| `ROLE-001`  | 角色      | 修改角色权限                   | permission\_version 更新 |
| `MENU-001`  | 菜单      | 菜单树形成循环                  | 拒绝保存                   |
| `FILE-001`  | 文件      | 上传 `.php` 文件             | 拒绝                     |
| `FILE-002`  | 文件      | 下载无权限文件                  | 返回 403                 |
| `LOG-001`   | 审计      | 创建用户                     | 写入审计日志                 |
| `MIG-001`   | 迁移      | 用户数量校验                   | 新旧数量一致或差异有说明           |
| `MIG-002`   | 迁移      | token / 2FA secret       | 不迁移                    |
| `AOT-001`   | AOT     | linux-x64 PublishAot     | 成功                     |
| `API-001`   | 契约      | OpenAPI 生成               | 成功                     |
| `FE-001`    | 前端      | 动态菜单以后端返回为准              | 路由正确注入                 |
| `FE-002`    | 前端      | 无按钮权限                    | 按钮不显示，接口仍返回 403        |
| `PERF-001`  | 性能      | 用户列表 P95                 | 达到预算                   |
| `WAF-001`   | WAF     | SQL 注入探测                 | WAF 或 AppGuard 拦截并记录   |

规则：

```text
TEST-DELIVER-001：每个权限码至少有一个授权测试和一个拒绝测试。
TEST-DELIVER-002：每个写接口必须有审计日志测试。
TEST-DELIVER-003：每个列表接口必须有分页、排序、筛选测试。
TEST-DELIVER-004：每个带 id 的接口必须有对象级授权测试。
TEST-DELIVER-005：每个上传接口必须有恶意扩展名测试。
TEST-DELIVER-006：每次迁移演练必须输出校验报告。
```

***

### 30.11 附录 I：实施里程碑与任务拆分 WBS

推荐里程碑：

```text
M0：工程骨架搭建
M1：数据库与迁移脚本
M2：认证与安全底座
M3：用户、角色、菜单、权限
M4：SoybeanAdmin 基础接入
M5：配置、字典、文件、日志
M6：组织架构与数据权限
M7：通知、任务、系统维护
M8：CMS 栏目、文章、媒体
M9：旧数据迁移演练
M10：安全、性能、AOT 验收
M11：灰度上线与旧系统切换
```

#### 30.11.1 M0：工程骨架搭建

交付物：

```text
1. .NET 10 Minimal APIs 项目。
2. Native AOT publish 配置。
3. SqlSugar ORM 基础接入。
4. MySQL 连接工厂。
5. ApiResult / PagedResult。
6. 全局异常中间件。
7. JsonSerializerContext。
8. 基础 CI。
```

验收标准：

```text
1. dotnet build 通过。
2. dotnet test 通过。
3. PublishAot 通过。
4. health/live 可访问。
5. health/ready 可访问。
```

#### 30.11.2 M3：用户、角色、菜单、权限

交付物：

```text
1. 用户 CRUD API。
2. 角色 CRUD API。
3. 菜单树 API。
4. 权限同步命令。
5. 角色分配权限。
6. 用户分配角色。
7. 权限缓存。
8. SoybeanAdmin 用户、角色、菜单页面。
```

验收标准：

```text
1. 所有 Endpoint 绑定权限码。
2. 所有写操作记录审计。
3. 权限变更后 permission_version 更新。
4. 前端按钮权限生效。
5. AOT publish 通过。
```

#### 30.11.3 M9：旧数据迁移演练

交付物：

```text
1. 迁移脚本。
2. 迁移映射文档。
3. 迁移校验 SQL。
4. 异常数据报告。
5. 回滚脚本或回滚方案。
```

验收标准：

```text
1. 用户、角色、菜单、权限数量校验通过。
2. token、2FA secret、backup code 不迁移。
3. 至少一个超级管理员可登录新系统。
4. 迁移后权限矩阵可用。
```

***

### 30.12 附录 J：上线验收清单

| 类别   | 验收项                                    | 要求   |
| ---- | -------------------------------------- | ---- |
| AOT  | `linux-x64 PublishAot` 成功              | 必须通过 |
| API  | OpenAPI 生成成功                           | 必须通过 |
| 契约   | OpenAPI diff 无破坏性变更                    | 必须通过 |
| 权限   | 未授权接口全部返回 401/403                      | 必须通过 |
| 对象授权 | 带 id 接口通过对象级授权测试                       | 必须通过 |
| 审计   | 所有写操作有审计日志                             | 必须通过 |
| 迁移   | 用户、角色、权限数量校验通过                         | 必须通过 |
| 前端   | 动态菜单以后端返回为准                            | 必须通过 |
| 安全   | Refresh Token 只保存 hash                 | 必须通过 |
| 文件   | 上传危险扩展名被拒绝                             | 必须通过 |
| 性能   | 核心 API 达到 P95 预算                       | 必须通过 |
| WAF  | Detection / Blocking 策略验证              | 必须通过 |
| 部署   | Nginx / Forwarded Headers / HTTPS 配置正确 | 必须通过 |
| 运维   | 备份恢复演练完成                               | 必须通过 |
| 文档   | 部署、恢复、迁移、权限矩阵文档齐全                      | 必须通过 |

规则：

```text
ACCEPT-001：任何 P0 验收项失败不得上线。
ACCEPT-002：安全、权限、迁移、AOT 项失败不得以“后续修复”为由上线。
ACCEPT-003：验收报告必须归档。
ACCEPT-004：上线版本必须可追溯 commit、构建号和数据库 migration 版本。
```

***

### 30.13 附录 K：旧系统冻结、迁移与切换策略

完整迁移项目必须规划旧系统冻结窗口和新旧系统切换策略。

#### 30.13.1 推荐切换阶段

```text
第一阶段：旧系统正常运行，新系统开发。
第二阶段：新系统迁移演练，旧系统继续运行。
第三阶段：选定冻结窗口，旧系统进入只读。
第四阶段：执行最终迁移。
第五阶段：新系统上线。
第六阶段：旧系统只读保留。
第七阶段：归档旧系统。
```

#### 30.13.2 冻结与切换规则

```text
CUTOVER-001：必须定义旧系统进入只读模式的时间点。
CUTOVER-002：必须定义哪些模块先冻结。
CUTOVER-003：必须定义冻结窗口内允许继续写入的数据。
CUTOVER-004：必须定义增量数据同步策略。
CUTOVER-005：必须定义最后一次迁移窗口。
CUTOVER-006：迁移失败必须有回滚方案。
CUTOVER-007：旧系统必须只读保留一段时间。
CUTOVER-008：旧系统保留期间不得继续产生业务写入。
CUTOVER-009：Nginx / DNS 切换必须有回滚步骤。
CUTOVER-010：切换后用户必须重新登录。
CUTOVER-011：切换后必须监控登录失败、403、500、慢查询和 WAF 拦截。
CUTOVER-012：切换完成必须生成迁移报告。
```

#### 30.13.3 切换前检查

```text
1. 数据库备份完成。
2. 文件存储备份完成。
3. 迁移脚本 dry run 通过。
4. 迁移校验 SQL 通过。
5. 超级管理员新系统登录验证通过。
6. SoybeanAdmin 动态路由验证通过。
7. 权限矩阵验证通过。
8. 回滚包准备完成。
9. WAF 规则处于预期模式。
10. 运维和业务负责人确认。
```

***

### 30.14 工程交付产物目录建议

```text
docs/
  architecture/
    overview.md
    adr/
  database/
    schema-design.md
    indexes.md
    migrations.md
  api/
    api-contracts.md
    error-codes.md
    openapi.md
  migration/
    legacy-mapping.md
    dry-run-report.md
    cutover-plan.md
  security/
    permission-matrix.md
    threat-model.md
    audit-policy.md
  frontend/
    soybean-pages.md
    route-adapter.md
    generated-types.md
  testing/
    test-matrix.md
    performance-plan.md
    security-test-plan.md
  release/
    wbs.md
    acceptance-checklist.md
    rollback.md
  ops/
    deployment.md
    configuration.md
    backup-restore.md
    troubleshooting.md
```

***

### 30.15 工程落地最终结论

第 30 章回答的问题是：

```text
开发人员具体按什么表、什么接口、什么页面、什么权限、什么任务去实现。
```

最终执行要求：

```text
1. 先完成数据库详细设计，再写 Repository。
2. 先完成 API 契约，再写前端页面。
3. 先完成权限矩阵，再做按钮权限。
4. 先完成迁移映射，再写迁移脚本。
5. 先完成 OpenAPI 生成链路，再开展前后端联调。
6. 先完成测试矩阵，再进入验收。
7. 先完成旧系统冻结与切换策略，再安排上线窗口。
```

至此，迁移计划从“架构与治理方案”进一步升级为“可执行工程实施蓝图”。

***

## 31. 参考资料

- ASP.NET Core Native AOT 官方文档：`https://learn.microsoft.com/aspnet/core/fundamentals/native-aot`
- .NET Native AOT 部署文档：`https://learn.microsoft.com/dotnet/core/deploying/native-aot/`
- .NET 支持策略：`https://dotnet.microsoft.com/platform/support/policy/dotnet-core`
- SqlSugar ORM GitHub：`https://github.com/DotNetNext/SqlSugar`
- SqlSugar ORM 文档：`https://www.donet5.com/Home/Doc`
- SqlSugar ORM NuGet：`https://www.nuget.org/packages/SqlSugarCore`
- MySqlConnector：`https://mysqlconnector.net/`
- SoybeanAdmin 文档：`https://docs.soybeanjs.cn/zh/`
- OWASP ASVS：`https://owasp.org/www-project-application-security-verification-standard/`
- OWASP API Security Top 10：`https://owasp.org/API-Security/`
- OWASP ModSecurity Core Rule Set：`https://owasp.org/www-project-modsecurity-core-rule-set/`
- OWASP File Upload Cheat Sheet：`https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html`
- OpenAPI Specification：`https://swagger.io/specification/`
- Semantic Versioning：`https://semver.org/`
- Problem Details for HTTP APIs RFC 9457：`https://www.rfc-editor.org/info/rfc9457`
- OpenFeature：`https://openfeature.dev/`
- OpenTelemetry .NET：`https://opentelemetry.io/docs/languages/dotnet/`
- WCAG 标准：`https://www.w3.org/WAI/standards-guidelines/wcag/`
- NIST SSDF：`https://csrc.nist.gov/pubs/sp/800/218/final`
- SLSA：`https://slsa.dev/`




