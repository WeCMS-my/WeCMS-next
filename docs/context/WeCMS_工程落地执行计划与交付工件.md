# WeCMS 工程落地执行计划与交付工件

> 本文档从《WeCMS Next .NET10 AOT + SoybeanAdmin 完整迁移重构计划》第 30 章抽取而来，可作为研发执行、任务拆分、Code Review 与上线验收的独立工作文档。
> 关联执行文档：`WeCMS_M0_工程骨架验证文档.md` 用于指导 M0 工程骨架搭建、AOT 发布、SqlSugar ORM、OpenAPI、SoybeanAdmin 联通、认证最小闭环、权限元数据闭环与旧系统迁移 Spike。


## 1. 工程落地执行计划与交付工件

本章用于把前文的架构、规则、治理要求转化为可执行的工程交付物。前文回答“系统应该如何设计、如何约束”；本章回答“开发人员按哪些表、哪些接口、哪些页面、哪些任务、哪些验收清单落地”。

本章也是后续项目拆分、排期、Code Review、测试验收、迁移上线的执行基线。

---

### 30.1 工程落地总原则

```text
DELIVERY-001：所有交付必须围绕后端契约优先原则执行。
DELIVERY-002：所有后端接口必须先定义 DTO、权限码、错误码和审计规则，再开发页面。
DELIVERY-003：所有前端页面必须以后端 OpenAPI / DTO / 权限矩阵为事实源。
DELIVERY-004：所有数据表必须先完成字段、索引、约束、审计字段、并发字段设计，再进入开发。
DELIVERY-005：所有模块必须同时交付 API、前端页面、权限矩阵、测试用例和迁移策略。
DELIVERY-006：任何未进入数据库设计、API 契约、权限矩阵、页面清单、测试矩阵的功能，不得进入开发。
DELIVERY-007：所有新增模块必须通过 Native AOT publish gate。
DELIVERY-008：所有新增模块必须通过 SqlSugar ORM 约束检查。
DELIVERY-009：所有新增模块必须明确是否需要旧系统数据迁移。
DELIVERY-010：所有新增模块必须明确上线验收标准和回滚策略。
```

核心交付顺序：

```text
数据库详细设计
  ↓
API 详细契约
  ↓
权限矩阵
  ↓
OpenAPI 生成
  ↓
前端 TypeScript 类型生成
  ↓
SoybeanAdmin 页面实现
  ↓
测试用例矩阵
  ↓
迁移演练
  ↓
上线验收
```

---

### 30.2 P0 交付工件

P0 是完整迁移第一阶段必须完成的工程产物。未完成 P0，不允许进入 UAT。

```text
P0-ARTIFACT-001：数据库详细设计文档。
P0-ARTIFACT-002：API 详细契约清单。
P0-ARTIFACT-003：ThinkPHP 到新系统数据迁移映射表。
P0-ARTIFACT-004：权限矩阵与按钮级权限清单。
P0-ARTIFACT-005：后端代码骨架。
P0-ARTIFACT-006：SoybeanAdmin 页面与路由实现清单。
P0-ARTIFACT-007：OpenAPI 生成流程。
P0-ARTIFACT-008：前端 TypeScript 类型生成流程。
P0-ARTIFACT-009：基础测试用例矩阵。
P0-ARTIFACT-010：AOT 发布验证脚本。
P0-ARTIFACT-011：数据库初始化和 seed 脚本。
P0-ARTIFACT-012：旧系统迁移 dry-run 脚本。
P0-ARTIFACT-013：上线验收清单。
```

P0 对应模块：

```text
Auth
User
Role
Menu
Permission
Setting
Dict
File
LoginLog
AuditLog
SecurityEvent
SoybeanAdmin 登录、动态路由、系统管理页面
```

---

### 30.3 P1 交付工件

P1 是基础系统进入可持续运营所需的工程产物。

```text
P1-ARTIFACT-001：组织架构详细设计。
P1-ARTIFACT-002：数据权限详细设计。
P1-ARTIFACT-003：通知公告、站内信、邮件 outbox 设计。
P1-ARTIFACT-004：后台任务与系统维护设计。
P1-ARTIFACT-005：CMS 栏目、文章、媒体、页面基础设计。
P1-ARTIFACT-006：性能压测方案。
P1-ARTIFACT-007：安全测试脚本清单。
P1-ARTIFACT-008：运维 Runbook。
P1-ARTIFACT-009：旧系统冻结与切换方案。
P1-ARTIFACT-010：灰度上线与回滚方案。
```

---

### 30.4 P2 交付工件

P2 用于产品化和平台化增强。

```text
P2-ARTIFACT-001：多租户预留落地方案。
P2-ARTIFACT-002：内容审核流方案。
P2-ARTIFACT-003：内容版本 diff 方案。
P2-ARTIFACT-004：插件化扩展方案。
P2-ARTIFACT-005：高级报表与数据看板方案。
P2-ARTIFACT-006：SBOM 与供应链制品签名增强方案。
P2-ARTIFACT-007：可访问性验收方案。
P2-ARTIFACT-008：ADR 决策记录目录。
```

P2 不应阻塞第一版上线，但所有 P2 能力在 P0/P1 设计中应预留扩展点。

---

### 30.5 后端工程骨架

后端工程采用模块化单体结构。所有 Endpoint 显式注册，不允许运行时扫描 Endpoint，不允许 MVC Controller，不允许 Razor View，不允许依赖运行时反射发现模块。

推荐目录：

```text
src/
  WeCms.Api/
    Program.cs
    appsettings.json
    appsettings.Development.json
    Json/
      WeCmsJsonContext.cs
    Middleware/
      ExceptionMiddleware.cs
      AuditMiddleware.cs
      AppGuardMiddleware.cs
      RequestIdMiddleware.cs
    Filters/
      PermissionEndpointFilter.cs
      AuditEndpointFilter.cs
      ValidationEndpointFilter.cs
    Extensions/
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs
      AuthenticationExtensions.cs
      AuthorizationExtensions.cs
      OpenApiExtensions.cs

  WeCms.Modules.System/
    Auth/
      AuthEndpoints.cs
      AuthService.cs
      IAuthRepository.cs
      AuthDtos.cs
      AuthValidators.cs
      AuthPermissions.cs
    Users/
      UserEndpoints.cs
      UserService.cs
      IUserRepository.cs
      UserDtos.cs
      UserValidators.cs
      UserPermissions.cs
    Roles/
      RoleEndpoints.cs
      RoleService.cs
      IRoleRepository.cs
      RoleDtos.cs
      RoleValidators.cs
      RolePermissions.cs
    Menus/
    Permissions/
    Settings/
    Dicts/
    Files/
    Logs/
    Security/
    Depts/
    Posts/
    Notices/
    Messages/
    Jobs/

  WeCms.Modules.Cms/
    Channels/
    Articles/
    Pages/
    Media/
    Tags/
    Links/

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
        Roles/
          RoleRepository.cs
      Cms/

  WeCms.Infrastructure/
    Cache/
      CacheKeyBuilder.cs
      PermissionCache.cs
      SettingCache.cs
    Security/
      PasswordHasher.cs
      TokenService.cs
      CurrentUserProvider.cs
      ClientIpProvider.cs
      DataProtectionService.cs
    Storage/
      IFileStorage.cs
      LocalFileStorage.cs
      FileTypeDetector.cs
    Mail/
      MailSender.cs
      MailTemplateRenderer.cs
    Jobs/
      JobRunner.cs
    OpenApi/
      OpenApiExporter.cs

  WeCms.Shared/
    ApiResult.cs
    PagedResult.cs
    ApiCodes.cs
    Permissions.cs
    CurrentUser.cs
    Clock.cs
    DomainException.cs
    ValidationError.cs
    Constants/
    Pagination/
    Security/
```

依赖矩阵说明：

