# WeCMS 从 0 重构核心开发计划书

**文档版本：v1.0**
**日期：2026-06-13**
**适用项目：WeCMS Next 重构版**
**技术栈：.NET 10 Native AOT / ASP.NET Core Minimal API / Vue 3 / SqlSugar / MySQL**

---

# 1. 重构目标

本次重构采用 **完全重建** 策略，不在旧项目上继续修补。

旧项目只作为业务理解参考，不复制旧代码、不保留旧兼容逻辑、不保留历史字段 fallback、不保留半实现代码。

核心目标是从 0 搭建一套：

* AOT-first 的 ASP.NET Core 后端底座；
* 基于 Minimal API 的稳定 API 服务；
* 基于 SqlSugar 的 MySQL 持久化层；
* 基于 Vue 3 的后台管理端；
* 清晰的模块化单体架构；
* 严格的权限、审计、OpenAPI、测试与发布门禁；
* 可长期演进的 CMS 基础平台。

最终定位：

> WeCMS 是一套基于 .NET 10 Native AOT 的模块化 CMS 后台系统，而不是传统三层后台模板。

---

# 2. 总体技术路线

## 2.1 后端技术栈

| 类型      | 技术                                                |
| ------- | ------------------------------------------------- |
| Runtime | .NET 10                                           |
| Web 框架  | ASP.NET Core Minimal API                          |
| 发布模式    | Native AOT                                        |
| ORM     | SqlSugar                                          |
| 数据库     | MySQL 8                                           |
| API 文档  | Microsoft.AspNetCore.OpenApi                      |
| 认证      | JWT Access Token + Refresh Token                  |
| 权限      | RBAC + Permission Code                            |
| 日志      | 结构化日志                                             |
| 测试      | xUnit                                             |
| 架构模式    | 模块化单体 + Clean Architecture 思想 + Vertical Slice 落地 |

## 2.2 前端技术栈

| 类型         | 技术                              |
| ---------- | ------------------------------- |
| Framework  | Vue 3                           |
| Build Tool | Vite                            |
| Language   | TypeScript                      |
| Router     | Vue Router                      |
| State      | Pinia                           |
| UI         | Naive UI 或 Element Plus 二选一     |
| API Client | 基于 OpenAPI 合同生成或手写 Typed Client |

---

# 3. 核心架构原则

## 3.1 总原则

1. 从 0 新建项目，不在旧项目上继续修补。
2. 后端必须 AOT-first。
3. API 层使用 Minimal API，不使用 MVC Controller。
4. 数据库 ORM 固定使用 SqlSugar。
5. 采用模块化单体，不做微服务。
6. 业务模块不得直接依赖 SqlSugar。
7. 权限系统必须先于 CMS 内容模块完成。
8. OpenAPI 合同必须作为构建产物和质量门禁。
9. 所有核心接口必须具备测试覆盖。
10. 不做过度抽象，不做万能 Repository，不做动态插件幻想。

## 3.2 明确禁止事项

禁止：

* MVC Controller；
* Session；
* AutoMapper 运行时映射；
* 运行时反射扫描 Endpoint；
* 动态插件加载；
* 业务模块直接使用 SqlSugarClient；
* 业务模块直接引用 WeCms.Persistence；
* 生产环境自动建表；
* 权限只做在前端；
* 无 OpenAPI 合同的接口进入主分支；
* 无测试的认证、权限、审计逻辑进入主分支；
* 为旧项目字段和旧接口做兼容。

---

# 4. 最终类库命名定型

经过调整后，最终采用以下简洁命名：

```text
src/
  WeCms.Api/

  WeCms.Core/
  WeCms.Contracts/
  WeCms.Abstractions/

  WeCms.Infrastructure/
  WeCms.Persistence/

  WeCms.Modules.System/
  WeCms.Modules.Cms/

  WeCms.DbMigrator/
  WeCms.Worker/
```

## 4.1 命名演进