```text
WeCms.Api -> WeCms.Modules.System / WeCms.Modules.Cms / WeCms.Infrastructure / WeCms.Persistence / WeCms.Shared
WeCms.Modules.System -> WeCms.Shared
WeCms.Modules.Cms -> WeCms.Shared
WeCms.Persistence -> WeCms.Shared / WeCms.Modules.System / WeCms.Modules.Cms
WeCms.Infrastructure -> WeCms.Shared
WeCms.Shared -> 不引用其它生产工程
```

`WeCms.Persistence` 是数据库适配器层，引用 System/Cms 模块只用于实现模块暴露的 repository port。System/Cms 模块不得引用 `WeCms.Persistence`、SqlSugar ORM、MySQLConnector、`DbConnection`、`DbTransaction` 或 SQL 文本。`WeCms.Infrastructure` 只承载非数据库基础设施实现，不能作为数据库访问层。

后端代码边界规则：

```text
BE-SKELETON-001：Endpoint 只负责 HTTP 输入输出绑定，不写业务规则。
BE-SKELETON-002：Service 负责业务规则、事务边界、领域不变量。
BE-SKELETON-003：Repository 实现只允许位于 WeCms.Persistence，且只负责 SQL 和数据映射，不写权限判断，不写业务流程。
BE-SKELETON-004：DTO 不复用数据库实体。
BE-SKELETON-005：Request DTO、Response DTO、Query DTO 必须分开。
BE-SKELETON-006：所有 DTO 必须加入 WeCmsJsonContext。
BE-SKELETON-007：所有 Endpoint 必须显式注册。
BE-SKELETON-008：所有模块必须有权限码常量文件。
BE-SKELETON-009：所有模块必须有 API 契约测试。
BE-SKELETON-010：所有模块必须有 Repository 集成测试。
BE-SKELETON-011：System/Cms 模块只能定义 repository port，不得直接依赖 Persistence 实现。
```

Endpoint 示例规范：

```csharp
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/api/v1/system/users")
            .RequireAuthorization()
            .WithTags("System.Users");

        group.MapGet("", UserHandlers.GetPagedAsync)
            .RequirePermission(SystemPermissions.UserList)
            .WithAudit("sys:user:list")
            .Produces<ApiResult<PagedResult<UserListItemResponse>>>()
            .Produces<ApiResult>(StatusCodes.Status403Forbidden);

        group.MapPost("", UserHandlers.CreateAsync)
            .RequirePermission(SystemPermissions.UserCreate)
            .WithAudit("sys:user:create")
            .RequireRateLimiting(RateLimitPolicies.AdminWrite)
            .Produces<ApiResult<IdResponse>>();

        return group;
    }
}
```

---

### 30.6 前端工程骨架

前端基于 SoybeanAdmin，但 SoybeanAdmin 只作为 UI 模板和工程脚手架，不能作为 API 契约来源。

推荐目录：

```text
frontend/
  soybean-admin/
    src/
      service/
        generated/
          schemas.ts
          services.ts
          types.ts
        api/
          auth.ts
          system/
            user.ts
            role.ts
            menu.ts
            permission.ts
            setting.ts
            dict.ts
            file.ts
            log.ts
            security.ts
            dept.ts
            post.ts
          cms/
            channel.ts
            article.ts
            page.ts
            media.ts
        adapters/
          route-adapter.ts
          menu-adapter.ts
          permission-adapter.ts
          pagination-adapter.ts
        request/
          instance.ts
          error-handler.ts
          token-handler.ts
      store/
        modules/
          auth.ts
          route.ts
          permission.ts
          user.ts
      router/
        dynamic-routes.ts
        guards.ts
      views/
        login/
        system/
          user/
          role/
          menu/
          permission/
          setting/
          dict/
          file/
          log/
          security/
          dept/
          post/
        cms/
          channel/
          article/
          page/
          media/
      constants/
        permissions.ts
        api-codes.ts
```

前端工程规则：

```text
FE-SKELETON-001：service/generated 目录禁止手写修改。
FE-SKELETON-002：service/api 只封装请求，不修改业务字段结构。
FE-SKELETON-003：service/adapters 只允许做 SoybeanAdmin 需要的 UI 适配。
FE-SKELETON-004：页面组件消费后端类型，不消费 mock 类型。
FE-SKELETON-005：动态路由事实源是后端菜单 DTO。
FE-SKELETON-006：按钮权限事实源是后端 permissions 数组。
FE-SKELETON-007：前端不得在页面内硬编码后端 URL。
FE-SKELETON-008：前端不得私自定义业务错误码。
FE-SKELETON-009：前端不得私自变更分页结构。
FE-SKELETON-010：401、403、429、500 必须由统一 request handler 处理。
```

---

### 30.7 OpenAPI 与前端类型生成流程

后端契约优先必须有自动化链路支撑，否则容易变成口头约定。

生成链路：

```text
后端 DTO / Minimal API Metadata
  ↓
OpenAPI JSON
  ↓
CI 保存契约快照
  ↓
OpenAPI diff 检测破坏性变更
  ↓
前端生成 TypeScript 类型
  ↓
前端 service/api 使用 generated 类型
  ↓
页面组件消费类型
```

建议命令：

```bash
# 后端导出 OpenAPI
dotnet run --project src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-v1.json

# 前端生成类型
pnpm openapi:generate
pnpm typecheck
pnpm build
```

OpenAPI 交付规则：

```text
OPENAPI-DELIVERY-001：OpenAPI JSON 是构建产物。
OPENAPI-DELIVERY-002：OpenAPI JSON 必须随版本归档。
OPENAPI-DELIVERY-003：OpenAPI diff 发现破坏性变更必须阻断合并。
OPENAPI-DELIVERY-004：后端 DTO 变更必须重新生成前端类型。
OPENAPI-DELIVERY-005：前端 generated 类型禁止手写修改。
OPENAPI-DELIVERY-006：OpenAPI 文档必须包含认证方式、错误响应、分页响应、权限码说明。
OPENAPI-DELIVERY-007：生产环境 OpenAPI UI 默认关闭或仅管理员可见。
OPENAPI-DELIVERY-008：SoybeanAdmin mock 类型不得进入正式业务页面。
```

推荐产物：

```text
artifacts/openapi/wecms-api-v1.json
docs/api/error-codes.md
docs/api/permission-codes.md
frontend/soybean-admin/src/service/generated/
```

---

### 30.8 实施里程碑与 WBS

#### M0：工程骨架搭建

交付物：

```text
1. Git 仓库结构。
2. .NET 10 Minimal API AOT 骨架。
3. SqlSugar ORM 接入。
4. MySQL 连接工厂。
5. ApiResult / PagedResult / ApiCodes。
6. ExceptionMiddleware。
7. JsonSerializerContext。
8. CI AOT publish。
9. SoybeanAdmin 工程初始化。
```

验收标准：

```text
1. dotnet publish /p:PublishAot=true 成功。
2. 健康检查接口可访问。
3. OpenAPI 可生成。
4. SoybeanAdmin build 成功。
```

#### M1：数据库与迁移脚本

交付物：

```text
1. sys_* 基础 schema。
2. cms_* 基础 schema。
3. migration 执行器或脚本规范。
4. base seed。
5. demo seed。
6. ThinkPHP 旧库读取连接。
7. 迁移 dry-run 报告。
```

验收标准：

```text
1. 新库可从空库初始化。
2. base seed 后可以创建超级管理员。
3. migration 可重复 dry run。
4. row count 校验报告可生成。
```

#### M2：认证与安全底座

交付物：

```text
1. 登录。
2. Refresh Token。
3. Logout。
4. /api/auth/me。
5. 密码修改。
6. 旧密码 hash 兼容。
7. 2FA 绑定与验证。
8. 登录限流。
9. 安全日志。
10. SoybeanAdmin 登录接入。
```

验收标准：

```text
1. 旧用户可登录新系统。
2. Refresh Token 只存 hash。
3. 登录失败触发限流。
4. 2FA 可绑定、验证、禁用。
5. 登录、登出、刷新都有安全日志。
```

#### M3：用户、角色、菜单、权限

交付物：

```text
1. 用户 CRUD。
2. 角色 CRUD。
3. 菜单树 CRUD。
4. 权限同步命令。
5. 用户分配角色。
6. 角色分配菜单。
7. 角色分配权限。
8. PermissionEndpointFilter。
9. permission_version。
10. SoybeanAdmin 用户/角色/菜单/权限页面。
```

验收标准：

```text
1. 所有 Endpoint 绑定权限码。
2. 无权限接口返回 403。
3. 权限变更后 permission_version 更新。
4. 前端动态菜单以后端返回为准。
5. 按钮权限正确生效。
6. 超级管理员保护规则生效。
```

#### M4：系统基础模块

交付物：

```text
1. 系统配置。
2. 字典管理。
3. 文件上传、下载、预览。
4. 登录日志。
5. 操作日志。
6. 安全事件。
7. i18n 基础管理。
8. 安全中心。
```

验收标准：

```text
1. 配置变更有审计。
2. 敏感配置不明文返回前端。
3. 文件上传危险类型被拒绝。
4. 文件下载必须鉴权。
5. 日志查询分页和权限过滤有效。
```

#### M5：组织架构与数据权限

交付物：

```text
1. 部门管理。
2. 岗位管理。
3. 用户部门关系。
4. 角色数据权限。
5. DataScope 查询过滤。
6. SoybeanAdmin 部门/岗位页面。
```

验收标准：

```text
1. 部门树不可形成循环。
2. 列表查询应用数据权限过滤。
3. 详情查询应用对象级授权。
4. 导出数据应用数据权限过滤。
```

#### M6：通知、任务、系统维护

交付物：

```text
1. 通知公告。
2. 站内信。
3. 邮件模板。
4. 邮件 outbox。
5. 后台任务。
6. 任务日志。
7. 系统维护页面。
8. 清理任务。
```

验收标准：

```text
1. 公告支持草稿、发布、下架。
2. 站内信支持已读/未读。
3. 邮件失败可重试。
4. 清理任务可手动触发并记录日志。
```

#### M7：CMS 栏目、文章、媒体

交付物：

```text
1. 栏目管理。
2. 文章管理。
3. 单页管理。
4. 标签管理。
5. 媒体库。
6. 发布/下架。
7. 回收站。
8. 内容版本。
```

验收标准：

```text
1. 文章必须归属栏目。
2. 内容发布记录发布人和发布时间。
3. 删除内容进入回收站。
4. 富文本附件建立引用关系。
5. 公开内容 API 不返回后台字段。
```

#### M8：旧数据迁移演练

交付物：

```text
1. 旧库全量迁移脚本。
2. 用户迁移。
3. 角色迁移。
4. 权限迁移。
5. 菜单迁移。
6. 文件迁移。
7. 配置迁移。
8. 日志迁移。
9. 迁移异常报告。
```

验收标准：

```text
1. row count 校验通过。
2. 权限矩阵校验通过。
3. 旧 token、2FA secret、SMTP 密码不自动迁移。
4. 文件引用可追溯。
5. 迁移脚本可重复执行。
```

#### M9：安全、性能、AOT 验收

交付物：

```text
1. AOT publish 报告。
2. 安全测试报告。
3. WAF 测试报告。
4. 性能压测报告。
5. OpenAPI diff 报告。
6. 依赖漏洞扫描报告。
```

验收标准：

```text
1. linux-x64 AOT 发布成功。
2. P0 安全用例全部通过。
3. P95 性能目标达成。
4. 高危依赖为 0。
5. OpenAPI 无未确认破坏性变更。
```

#### M10：灰度上线与旧系统切换

交付物：

```text
1. 最终迁移窗口计划。
2. 旧系统只读方案。
3. 新系统上线方案。
4. 回滚方案。
5. 运维接管文档。
```

验收标准：

```text
1. 切换前备份完成。
2. 旧系统进入只读。
3. 最终迁移完成。
4. 核心用户可登录。
5. 旧系统只读归档。
```

---

### 30.9 旧系统冻结与切换策略

完整迁移必须设计旧系统冻结、迁移和切换流程，避免新旧系统并行写入导致数据不一致。

推荐策略：

```text
第一阶段：旧系统正常运行，新系统开发。
第二阶段：新系统进行迁移演练，旧系统继续运行。
第三阶段：选定冻结窗口，旧系统进入只读模式。
第四阶段：执行最终迁移。
第五阶段：新系统上线。
第六阶段：旧系统只读保留。
第七阶段：归档旧系统。
```

切换规则：

```text
CUTOVER-001：切换前必须完成全量备份。
CUTOVER-002：切换前必须完成最终迁移 dry run。
CUTOVER-003：切换窗口必须冻结旧系统写操作。
CUTOVER-004：冻结后旧系统只允许查询，不允许用户、权限、内容、文件写入。
CUTOVER-005：最终迁移必须输出 row count 校验报告。
CUTOVER-006：最终迁移必须输出权限矩阵校验报告。
CUTOVER-007：最终迁移必须输出异常数据清单。
CUTOVER-008：迁移失败必须按回滚方案恢复旧系统写入。
CUTOVER-009：新系统上线后用户必须重新登录。
CUTOVER-010：旧系统至少保留一个只读观察周期。
CUTOVER-011：旧系统归档前必须清理敏感 token、secret、临时文件。
CUTOVER-012：Nginx / DNS / Admin URL 切换必须有回滚步骤。
```

切换检查清单：

```text
1. 备份完成。
2. 新系统版本号确认。
3. 新系统 AOT 发布物 checksum 确认。
4. 数据库 migration 版本确认。
5. base seed 和权限同步完成。
6. 旧系统只读开关已开启。
7. 最终迁移脚本执行完成。
8. 核心账号登录验证完成。
9. 核心菜单权限验证完成。
10. 文件上传下载验证完成。
11. 日志写入验证完成。
12. 前端静态资源缓存刷新完成。
```

---

### 30.10 上线验收清单

上线验收必须以清单形式执行。任何 Must 级别项目失败，不允许上线。