| 原命名                            | 最终命名                 | 说明                           |
| ------------------------------ | -------------------- | ---------------------------- |
| WeCms.SharedKernel             | WeCms.Core           | 更简洁，表示系统核心基础类型               |
| WeCms.Application.Abstractions | WeCms.Abstractions   | 更简洁，表示跨层抽象接口                 |
| WeCms.Infrastructure.SqlSugar  | WeCms.Persistence    | 不暴露 ORM 名称，持久化层内部使用 SqlSugar |
| WeCms.Infrastructure           | WeCms.Infrastructure | 保留，负责非数据库基础设施                |
| WeCms.Contracts                | WeCms.Contracts      | 保留，负责 API 合同                 |

---

# 5. 各类库职责边界

## 5.1 WeCms.Api

`WeCms.Api` 是后端启动宿主，只负责应用装配。

职责：

* Program.cs；
* 注册模块；
* 注册认证授权；
* 注册 OpenAPI；
* 注册中间件；
* 注册 HealthCheck；
* 注册 JSON Source Generator；
* 注册全局异常处理；
* 注册 CORS、限流、安全策略。

禁止：

* 写业务逻辑；
* 写 SQL；
* 写数据库 Entity；
* 写具体权限判断细节；
* 放业务 UseCase。

---

## 5.2 WeCms.Core

`WeCms.Core` 是最底层核心基础库。

职责：

* Entity 基类；
* AuditableEntity；
* SoftDeleteEntity；
* Result；
* Error；
* DomainException；
* PageRequest；
* PageResult；
* 基础枚举；
* 系统常量；
* Guard；
* PermissionCode 基础类型。

推荐目录：

```text
WeCms.Core/
  Entities/
    Entity.cs
    AuditableEntity.cs
    SoftDeleteEntity.cs

  Errors/
    Error.cs
    ErrorCodes.cs
    DomainException.cs

  Results/
    Result.cs
    ResultOfT.cs

  Pagination/
    PageRequest.cs
    PageResult.cs

  Security/
    PermissionCode.cs

  Auditing/
    AuditAction.cs

  Constants/
    SystemConstants.cs
```

禁止放入：

* SqlSugar；
* JWT 实现；
* HTTP Context；
* 数据库连接；
* 用户模块业务逻辑；
* CMS 模块业务逻辑；
* 文件存储实现；
* 缓存实现。

---

## 5.3 WeCms.Contracts

`WeCms.Contracts` 负责 API 合同。

职责：

* Request DTO；
* Response DTO；
* 分页 DTO；
* OpenAPI 对外模型；
* 前后端共享的接口契约。

推荐目录：

```text
WeCms.Contracts/
  Auth/
    LoginRequest.cs
    LoginResponse.cs
    RefreshTokenRequest.cs
    CurrentUserResponse.cs

  System/
    Users/
      CreateUserRequest.cs
      UpdateUserRequest.cs
      UserListItemResponse.cs

    Roles/
      CreateRoleRequest.cs
      UpdateRoleRequest.cs
      RoleResponse.cs

    Menus/
      CreateMenuRequest.cs
      UpdateMenuRequest.cs
      MenuResponse.cs

  Cms/
    Contents/
      CreateContentRequest.cs
      UpdateContentRequest.cs
      ContentListItemResponse.cs
      ContentDetailResponse.cs
```

禁止：

* 数据库 Entity；
* SqlSugar Attribute；
* 业务 Service；
* Repository；
* 权限实现逻辑。

---

## 5.4 WeCms.Abstractions

`WeCms.Abstractions` 负责跨层接口。

业务模块依赖它，而不是依赖具体实现。

职责：

* IUnitOfWork；
* IRepository；
* ICurrentUser；
* IClock；
* IIdGenerator；
* IPasswordHasher；
* ITokenService；
* IFileStorage；
* ICacheService；
* IAuditWriter；
* ILoginLogWriter；
* IPermissionChecker。

推荐目录：

```text
WeCms.Abstractions/
  Persistence/
    IUnitOfWork.cs
    IRepository.cs
    IQueryRepository.cs

  Security/
    ICurrentUser.cs
    IPasswordHasher.cs
    ITokenService.cs
    IPermissionChecker.cs

  Time/
    IClock.cs

  Ids/
    IIdGenerator.cs

  Files/
    IFileStorage.cs

  Caching/
    ICacheService.cs

  Auditing/
    IAuditWriter.cs
    ILoginLogWriter.cs
```