| 类别 | 验收项 | 级别 | 结果 |
|---|---|---:|---|
| AOT | `dotnet publish /p:PublishAot=true` 成功 | Must | 待验收 |
| AOT | 生产发布物为 Native AOT 可执行文件 | Must | 待验收 |
| API | OpenAPI 生成成功 | Must | 待验收 |
| API | OpenAPI diff 无未确认破坏性变更 | Must | 待验收 |
| 契约 | 前端 generated 类型来自后端 OpenAPI | Must | 待验收 |
| 认证 | 登录、刷新、退出可用 | Must | 待验收 |
| 认证 | Refresh Token 只保存 hash | Must | 待验收 |
| 权限 | 所有敏感 Endpoint 绑定权限码 | Must | 待验收 |
| 权限 | 未授权访问返回 401 / 403 | Must | 待验收 |
| 权限 | 对象级授权用例通过 | Must | 待验收 |
| 审计 | 所有写操作记录审计日志 | Must | 待验收 |
| 安全 | 密码、token、secret 不进入日志 | Must | 待验收 |
| 安全 | 登录限流生效 | Must | 待验收 |
| 文件 | 危险扩展名上传被拒绝 | Must | 待验收 |
| 文件 | 文件下载必须鉴权 | Must | 待验收 |
| SQL | 禁止 `SELECT *` 检查通过 | Must | 待验收 |
| SQL | 关键列表查询有索引评估 | Should | 待验收 |
| 性能 | 核心接口 P95 达标 | Must | 待验收 |
| WAF | WAF Detection / Blocking 策略验证 | Should | 待验收 |
| 迁移 | 用户、角色、权限数量校验通过 | Must | 待验收 |
| 迁移 | 迁移异常数据有报告 | Must | 待验收 |
| 前端 | 动态菜单以后端返回为准 | Must | 待验收 |
| 前端 | 按钮权限正确显示 | Must | 待验收 |
| 部署 | Nginx 反向代理配置正确 | Must | 待验收 |
| 部署 | Kestrel 不直接暴露公网 | Must | 待验收 |
| 运维 | 备份恢复手册存在 | Must | 待验收 |
| 运维 | 超级管理员恢复手册存在 | Must | 待验收 |
```

---

### 30.11 附录 A：数据库详细设计基线

本节定义数据库详细设计应包含的最小信息。具体 SQL 可在 `database/schema` 和 migration 脚本中落地。

#### 30.11.1 通用字段规范

所有业务表默认包含：

| 字段 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| `id` | bigint | 是 | 主键 |
| `created_at` | datetime(3) | 是 | 创建时间，UTC |
| `created_by` | bigint null | 否 | 创建人 |
| `updated_at` | datetime(3) | 是 | 更新时间，UTC |
| `updated_by` | bigint null | 否 | 更新人 |
| `deleted_at` | datetime(3) null | 否 | 软删除时间 |
| `deleted_by` | bigint null | 否 | 软删除人 |
| `row_version` | bigint | 是 | 乐观并发版本 |
| `legacy_id` | bigint null | 否 | 旧系统 ID，迁移表建议保留 |

通用规则：

```text
DB-COMMON-001：业务表默认采用 bigint 主键。
DB-COMMON-002：所有时间字段统一使用 UTC。
DB-COMMON-003：软删除统一使用 deleted_at / deleted_by。
DB-COMMON-004：关键业务表必须有 row_version。
DB-COMMON-005：迁移自旧系统的核心表必须保留 legacy_id。
DB-COMMON-006：所有唯一索引必须考虑 deleted_at 语义。
DB-COMMON-007：所有状态字段必须有字典或枚举文档。
DB-COMMON-008：所有外键关系即使不建立物理 FK，也必须在文档中声明逻辑约束。
```

#### 30.11.2 `sys_user`

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---:|---|---|
| `id` | bigint | 是 | auto | 主键 |
| `legacy_id` | bigint null | 否 | null | 旧 `think_admin.id` |
| `username` | varchar(64) | 是 | - | 登录名，唯一 |
| `display_name` | varchar(100) | 是 | - | 显示名称 |
| `email` | varchar(191) null | 否 | null | 邮箱 |
| `phone` | varchar(32) null | 否 | null | 手机号 |
| `avatar_file_id` | bigint null | 否 | null | 头像文件 |
| `password_hash` | varchar(255) | 是 | - | 密码哈希 |
| `password_algo` | varchar(32) | 是 | `legacy_php` | 密码算法 |
| `must_change_password` | tinyint | 是 | 0 | 是否强制改密 |
| `status` | tinyint | 是 | 1 | 1 正常，0 禁用，2 锁定 |
| `security_stamp` | varchar(64) | 是 | - | 安全戳 |
| `permission_version` | int | 是 | 1 | 权限版本 |
| `last_login_at` | datetime(3) null | 否 | null | 最后登录时间 |
| `last_login_ip` | varchar(64) null | 否 | null | 最后登录 IP |
| `twofa_enabled` | tinyint | 是 | 0 | 是否启用 2FA |
| `created_at` | datetime(3) | 是 | current | 创建时间 |
| `updated_at` | datetime(3) | 是 | current | 更新时间 |
| `deleted_at` | datetime(3) null | 否 | null | 软删除时间 |
| `row_version` | bigint | 是 | 1 | 并发版本 |

索引：

```text
uk_sys_user_username
idx_sys_user_status
idx_sys_user_deleted_at
idx_sys_user_legacy_id
```

约束：

```text
1. username 全局唯一。
2. 超级管理员不能被普通管理员禁用或删除。
3. 默认查询必须排除 deleted_at 不为空的数据。
4. 修改密码必须更新 security_stamp。
5. 修改用户角色必须更新 permission_version。
```

#### 30.11.3 `sys_role`

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---:|---|---|
| `id` | bigint | 是 | auto | 主键 |
| `legacy_id` | bigint null | 否 | null | 旧 `think_auth_group.id` |
| `code` | varchar(64) | 是 | - | 角色编码，唯一 |
| `name` | varchar(100) | 是 | - | 角色名称 |
| `status` | tinyint | 是 | 1 | 状态 |
| `sort` | int | 是 | 0 | 排序 |
| `data_scope` | varchar(32) | 是 | `all` | 数据范围 |
| `is_system` | tinyint | 是 | 0 | 是否系统内置 |
| `remark` | varchar(500) null | 否 | null | 备注 |
| `created_at` | datetime(3) | 是 | current | 创建时间 |
| `updated_at` | datetime(3) | 是 | current | 更新时间 |
| `deleted_at` | datetime(3) null | 否 | null | 软删除时间 |
| `row_version` | bigint | 是 | 1 | 并发版本 |

索引：

```text
uk_sys_role_code
idx_sys_role_status
idx_sys_role_deleted_at
```

约束：

```text
1. role code 创建后不得随意修改。
2. 系统内置角色不得删除。
3. 删除角色前必须检查用户关联。
4. 修改角色权限必须更新关联用户 permission_version。
```

#### 30.11.4 `sys_menu`

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---:|---|---|
| `id` | bigint | 是 | auto | 主键 |
| `legacy_id` | bigint null | 否 | null | 旧 `think_auth_rule.id` |
| `parent_id` | bigint null | 否 | null | 父菜单 |
| `type` | varchar(32) | 是 | - | catalog/menu/button/external |
| `title` | varchar(100) | 是 | - | 菜单标题 |
| `name` | varchar(100) | 是 | - | 路由名称 |
| `path` | varchar(255) null | 否 | null | 路由路径 |
| `component` | varchar(255) null | 否 | null | 前端组件 key |
| `icon` | varchar(100) null | 否 | null | 图标 |
| `permission_code` | varchar(128) null | 否 | null | 权限码 |
| `visible` | tinyint | 是 | 1 | 是否显示 |
| `keep_alive` | tinyint | 是 | 0 | 是否缓存 |
| `external_url` | varchar(500) null | 否 | null | 外链地址 |
| `status` | tinyint | 是 | 1 | 状态 |
| `sort` | int | 是 | 0 | 排序 |
| `created_at` | datetime(3) | 是 | current | 创建时间 |
| `updated_at` | datetime(3) | 是 | current | 更新时间 |
| `deleted_at` | datetime(3) null | 否 | null | 软删除时间 |
| `row_version` | bigint | 是 | 1 | 并发版本 |

索引：

```text
idx_sys_menu_parent_sort
idx_sys_menu_permission_code
idx_sys_menu_status
```

约束：

```text
1. 菜单树不得形成循环。
2. 菜单层级必须限制最大深度。
3. component 必须在前端白名单中。
4. button 类型可以无 path，但必须绑定 permission_code。
```

#### 30.11.5 `sys_permission`

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---:|---|---|
| `id` | bigint | 是 | auto | 主键 |
| `legacy_id` | bigint null | 否 | null | 旧权限节点 ID |
| `code` | varchar(128) | 是 | - | 权限码，唯一 |
| `name` | varchar(100) | 是 | - | 权限名称 |
| `module` | varchar(64) | 是 | - | 模块 |
| `resource` | varchar(64) | 是 | - | 资源 |
| `action` | varchar(64) | 是 | - | 动作 |
| `http_method` | varchar(16) null | 否 | null | HTTP 方法 |
| `route_pattern` | varchar(255) null | 否 | null | 路由模板 |
| `status` | tinyint | 是 | 1 | 状态 |
| `is_system` | tinyint | 是 | 1 | 是否系统权限 |
| `created_at` | datetime(3) | 是 | current | 创建时间 |
| `updated_at` | datetime(3) | 是 | current | 更新时间 |

索引：

```text
uk_sys_permission_code
idx_sys_permission_module_resource
```

约束：

```text
1. 权限 code 创建后不得随意修改。
2. 系统权限不得物理删除，只能停用或迁移。
3. Endpoint 权限码必须同步进入该表。
```

#### 30.11.6 关系表基线

```text
sys_user_role：user_id、role_id，唯一 user_id + role_id。
sys_role_menu：role_id、menu_id，唯一 role_id + menu_id。
sys_role_permission：role_id、permission_id，唯一 role_id + permission_id。
sys_user_dept：user_id、dept_id、is_primary，唯一 user_id + dept_id。
sys_role_dept：role_id、dept_id，唯一 role_id + dept_id。
```

#### 30.11.7 安全与会话表清单

```text
sys_refresh_token：保存 refresh token hash、设备、IP、过期、吊销信息。
sys_user_session：保存当前活跃会话、设备、登录时间、最后活动时间。
sys_password_reset_token：保存密码找回 token hash、过期、使用状态。
sys_account_lock：保存账号锁定、失败次数、解锁时间。
sys_security_event：保存安全事件。
sys_ip_ban：保存 IP 封禁和解封记录。
```

#### 30.11.8 配置、文件、日志与任务表清单

```text
sys_setting_group：配置分组。
sys_setting：配置项，区分普通配置和敏感配置引用。
sys_setting_change_log：配置变更历史。
sys_file：文件元数据。
sys_file_reference：文件引用关系。
sys_login_log：登录日志。
sys_audit_log：操作审计日志。
sys_request_log：可选请求日志。
sys_dict_type：字典类型。
sys_dict_value：字典值。
sys_job：后台任务。
sys_job_log：任务执行日志。
```

#### 30.11.9 CMS 表清单

```text
cms_channel：栏目。
cms_article：文章主表。
cms_article_content：文章正文。
cms_article_tag：文章标签关系。
cms_tag：标签。
cms_page：单页。
cms_media：媒体库。
cms_link：友情链接。
cms_content_revision：内容版本。
cms_content_publish_log：发布记录。
cms_content_recycle：回收站。
cms_site_setting：站点配置。
```

---

### 30.12 附录 B：API 详细契约清单

API 契约清单必须在开发前维护。每个接口必须包含：

```text
HTTP Method
Path
权限码
是否允许匿名
是否审计
是否限流
是否幂等
请求 DTO
响应 DTO
错误码
对象级授权规则
数据权限规则
```

#### 30.12.1 Auth API

| 方法 | 路径 | 权限 | 审计 | 限流 | 说明 |
|---|---|---|---|---|---|
| POST | `/api/v1/auth/login` | Anonymous | 是 | 是 | 登录 |
| POST | `/api/v1/auth/refresh` | Anonymous | 是 | 是 | 刷新 token |
| POST | `/api/v1/auth/logout` | Authenticated | 是 | 是 | 退出 |
| GET | `/api/v1/auth/me` | Authenticated | 否 | 是 | 当前用户信息 |
| POST | `/api/v1/auth/password` | Authenticated | 是 | 是 | 修改密码 |
| POST | `/api/v1/auth/password/forgot` | Anonymous | 是 | 是 | 忘记密码 |
| POST | `/api/v1/auth/password/reset` | Anonymous | 是 | 是 | 重置密码 |
| GET | `/api/v1/auth/sessions` | Authenticated | 否 | 是 | 我的会话 |
| DELETE | `/api/v1/auth/sessions/{id}` | Authenticated | 是 | 是 | 吊销指定会话 |
| DELETE | `/api/v1/auth/sessions` | Authenticated | 是 | 是 | 吊销全部会话 |
| GET | `/api/v1/auth/captcha` | Anonymous | 否 | 是 | 验证码 |
| POST | `/api/v1/auth/2fa/setup` | Authenticated | 是 | 是 | 初始化 2FA |
| POST | `/api/v1/auth/2fa/enable` | Authenticated | 是 | 是 | 启用 2FA |
| POST | `/api/v1/auth/2fa/disable` | Authenticated | 是 | 是 | 禁用 2FA |
| POST | `/api/v1/auth/2fa/verify` | Anonymous/Auth | 是 | 是 | 验证 2FA |

#### 30.12.2 System API

| 模块 | 方法 | 路径 | 权限码 |
|---|---|---|---|
| 用户 | GET | `/api/v1/system/users` | `sys:user:list` |
| 用户 | GET | `/api/v1/system/users/{id}` | `sys:user:detail` |
| 用户 | POST | `/api/v1/system/users` | `sys:user:create` |
| 用户 | PUT | `/api/v1/system/users/{id}` | `sys:user:update` |
| 用户 | DELETE | `/api/v1/system/users/{id}` | `sys:user:delete` |
| 用户 | POST | `/api/v1/system/users/{id}/password/reset` | `sys:user:reset-password` |
| 用户 | PUT | `/api/v1/system/users/{id}/roles` | `sys:user:assign-role` |
| 用户 | POST | `/api/v1/system/users/{id}/force-logout` | `sys:user:force-logout` |
| 角色 | GET | `/api/v1/system/roles` | `sys:role:list` |
| 角色 | POST | `/api/v1/system/roles` | `sys:role:create` |
| 角色 | PUT | `/api/v1/system/roles/{id}` | `sys:role:update` |
| 角色 | DELETE | `/api/v1/system/roles/{id}` | `sys:role:delete` |
| 角色 | PUT | `/api/v1/system/roles/{id}/menus` | `sys:role:menu:update` |
| 角色 | PUT | `/api/v1/system/roles/{id}/permissions` | `sys:role:permission:update` |
| 菜单 | GET | `/api/v1/system/menus/tree` | `sys:menu:list` |
| 菜单 | POST | `/api/v1/system/menus` | `sys:menu:create` |
| 菜单 | PUT | `/api/v1/system/menus/{id}` | `sys:menu:update` |
| 菜单 | DELETE | `/api/v1/system/menus/{id}` | `sys:menu:delete` |
| 权限 | GET | `/api/v1/system/permissions` | `sys:permission:list` |
| 权限 | POST | `/api/v1/system/permissions/sync` | `sys:permission:sync` |
| 配置 | GET | `/api/v1/system/settings` | `sys:setting:list` |
| 配置 | PUT | `/api/v1/system/settings/{key}` | `sys:setting:update` |
| 字典 | GET | `/api/v1/system/dicts/types` | `sys:dict:list` |
| 字典 | POST | `/api/v1/system/dicts/types` | `sys:dict:create` |
| 文件 | POST | `/api/v1/system/files/upload` | `sys:file:upload` |
| 文件 | GET | `/api/v1/system/files/{id}/download` | `sys:file:download` |
| 日志 | GET | `/api/v1/system/logs/login` | `sys:log:login:list` |
| 日志 | GET | `/api/v1/system/logs/audit` | `sys:log:audit:list` |
| 安全 | GET | `/api/v1/system/security/events` | `sys:security:event:list` |
| 部门 | GET | `/api/v1/system/depts/tree` | `sys:dept:list` |
| 岗位 | GET | `/api/v1/system/posts` | `sys:post:list` |

#### 30.12.3 CMS API

| 模块 | 方法 | 路径 | 权限码 |
|---|---|---|---|
| 栏目 | GET | `/api/v1/cms/channels/tree` | `cms:channel:list` |
| 栏目 | POST | `/api/v1/cms/channels` | `cms:channel:create` |
| 栏目 | PUT | `/api/v1/cms/channels/{id}` | `cms:channel:update` |
| 栏目 | DELETE | `/api/v1/cms/channels/{id}` | `cms:channel:delete` |
| 文章 | GET | `/api/v1/cms/articles` | `cms:article:list` |
| 文章 | GET | `/api/v1/cms/articles/{id}` | `cms:article:detail` |
| 文章 | POST | `/api/v1/cms/articles` | `cms:article:create` |
| 文章 | PUT | `/api/v1/cms/articles/{id}` | `cms:article:update` |
| 文章 | DELETE | `/api/v1/cms/articles/{id}` | `cms:article:delete` |
| 文章 | POST | `/api/v1/cms/articles/{id}/publish` | `cms:article:publish` |
| 文章 | POST | `/api/v1/cms/articles/{id}/offline` | `cms:article:offline` |
| 页面 | GET | `/api/v1/cms/pages` | `cms:page:list` |
| 页面 | POST | `/api/v1/cms/pages` | `cms:page:create` |
| 媒体 | GET | `/api/v1/cms/media` | `cms:media:list` |
| 媒体 | POST | `/api/v1/cms/media/upload` | `cms:media:upload` |
| 媒体 | DELETE | `/api/v1/cms/media/{id}` | `cms:media:delete` |

#### 30.12.4 示例：创建用户契约

```http
POST /api/v1/system/users
```

权限码：

```text
sys:user:create
```

请求：

```json
{
  "username": "editor",
  "displayName": "内容编辑",
  "email": "editor@example.com",
  "phone": "60123456789",
  "roleIds": [2, 3],
  "status": 1
}
```

响应：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": 10001
  }
}
```

规则：

```text
1. username 必须唯一。
2. roleIds 必须存在且启用。
3. 非超级管理员不得授予自己不拥有的角色。
4. 创建成功必须记录审计日志。
5. 创建成功必须初始化 security_stamp 和 permission_version。
6. 创建成功必须要求用户首次登录修改密码，除非设置为系统生成邀请流程。
```

---

### 30.13 附录 C：ThinkPHP 到新系统数据迁移映射

迁移映射必须形成可执行脚本和校验报告。

#### 30.13.1 迁移总规则

```text
MIG-MAP-001：旧系统 token 不迁移。
MIG-MAP-002：旧系统 session 不迁移。
MIG-MAP-003：旧系统 2FA secret 默认不迁移，要求重新绑定。
MIG-MAP-004：旧系统 SMTP 密码等敏感配置默认不自动迁移，要求重新录入或使用 secret reference。
MIG-MAP-005：旧系统 password_hash 可迁移，登录成功后升级为新算法。
MIG-MAP-006：旧系统 role rules CSV 必须拆成关系表。
MIG-MAP-007：旧系统 auth_rule 必须拆分为 menu 和 permission 两类语义。
MIG-MAP-008：所有迁移记录必须保留 legacy_id。
MIG-MAP-009：迁移脚本必须输出异常数据清单。
MIG-MAP-010：迁移后必须执行 row count 和权限矩阵校验。
```