原则：

> Abstractions 只放接口，不放实现。

---

## 5.5 WeCms.Infrastructure

`WeCms.Infrastructure` 负责非数据库基础设施实现。

职责：

* JWT Token 服务；
* 密码哈希；
* 当前用户上下文；
* 时间服务；
* ID 生成器；
* 缓存实现；
* 文件存储实现；
* 邮件服务；
* 审计上下文；
* 安全工具；
* 配置 Options。

推荐目录：

```text
WeCms.Infrastructure/
  DependencyInjection.cs

  Security/
    PasswordHasher.cs
    JwtTokenService.cs

  Identity/
    CurrentUser.cs
    CurrentUserAccessor.cs

  Time/
    SystemClock.cs

  Ids/
    SnowflakeIdGenerator.cs

  Caching/
    MemoryCacheService.cs

  Files/
    LocalFileStorage.cs

  Auditing/
    AuditContext.cs

  Options/
    JwtOptions.cs
    StorageOptions.cs
```

禁止：

* SqlSugarCore；
* MySQL 连接；
* 数据库 Entity；
* Migration；
* Seed；
* 业务模块实现。

---

## 5.6 WeCms.Persistence

`WeCms.Persistence` 负责数据库持久化层。

虽然项目名不包含 SqlSugar，但当前内部实现固定使用 SqlSugar。

职责：

* SqlSugar 初始化；
* 数据库连接；
* Entity；
* 表映射；
* UnitOfWork；
* Repository；
* Migration；
* Seed；
* 软删除；
* 审计字段自动填充；
* 分页查询；
* 事务处理。

推荐目录：

```text
WeCms.Persistence/
  DependencyInjection.cs

  Db/
    SqlSugarOptions.cs
    SqlSugarClientFactory.cs
    DbSession.cs
    UnitOfWork.cs

  Entities/
    System/
      SystemUserEntity.cs
      SystemRoleEntity.cs
      SystemPermissionEntity.cs
      SystemMenuEntity.cs
      SystemRefreshTokenEntity.cs
      SystemLoginLogEntity.cs
      SystemAuditLogEntity.cs

    Cms/
      CmsContentEntity.cs
      CmsCategoryEntity.cs
      CmsTagEntity.cs
      CmsMediaEntity.cs
      CmsPageEntity.cs

  Mapping/
    TableNames.cs
    EntityMappings.cs

  Repositories/
    System/
      SystemUserRepository.cs
      SystemRoleRepository.cs
      SystemMenuRepository.cs

    Cms/
      CmsContentRepository.cs
      CmsCategoryRepository.cs

  Migrations/
    Migration_20260613_InitialSystem.cs
    Migration_20260613_InitialCms.cs

  Seed/
    AdminSeed.cs
    PermissionSeed.cs
    MenuSeed.cs
    DictSeed.cs

  Interceptors/
    AuditFieldInterceptor.cs
    SoftDeleteInterceptor.cs
```

对外只暴露注册入口：

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
```

---

## 5.7 WeCms.Modules.System

`WeCms.Modules.System` 是系统管理模块。

职责：

* Auth；
* Users；
* Roles；
* Permissions；
* Menus；
* Dicts；
* Configs；
* AuditLogs；
* LoginLogs。

推荐目录：

```text
WeCms.Modules.System/
  DependencyInjection.cs
  SystemModuleEndpoints.cs

  Auth/
    LoginEndpoint.cs
    RefreshTokenEndpoint.cs
    LogoutEndpoint.cs
    CurrentUserEndpoint.cs

  Users/
    CreateUser.cs
    UpdateUser.cs
    DeleteUser.cs
    ListUsers.cs
    GetUserDetail.cs

  Roles/
    CreateRole.cs
    UpdateRole.cs
    AssignRolePermissions.cs

  Permissions/
    PermissionCatalog.cs
    PermissionDefinitions.cs

  Menus/
    CreateMenu.cs
    UpdateMenu.cs
    ListMenus.cs

  AuditLogs/
    ListAuditLogs.cs

  LoginLogs/
    ListLoginLogs.cs