#### 30.13.2 核心表映射

| 旧表 | 旧字段 | 新表 | 新字段 | 迁移规则 |
|---|---|---|---|---|
| `think_admin` | `id` | `sys_user` | `legacy_id` | 保留旧 ID |
| `think_admin` | `username` | `sys_user` | `username` | 原样迁移，需去重检查 |
| `think_admin` | `realname` | `sys_user` | `display_name` | 原样迁移，空值用 username |
| `think_admin` | `password` | `sys_user` | `password_hash` | 原样迁移，标记 `legacy_php` |
| `think_admin` | `groupid` | `sys_user_role` | `role_id` | 通过旧角色映射新角色 |
| `think_admin` | `token` | - | - | 不迁移 |
| `think_admin` | `token_expire_at` | - | - | 不迁移 |
| `think_admin` | `twofa_secret` | - | - | 默认不迁移 |
| `think_admin` | `status` | `sys_user` | `status` | 状态映射 |
| `think_auth_group` | `id` | `sys_role` | `legacy_id` | 保留旧 ID |
| `think_auth_group` | `title` | `sys_role` | `name` | 原样迁移 |
| `think_auth_group` | `rules` | `sys_role_permission` | - | CSV 拆分 |
| `think_auth_group_access` | `uid` | `sys_user_role` | `user_id` | 映射用户 |
| `think_auth_group_access` | `group_id` | `sys_user_role` | `role_id` | 映射角色 |
| `think_auth_rule` | `id` | `sys_menu` / `sys_permission` | `legacy_id` | 按 type / name 拆分 |
| `think_auth_rule` | `name` | `sys_permission` | `legacy_code` | 转换为新权限码 |
| `think_auth_rule` | `title` | `sys_menu` / `sys_permission` | `title` / `name` | 原样迁移 |
| `think_config` | `name` | `sys_setting` | `key` | 按配置分组迁移 |
| `think_config` | `value` | `sys_setting` | `value` | 敏感配置脱敏或重录 |
| `think_file` | `id` | `sys_file` | `legacy_id` | 保留旧 ID |
| `think_file` | `path` | `sys_file` | `storage_key` | 转换存储 key |
| `think_log` | - | `sys_login_log` | - | 按字段映射 |
| `think_operate_log` | - | `sys_audit_log` | - | 按字段映射 |
| `think_i18n_message` | - | `sys_i18n_message` | - | 按 locale + key 迁移 |
| `think_notice` | - | `sys_notice` | - | 保留公告数据 |
| `think_mail_notify` | - | `sys_mail_template` / `sys_mail_outbox` | - | 按语义拆分 |
| `think_msg_sender` | - | `sys_message_sender` | - | 按语义迁移 |

#### 30.13.3 权限路径映射示例

| 旧权限路径 | 新权限码 |
|---|---|
| `/user/index` | `sys:user:list` |
| `/user/add` | `sys:user:create` |
| `/user/edit` | `sys:user:update` |
| `/user/del` | `sys:user:delete` |
| `/user/state` | `sys:user:update-status` |
| `/role/index` | `sys:role:list` |
| `/role/add` | `sys:role:create` |
| `/role/edit` | `sys:role:update` |
| `/role/del` | `sys:role:delete` |
| `/role/giveAccess` | `sys:role:permission:update` |
| `/menu/index` | `sys:menu:list` |
| `/menu/add` | `sys:menu:create` |
| `/menu/edit` | `sys:menu:update` |
| `/menu/del` | `sys:menu:delete` |
| `/setting/index` | `sys:setting:list` |
| `/setting/save` | `sys:setting:update` |
| `/file/upload` | `sys:file:upload` |
| `/logmanage/index` | `sys:log:audit:list` |

#### 30.13.4 迁移校验项

```text
1. 旧用户数量与新用户数量。
2. 旧角色数量与新角色数量。
3. 旧用户角色关系数量与新用户角色关系数量。
4. 旧权限节点数量与新菜单/权限数量。
5. 旧 role.rules 拆分后的关系数量。
6. 新权限矩阵与 Endpoint 权限码数量。
7. 文件元数据数量。
8. 文件物理存在率。
9. 登录日志数量。
10. 操作日志数量。
11. 配置项数量。
12. 异常用户、异常角色、异常权限、缺失文件清单。
```

---

### 30.14 附录 D：权限矩阵与菜单按钮清单

权限矩阵是后端权限码、前端按钮权限、测试用例、初始化种子数据的共同来源。

权限矩阵规则：

```text
PERM-MATRIX-DELIVERY-001：每个菜单必须登记对应权限码。
PERM-MATRIX-DELIVERY-002：每个按钮必须登记对应权限码。
PERM-MATRIX-DELIVERY-003：每个写接口必须登记对应权限码。
PERM-MATRIX-DELIVERY-004：权限矩阵必须进入版本管理。
PERM-MATRIX-DELIVERY-005：权限矩阵变更必须触发权限同步。
PERM-MATRIX-DELIVERY-006：前端按钮权限必须来自权限矩阵和后端 permissions。
```

权限矩阵示例：