```

---

## 5.8 WeCms.Modules.Cms

`WeCms.Modules.Cms` 是 CMS 内容模块。

职责：

* 内容管理；
* 分类管理；
* 标签管理；
* 媒体管理；
* 页面管理；
* 站点配置。

推荐目录：

```text
WeCms.Modules.Cms/
  DependencyInjection.cs
  CmsModuleEndpoints.cs

  Contents/
    CreateContent.cs
    UpdateContent.cs
    PublishContent.cs
    UnpublishContent.cs
    DeleteContent.cs
    ListContents.cs
    GetContentDetail.cs

  Categories/
    CreateCategory.cs
    UpdateCategory.cs
    ListCategories.cs

  Tags/
    CreateTag.cs
    UpdateTag.cs
    ListTags.cs

  Media/
    UploadMedia.cs
    ListMedia.cs

  Pages/
    CreatePage.cs
    UpdatePage.cs
    ListPages.cs
```

---

# 6. 最终依赖关系

## 6.1 依赖方向

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Persistence

WeCms.Modules.System
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Modules.Cms
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Infrastructure
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Persistence
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions
  -> WeCms.Infrastructure
  -> SqlSugarCore
```

## 6.2 关键规则

业务模块允许依赖：

* WeCms.Core；
* WeCms.Contracts；
* WeCms.Abstractions。

业务模块禁止依赖：

* WeCms.Persistence；
* SqlSugarCore；
* SqlSugarClient；
* 数据库 Entity；
* 数据库表名；
* 数据库字段名。

---

# 7. 后端模块注册规范

所有模块采用显式注册，不做运行时反射扫描。

示例：

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddSystemModule();
builder.Services.AddCmsModule();

app.MapSystemModule();
app.MapCmsModule();
```

模块内部提供两个入口：

```csharp
public static class SystemModule
{
    public static IServiceCollection AddSystemModule(
        this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSystemModule(
        this IEndpointRouteBuilder app)
    {
        return app;
    }
}
```

---

# 8. 数据库设计原则

## 8.1 主键策略

建议使用 long 类型 SnowflakeId。

原因：

* 适合 MySQL 索引；
* 比 Guid 更适合聚簇索引；
* 前端传输简单；
* 后期可支持分布式扩展。

## 8.2 基础字段

所有核心业务表统一包含：

```text
id
created_at
created_by
updated_at
updated_by
deleted_at
deleted_by
is_deleted
row_version
remark
```

## 8.3 生产环境迁移策略

生产 API 不负责自动建表。

数据库迁移统一通过：

```text
WeCms.DbMigrator
```

负责：

* 初始化数据库；
* 执行 migration；
* 执行 seed；
* 校验基础数据；
* 初始化管理员；
* 初始化权限码；
* 初始化菜单。

环境策略：

| 环境          | 策略                          |
| ----------- | --------------------------- |
| Development | 可自动初始化                      |
| Test        | 每次重建测试库                     |
| Staging     | 显式执行 migration              |
| Production  | 禁止 API 自动建表，只允许显式 migration |

---

# 9. 第一批核心数据表

## 9.1 System 模块

```text
system_user
system_role
system_user_role
system_permission
system_role_permission
system_menu
system_dict_type
system_dict_data
system_config
system_refresh_token
system_login_log
system_audit_log
```

## 9.2 CMS 模块

```text
cms_content
cms_category
cms_tag
cms_content_tag
cms_media
cms_page
cms_site_setting
```

---

# 10. 权限系统设计

## 10.1 权限模型

采用：

```text
用户 -> 角色 -> 权限 -> 菜单 / 按钮 / 接口
```

核心原则：

> 菜单只是权限的展示形式，真正的安全边界必须在后端接口权限上。

## 10.2 权限码设计

权限使用 Permission Code，不直接绑定前端按钮名称。

示例：

```text
system.user.read
system.user.create
system.user.update
system.user.delete

system.role.read
system.role.create
system.role.update
system.role.delete
system.role.assign_permission

system.menu.read
system.menu.create
system.menu.update
system.menu.delete

cms.content.read
cms.content.create
cms.content.update
cms.content.delete
cms.content.publish
cms.content.unpublish
```

## 10.3 Endpoint 权限声明

推荐形式：

```csharp
group.MapGet("/users", ListUsers.Handle)
     .RequirePermission("system.user.read");
```

权限定义集中管理：

```text
PermissionCatalog
PermissionDefinition
PermissionGroup
```

权限由代码定义，再 seed 到数据库。

---

# 11. API 设计规范

## 11.1 路由规范

统一使用：

```text
/api/v1/{module}/{resource}
```

示例：

```text
/api/v1/auth/login
/api/v1/system/users
/api/v1/system/roles
/api/v1/system/menus
/api/v1/cms/contents
/api/v1/cms/categories
```

## 11.2 REST 风格

```text
GET    /api/v1/system/users
GET    /api/v1/system/users/{id}
POST   /api/v1/system/users
PUT    /api/v1/system/users/{id}
DELETE /api/v1/system/users/{id}
```

特殊动作：

```text
POST /api/v1/system/users/{id}/enable
POST /api/v1/system/users/{id}/disable
POST /api/v1/cms/contents/{id}/publish
POST /api/v1/cms/contents/{id}/unpublish
```

## 11.3 返回模型

成功时直接返回 DTO。

分页返回：

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

失败使用 ProblemDetails：

```json
{
  "type": "https://wecms/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "traceId": "xxx"
}
```

不建议使用传统统一包装：

```json
{
  "code": 0,
  "message": "success",
  "data": {}
}
```

---

# 12. 第一批 API 清单

## 12.1 Auth

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

## 12.2 System Users

```text
GET    /api/v1/system/users
GET    /api/v1/system/users/{id}
POST   /api/v1/system/users
PUT    /api/v1/system/users/{id}
DELETE /api/v1/system/users/{id}
POST   /api/v1/system/users/{id}/enable
POST   /api/v1/system/users/{id}/disable
```

## 12.3 System Roles

```text
GET    /api/v1/system/roles
GET    /api/v1/system/roles/{id}
POST   /api/v1/system/roles
PUT    /api/v1/system/roles/{id}
DELETE /api/v1/system/roles/{id}
POST   /api/v1/system/roles/{id}/permissions
```

## 12.4 System Menus

```text
GET    /api/v1/system/menus
POST   /api/v1/system/menus
PUT    /api/v1/system/menus/{id}
DELETE /api/v1/system/menus/{id}
```

## 12.5 System Logs

```text
GET /api/v1/system/audit-logs
GET /api/v1/system/login-logs
```

## 12.6 CMS Contents

```text
GET    /api/v1/cms/contents
GET    /api/v1/cms/contents/{id}
POST   /api/v1/cms/contents
PUT    /api/v1/cms/contents/{id}
DELETE /api/v1/cms/contents/{id}
POST   /api/v1/cms/contents/{id}/publish
POST   /api/v1/cms/contents/{id}/unpublish
```

## 12.7 CMS Base

```text
GET    /api/v1/cms/categories
POST   /api/v1/cms/categories
PUT    /api/v1/cms/categories/{id}
DELETE /api/v1/cms/categories/{id}

GET    /api/v1/cms/tags
POST   /api/v1/cms/tags
PUT    /api/v1/cms/tags/{id}
DELETE /api/v1/cms/tags/{id}

GET    /api/v1/cms/media
POST   /api/v1/cms/media
```

---

# 13. Vue 3 管理端设计

## 13.1 前端目录结构

```text
frontend/
  admin/
    src/
      app/
        main.ts
        providers.ts

      router/
        index.ts
        guards.ts
        routes.static.ts
        routes.dynamic.ts

      stores/
        auth.store.ts
        user.store.ts
        menu.store.ts
        permission.store.ts

      api/
        http.ts
        auth.api.ts
        system/
          users.api.ts
          roles.api.ts
          menus.api.ts
        cms/
          contents.api.ts
          categories.api.ts

      layouts/
        AdminLayout.vue