| 模块 | 菜单 | 动作 | 权限码 | API | 前端按钮 |
|---|---|---|---|---|---|
| 用户管理 | 用户列表 | 查看 | `sys:user:list` | `GET /api/v1/system/users` | 页面访问 |
| 用户管理 | 用户列表 | 新增 | `sys:user:create` | `POST /api/v1/system/users` | 新增按钮 |
| 用户管理 | 用户列表 | 编辑 | `sys:user:update` | `PUT /api/v1/system/users/{id}` | 编辑按钮 |
| 用户管理 | 用户列表 | 删除 | `sys:user:delete` | `DELETE /api/v1/system/users/{id}` | 删除按钮 |
| 用户管理 | 用户列表 | 重置密码 | `sys:user:reset-password` | `POST /api/v1/system/users/{id}/password/reset` | 重置密码按钮 |
| 用户管理 | 用户列表 | 分配角色 | `sys:user:assign-role` | `PUT /api/v1/system/users/{id}/roles` | 分配角色按钮 |
| 角色管理 | 角色列表 | 查看 | `sys:role:list` | `GET /api/v1/system/roles` | 页面访问 |
| 角色管理 | 角色列表 | 新增 | `sys:role:create` | `POST /api/v1/system/roles` | 新增按钮 |
| 角色管理 | 角色列表 | 编辑 | `sys:role:update` | `PUT /api/v1/system/roles/{id}` | 编辑按钮 |
| 角色管理 | 角色列表 | 删除 | `sys:role:delete` | `DELETE /api/v1/system/roles/{id}` | 删除按钮 |
| 角色管理 | 角色列表 | 分配菜单 | `sys:role:menu:update` | `PUT /api/v1/system/roles/{id}/menus` | 菜单授权按钮 |
| 角色管理 | 角色列表 | 分配权限 | `sys:role:permission:update` | `PUT /api/v1/system/roles/{id}/permissions` | 权限授权按钮 |
| 菜单管理 | 菜单树 | 查看 | `sys:menu:list` | `GET /api/v1/system/menus/tree` | 页面访问 |
| 菜单管理 | 菜单树 | 新增 | `sys:menu:create` | `POST /api/v1/system/menus` | 新增按钮 |
| 菜单管理 | 菜单树 | 编辑 | `sys:menu:update` | `PUT /api/v1/system/menus/{id}` | 编辑按钮 |
| 菜单管理 | 菜单树 | 删除 | `sys:menu:delete` | `DELETE /api/v1/system/menus/{id}` | 删除按钮 |
| 权限管理 | 权限列表 | 查看 | `sys:permission:list` | `GET /api/v1/system/permissions` | 页面访问 |
| 权限管理 | 权限列表 | 同步 | `sys:permission:sync` | `POST /api/v1/system/permissions/sync` | 同步按钮 |
| 配置管理 | 配置列表 | 查看 | `sys:setting:list` | `GET /api/v1/system/settings` | 页面访问 |
| 配置管理 | 配置列表 | 修改 | `sys:setting:update` | `PUT /api/v1/system/settings/{key}` | 保存按钮 |
| 文件管理 | 文件列表 | 查看 | `sys:file:list` | `GET /api/v1/system/files` | 页面访问 |
| 文件管理 | 文件列表 | 上传 | `sys:file:upload` | `POST /api/v1/system/files/upload` | 上传按钮 |
| 文件管理 | 文件列表 | 下载 | `sys:file:download` | `GET /api/v1/system/files/{id}/download` | 下载按钮 |
| 文件管理 | 文件列表 | 删除 | `sys:file:delete` | `DELETE /api/v1/system/files/{id}` | 删除按钮 |
| 日志管理 | 登录日志 | 查看 | `sys:log:login:list` | `GET /api/v1/system/logs/login` | 页面访问 |
| 日志管理 | 操作日志 | 查看 | `sys:log:audit:list` | `GET /api/v1/system/logs/audit` | 页面访问 |
| 安全中心 | 安全事件 | 查看 | `sys:security:event:list` | `GET /api/v1/system/security/events` | 页面访问 |
| CMS | 栏目管理 | 查看 | `cms:channel:list` | `GET /api/v1/cms/channels/tree` | 页面访问 |
| CMS | 文章管理 | 发布 | `cms:article:publish` | `POST /api/v1/cms/articles/{id}/publish` | 发布按钮 |
| CMS | 媒体库 | 上传 | `cms:media:upload` | `POST /api/v1/cms/media/upload` | 上传按钮 |

---

### 30.15 附录 E：SoybeanAdmin 页面与路由实现清单

页面实现清单用于确保 SoybeanAdmin 不偏离后端契约。

| 页面 | 路由 | 组件路径 | 权限码 | API |
|---|---|---|---|---|
| 登录 | `/login` | `views/login/index.vue` | Anonymous | `/api/v1/auth/login` |
| 2FA 验证 | `/login/2fa` | `views/login/two-factor.vue` | Anonymous | `/api/v1/auth/2fa/verify` |
| 个人中心 | `/profile` | `views/profile/index.vue` | Authenticated | `/api/v1/auth/me` |
| 修改密码 | `/profile/password` | `views/profile/password.vue` | Authenticated | `/api/v1/auth/password` |
| 用户管理 | `/system/user` | `views/system/user/index.vue` | `sys:user:list` | `/api/v1/system/users` |
| 角色管理 | `/system/role` | `views/system/role/index.vue` | `sys:role:list` | `/api/v1/system/roles` |
| 菜单管理 | `/system/menu` | `views/system/menu/index.vue` | `sys:menu:list` | `/api/v1/system/menus/tree` |
| 权限管理 | `/system/permission` | `views/system/permission/index.vue` | `sys:permission:list` | `/api/v1/system/permissions` |
| 配置管理 | `/system/setting` | `views/system/setting/index.vue` | `sys:setting:list` | `/api/v1/system/settings` |
| 字典管理 | `/system/dict` | `views/system/dict/index.vue` | `sys:dict:list` | `/api/v1/system/dicts/types` |
| 文件管理 | `/system/file` | `views/system/file/index.vue` | `sys:file:list` | `/api/v1/system/files` |
| 登录日志 | `/system/log/login` | `views/system/log/login.vue` | `sys:log:login:list` | `/api/v1/system/logs/login` |
| 操作日志 | `/system/log/audit` | `views/system/log/audit.vue` | `sys:log:audit:list` | `/api/v1/system/logs/audit` |
| 安全事件 | `/system/security/event` | `views/system/security/event.vue` | `sys:security:event:list` | `/api/v1/system/security/events` |
| 部门管理 | `/system/dept` | `views/system/dept/index.vue` | `sys:dept:list` | `/api/v1/system/depts/tree` |
| 岗位管理 | `/system/post` | `views/system/post/index.vue` | `sys:post:list` | `/api/v1/system/posts` |
| 通知公告 | `/system/notice` | `views/system/notice/index.vue` | `sys:notice:list` | `/api/v1/system/notices` |
| 任务管理 | `/system/job` | `views/system/job/index.vue` | `sys:job:list` | `/api/v1/system/jobs` |
| CMS 栏目 | `/cms/channel` | `views/cms/channel/index.vue` | `cms:channel:list` | `/api/v1/cms/channels/tree` |
| CMS 文章 | `/cms/article` | `views/cms/article/index.vue` | `cms:article:list` | `/api/v1/cms/articles` |
| CMS 页面 | `/cms/page` | `views/cms/page/index.vue` | `cms:page:list` | `/api/v1/cms/pages` |
| CMS 媒体 | `/cms/media` | `views/cms/media/index.vue` | `cms:media:list` | `/api/v1/cms/media` |

页面交付规则：

```text
FE-PAGE-DELIVERY-001：每个页面必须登记路由、组件、权限码、API。
FE-PAGE-DELIVERY-002：每个页面必须列出按钮级权限。
FE-PAGE-DELIVERY-003：每个页面必须列出表格列和筛选项。
FE-PAGE-DELIVERY-004：每个弹窗表单必须对应后端 Request DTO。
FE-PAGE-DELIVERY-005：每个页面必须处理 loading、empty、error、403 状态。
FE-PAGE-DELIVERY-006：每个页面不得消费 SoybeanAdmin mock 数据。
```

---

### 30.16 附录 F：测试用例矩阵