      views/
        login/
        dashboard/
        system/
          users/
          roles/
          menus/
          permissions/
        cms/
          contents/
          categories/
          tags/
          media/

      components/
      permissions/
      types/
```

## 13.2 前端基础能力

第一版必须完成：

* 登录页；
* 主布局；
* 侧边菜单；
* 顶部栏；
* 面包屑；
* 用户信息；
* 动态菜单；
* 权限路由；
* 按钮权限；
* 请求拦截器；
* Token 自动刷新；
* 401 / 403 / 500 统一处理；
* 错误页；
* Dashboard 占位页。

---

# 14. 开发阶段计划

## P0：工程骨架阶段

目标：

* 新建 solution；
* 建立最终类库结构；
* API 可以启动；
* Vue 3 admin 可以启动；
* OpenAPI 可以生成；
* HealthCheck 可以访问；
* Native AOT publish 可以通过。

验收命令：

```bash
dotnet build
dotnet test
dotnet publish src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true

pnpm install
pnpm typecheck
pnpm build
```

---

## P1-A：Infrastructure 基础设施阶段

目标：

完成非数据库基础设施。

内容：

* IClock / SystemClock；
* IIdGenerator / SnowflakeIdGenerator；
* IPasswordHasher；
* ITokenService；
* ICurrentUser；
* JWT Options；
* 审计上下文；
* 基础缓存；
* 本地文件存储接口与实现。

验收标准：

* 不依赖数据库；
* 单元测试通过；
* AOT publish 通过；
* 认证相关基础服务可测试。

---

## P1-B：Persistence 持久化阶段

目标：

完成 SqlSugar + MySQL + AOT 验证。

内容：

* SqlSugarClientFactory；
* UnitOfWork；
* Entity；
* Mapping；
* Migration；
* Seed；
* Repository；
* 事务；
* 分页；
* 软删除；
* 审计字段自动填充。

必须验证：

* AOT publish 后可以连接 MySQL；
* AOT publish 后可以执行 CRUD；
* AOT publish 后可以执行事务；
* AOT publish 后可以分页查询；
* AOT publish 后可以执行基础 Join；
* 软删除过滤生效；
* 审计字段自动填充。

这是整个项目第一道关键质量门禁。

---

## P2：Auth 登录闭环阶段

目标：

完成后台登录闭环。

内容：

* 用户表；
* 密码哈希；
* 登录；
* Refresh Token；
* Token 轮换；
* 退出；
* 当前用户；
* 登录日志。

接口：

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

验收标准：

* 错误密码不可登录；
* 禁用用户不可登录；
* Refresh Token 可轮换；
* 旧 Refresh Token 失效；
* 登录日志记录 IP / UserAgent；
* OpenAPI Schema 正确；
* AOT publish 通过。

---

## P3：RBAC 权限阶段

目标：

完成用户、角色、权限、菜单闭环。

内容：

* 用户管理；
* 角色管理；
* 权限码管理；
* 角色分配权限；
* 用户分配角色；
* 菜单绑定权限；
* Endpoint 权限校验；
* 前端动态菜单；
* 前端按钮权限。

验收标准：

* 无权限访问接口返回 403；
* 超级管理员拥有全部权限；
* 普通角色只能访问被授权接口；
* 菜单根据权限动态显示；
* 按钮根据权限动态显示；
* 权限 seed 可重复执行。

---

## P4：系统管理模块阶段

目标：

完成后台系统基础管理。

模块：

* 用户管理；
* 角色管理；
* 权限管理；
* 菜单管理；
* 字典管理；
* 系统配置；
* 操作日志；
* 登录日志。

验收标准：

* 每个模块支持分页；
* 每个模块支持基础筛选；
* 核心模块支持新增、修改、删除；
* 关键操作写审计日志；
* 所有接口有权限控制；
* 所有接口有 OpenAPI 合同。

---

## P5：Vue 3 管理端阶段

目标：

完成可用的后台管理端基础壳。

内容：

* 登录页；
* AdminLayout；
* 动态菜单；
* 权限路由；
* 请求拦截器；
* Token 刷新；
* 用户管理页面；
* 角色管理页面；
* 菜单管理页面；
* 权限管理页面；
* 日志查看页面。

验收标准：

* 登录成功进入后台；
* 刷新页面权限不丢失；
* Token 过期可刷新；
* 无权限菜单不可见；
* 无权限接口返回 403 后前端提示正确；
* 前端生产构建成功。

---

## P6：CMS 内容核心阶段

目标：

完成 CMS 最小可用核心。

模块：

* 内容管理；
* 分类管理；
* 标签管理；
* 媒体管理；
* 页面管理；
* 站点配置。

内容状态：

```text
Draft
PendingReview
Published
Archived
Deleted
```

验收标准：

* 内容可创建草稿；
* 内容可发布；
* 内容可下架；
* Slug 唯一；
* 分类可管理；
* 标签可管理；
* 内容支持分页、搜索、筛选；
* 内容操作写审计日志；
* 所有 CMS 接口具备权限控制。

---

# 15. 测试与质量门禁

## 15.1 后端门禁

每次提交必须通过：

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
```

要求：

* Build 通过；
* Test 通过；
* AOT publish 通过；
* OpenAPI 可导出；
* 无核心 AOT 警告；
* 无权限测试失败；
* 无合同测试失败。

## 15.2 前端门禁

```bash
pnpm install
pnpm typecheck
pnpm lint
pnpm build
```

要求：

* TypeScript 无错误；
* 生产构建成功；
* 权限路由正常；
* API 类型与后端合同一致。

## 15.3 架构测试

必须增加架构测试，防止后期腐化。

测试规则：

* WeCms.Core 不允许引用任何业务模块；
* WeCms.Contracts 不允许引用 Infrastructure / Persistence；
* WeCms.Abstractions 不允许引用实现层；
* Modules 不允许引用 WeCms.Persistence；
* Modules 不允许引用 SqlSugarCore；
* Api 不允许写业务 UseCase；
* Endpoint 不允许直接写 SQL；
* Persistence 不允许引用 Api；
* Infrastructure 不允许引用 Persistence。

---

# 16. MVP 范围

## 16.1 MVP 必须包含

* 后台登录；
* Refresh Token；
* 用户管理；
* 角色管理；
* 权限管理；
* 菜单管理；
* 操作日志；
* 登录日志；
* 内容管理；
* 分类管理；
* 标签管理；
* Vue 3 后台壳；
* OpenAPI 合同；
* AOT 发布；
* CI 门禁。

## 16.2 MVP 暂不包含

* 多租户；
* 工作流审批；
* 插件市场；
* 低代码表单；
* 页面搭建器；
* 多数据库运行时切换；
* 全文搜索；
* 消息中心；
* 复杂站群管理。

---

# 17. 第一阶段执行重点

第一阶段不要急着做 CMS 文章功能。

正确顺序是：

```text
工程骨架
  -> AOT publish
  -> SqlSugar + MySQL AOT 验证
  -> Auth
  -> RBAC
  -> 菜单
  -> 系统管理
  -> Vue 后台壳
  -> CMS 内容
```

最关键的两道门禁：

## 门禁 1：AOT + SqlSugar + MySQL 可运行

必须确认：

```text
Minimal API
JWT
OpenAPI
SqlSugar
MySQL
CRUD
事务
分页
AOT publish
```

全部可以在 Native AOT 发布后正常工作。

## 门禁 2：权限系统闭环

必须确认：

```text
用户
角色
权限
菜单
接口
前端按钮
```

形成完整闭环。

---

# 18. 最终架构定型总结

最终采用：

```text
WeCms.Core
WeCms.Contracts
WeCms.Abstractions
WeCms.Infrastructure
WeCms.Persistence
WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Api
WeCms.DbMigrator
WeCms.Worker
```

一句话说明：

> Core 放基础类型，Contracts 放 API 合同，Abstractions 放接口，Infrastructure 放通用实现，Persistence 放数据库实现，Modules 放业务模块，Api 只负责启动装配。

最终目标：

> 建立一套干净、稳定、可测试、可发布、可扩展、不背历史包袱的 .NET 10 AOT CMS 后台系统。