测试矩阵必须覆盖认证、授权、对象级授权、迁移、AOT、前端路由和按钮权限。

| 编号 | 模块 | 场景 | 预期 | 等级 |
|---|---|---|---|---|
| AUTH-001 | 登录 | 正确账号密码登录 | 返回 token | P0 |
| AUTH-002 | 登录 | 错误密码连续失败 | 触发限流或验证码 | P0 |
| AUTH-003 | 登录 | 禁用用户登录 | 返回禁止登录 | P0 |
| AUTH-004 | Refresh | 使用已吊销 refresh token | 返回 401 | P0 |
| AUTH-005 | 2FA | 启用 2FA 用户登录 | 要求二次验证 | P0 |
| AUTHZ-001 | 权限 | 无 `sys:user:list` 访问用户列表 | 返回 403 | P0 |
| AUTHZ-002 | 权限 | 有编辑权限但编辑超级管理员 | 返回 403 | P0 |
| AUTHZ-003 | 权限 | 删除自己 | 返回业务错误 | P0 |
| AUTHZ-004 | 权限 | 修改角色权限后用户权限刷新 | permission_version 更新 | P0 |
| USER-001 | 用户 | 创建用户 | 用户创建，审计日志写入 | P0 |
| USER-002 | 用户 | 重复 username | 返回唯一约束错误 | P0 |
| ROLE-001 | 角色 | 删除已分配用户的角色 | 拒绝或要求先解除关联 | P0 |
| MENU-001 | 菜单 | 创建循环菜单 | 拒绝 | P0 |
| FILE-001 | 文件 | 上传 `.php` 文件 | 拒绝 | P0 |
| FILE-002 | 文件 | 下载无权限文件 | 返回 403 | P0 |
| LOG-001 | 审计 | 写接口执行 | 产生审计日志 | P0 |
| SETTING-001 | 配置 | 修改敏感配置 | 返回脱敏值，审计记录 | P0 |
| MIG-001 | 迁移 | 用户数量校验 | 新旧数量一致或差异有说明 | P0 |
| MIG-002 | 迁移 | 角色权限 CSV 拆分 | 关系表数量符合预期 | P0 |
| AOT-001 | AOT | linux-x64 PublishAot | 发布成功 | P0 |
| OPENAPI-001 | 契约 | OpenAPI 生成 | 成功 | P0 |
| FE-001 | 前端 | 登录后加载动态路由 | 菜单正确显示 | P0 |
| FE-002 | 前端 | 无按钮权限 | 按钮不显示 | P0 |
| CMS-001 | CMS | 发布文章 | 状态变为 published，记录发布日志 | P1 |
| CMS-002 | CMS | 删除文章 | 进入回收站 | P1 |
| WAF-001 | WAF | SQL 注入 payload | WAF 或应用拒绝 | P1 |
| PERF-001 | 性能 | `/api/auth/me` P95 | 达到预算 | P1 |

测试规则：

```text
TEST-MATRIX-001：P0 用例必须进入 CI 或发布前验收。
TEST-MATRIX-002：权限测试必须覆盖 401、403、对象级授权、数据权限。
TEST-MATRIX-003：迁移测试必须输出 row count 和异常数据报告。
TEST-MATRIX-004：AOT 测试必须使用发布后的可执行文件运行 smoke test。
TEST-MATRIX-005：前端测试必须覆盖动态菜单、动态路由、按钮权限。
```

---

### 30.17 附录 G：详细数据库对象清单

本清单用于防止开发时遗漏基础表。

P0 表：

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_menu
sys_role_permission
sys_refresh_token
sys_user_session
sys_password_reset_token
sys_setting_group
sys_setting
sys_setting_change_log
sys_file
sys_file_reference
sys_login_log
sys_audit_log
sys_security_event
sys_dict_type
sys_dict_value
```

P1 表：

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
sys_job
sys_job_log
sys_system_maintenance_log
```

CMS 表：

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

---

### 30.18 附录 H：详细 API 端点清单

Auth：

```text
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout
GET    /api/v1/auth/me
POST   /api/v1/auth/password
POST   /api/v1/auth/password/forgot
POST   /api/v1/auth/password/reset
GET    /api/v1/auth/sessions
DELETE /api/v1/auth/sessions/{id}
DELETE /api/v1/auth/sessions
GET    /api/v1/auth/captcha
POST   /api/v1/auth/2fa/setup
POST   /api/v1/auth/2fa/enable
POST   /api/v1/auth/2fa/disable
POST   /api/v1/auth/2fa/verify
POST   /api/v1/auth/2fa/backup-codes/regenerate
```

System：

```text
GET    /api/v1/system/users
GET    /api/v1/system/users/{id}
POST   /api/v1/system/users
PUT    /api/v1/system/users/{id}
DELETE /api/v1/system/users/{id}
POST   /api/v1/system/users/{id}/password/reset
PUT    /api/v1/system/users/{id}/roles
POST   /api/v1/system/users/{id}/force-logout
POST   /api/v1/system/users/{id}/unlock

GET    /api/v1/system/roles
GET    /api/v1/system/roles/{id}
POST   /api/v1/system/roles
PUT    /api/v1/system/roles/{id}
DELETE /api/v1/system/roles/{id}
GET    /api/v1/system/roles/{id}/menus
PUT    /api/v1/system/roles/{id}/menus
GET    /api/v1/system/roles/{id}/permissions
PUT    /api/v1/system/roles/{id}/permissions

GET    /api/v1/system/menus/tree
POST   /api/v1/system/menus
PUT    /api/v1/system/menus/{id}
DELETE /api/v1/system/menus/{id}
PATCH  /api/v1/system/menus/sort

GET    /api/v1/system/permissions
POST   /api/v1/system/permissions/sync

GET    /api/v1/system/settings
PUT    /api/v1/system/settings/{key}
GET    /api/v1/system/dicts/types
POST   /api/v1/system/dicts/types
PUT    /api/v1/system/dicts/types/{id}
DELETE /api/v1/system/dicts/types/{id}

POST   /api/v1/system/files/upload
GET    /api/v1/system/files
GET    /api/v1/system/files/{id}/download
DELETE /api/v1/system/files/{id}

GET    /api/v1/system/logs/login
GET    /api/v1/system/logs/audit
GET    /api/v1/system/security/events
```

CMS：

```text
GET    /api/v1/cms/channels/tree
POST   /api/v1/cms/channels
PUT    /api/v1/cms/channels/{id}
DELETE /api/v1/cms/channels/{id}

GET    /api/v1/cms/articles
GET    /api/v1/cms/articles/{id}
POST   /api/v1/cms/articles
PUT    /api/v1/cms/articles/{id}
DELETE /api/v1/cms/articles/{id}
POST   /api/v1/cms/articles/{id}/publish
POST   /api/v1/cms/articles/{id}/offline
POST   /api/v1/cms/articles/{id}/restore

GET    /api/v1/cms/pages
POST   /api/v1/cms/pages
PUT    /api/v1/cms/pages/{id}
POST   /api/v1/cms/pages/{id}/publish

GET    /api/v1/cms/media
POST   /api/v1/cms/media/upload
DELETE /api/v1/cms/media/{id}
```

---

### 30.19 工程落地最终结论

本章新增的工程交付内容必须作为后续开发项目的执行依据。最终落地不应只停留在架构和规则层面，而必须形成以下硬产物：

```text
1. 数据库详细设计。
2. API 详细契约。
3. 旧系统数据迁移映射。
4. 权限矩阵。
5. SoybeanAdmin 页面清单。
6. 后端代码骨架。
7. 前端代码骨架。
8. OpenAPI 与类型生成流程。
9. 测试用例矩阵。
10. 实施里程碑。
11. 上线验收清单。
12. 旧系统冻结与切换策略。
```

一句话总结：

```text
前文定义“系统如何设计、如何约束、如何安全运行”；
本章定义“开发人员具体按什么表、什么接口、什么页面、什么任务去实现”。
```

---



