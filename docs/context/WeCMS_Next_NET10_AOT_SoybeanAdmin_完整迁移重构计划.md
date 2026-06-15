# WeCMS Next .NET 10 JIT + SoybeanAdmin 完整迁移重构计划

> 说明：文件名保留历史路径以避免引用漂移，但当前正文已经切换为 **JIT 运行时基线**。  
> 说明：本文件是历史命名路径下的总览文档，**不是** M0-BE 当前执行入口；当前 backend-only 主计划请优先阅读 `docs/context/WeCMS Next M0-BE 后端-only 开发计划 v2.0.md`。  
> 目标技术栈：ASP.NET Core Minimal APIs / .NET 10 / JIT publish/runtime / SqlSugar ORM / SoybeanAdmin

---

## 1. 文档定位

本文档描述 WeCMS Next 的长期迁移重构总览。它回答以下问题：

1. 后端采用什么 API 编程模型
2. 后端采用什么运行时与发布基线
3. 数据访问和模块边界如何约束
4. 前后端契约如何交付
5. 迁移阶段如何分步推进

当前结论：

- 后端继续使用 ASP.NET Core Minimal APIs
- 宿主继续使用 `WebApplication.CreateSlimBuilder(args)`
- 运行时基线为 JIT publish/runtime
- ORM 方向为 SqlSugar
- OpenAPI 仍是前后端契约来源
- 当前 M0-BE 阶段仍是 backend-only，前端开发与前端 generated 类型生成继续后移

---

## 2. 核心架构决策

### 2.1 保留不变

- Minimal API
- `CreateSlimBuilder`
- 后端契约优先
- 模块化单体
- OpenAPI 作为契约来源
- SqlSugar 仅限 `WeCms.Persistence`

### 2.2 已变更

- 从 `Native AOT Only` 切换到 `JIT publish/runtime`
- 不再将 AOT publish 作为强制发布门禁
- 不再以 AOT 兼容性作为 SqlSugar 准入前提

### 2.3 不在本次变更范围

- 不改成 MVC Controller
- 不改成 `CreateBuilder`
- 不更换 SqlSugar ORM
- 不放松数据库边界

---

## 3. 技术栈

| 维度 | 当前基线 |
|---|---|
| 后端 API 模型 | ASP.NET Core Minimal APIs |
| 运行时 | .NET 10 |
| 发布方式 | JIT publish/runtime |
| Host 启动 | `WebApplication.CreateSlimBuilder(args)` |
| ORM | SqlSugar ORM |
| 数据库 | MySQL |
| 前端 | SoybeanAdmin（后移，当前 M0-BE 不开发） |
| 契约来源 | OpenAPI |

---

## 4. 后端架构约束

1. 只允许 Minimal API。
2. 必须显式注册 Endpoint。
3. 禁止 MVC Controller、Razor、运行时 Endpoint 扫描。
4. 禁止动态代理 AOP、runtime code generation。
5. DTO 必须进入 `System.Text.Json` Source Generator。
6. OpenAPI 是前后端契约交付物。
7. `CreateSlimBuilder` 为当前宿主启动基线。

---

## 5. SqlSugar ORM 约束

1. SqlSugar 只允许在 `WeCms.Persistence` 注册和使用。
2. `WeCms.Modules.*` 只能定义 repository port，不得持有 `SqlSugarClient`、`ISqlSugarClient`、连接对象或 SQL 文本。
3. 禁止 `dynamic` 查询/返回。
4. 禁止 `SELECT *`。
5. 所有 SQL 必须显式列字段。
6. 排序字段必须白名单映射。
7. Repository 方法必须支持 `CancellationToken`。
8. Service / UseCase 负责业务规则和事务边界。
9. Repository interface 只保留在模块层或 `WeCms.Shared`，Repository implementation 只允许存在于 `WeCms.Persistence`。
10. Service / UseCase 获取 Repository、UnitOfWork、Clock、Token、密码、随机数等有副作用依赖时，必须通过接口 + DI。

---

## 6. 运行时与发布基线

当前发布基线：

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

说明：

- 当前要求是普通 JIT publish 成功
- 不再要求 `PublishAot=true`
- 不再要求 `IsAotCompatible=true`
- 不再要求 `EnableAotAnalyzer=true`

---

## 7. 目录与模块边界

```text
backend/
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
frontend/
  soybean-admin/
artifacts/
  openapi/
docs/
  context/
  specs/
  adr/
```

边界要求：

- `WeCms.Persistence` 是唯一数据库适配器层
- `WeCms.Modules.*` 不得反向依赖 `WeCms.Persistence`
- 所有数据库访问只能发生在 `WeCms.Persistence`
- `WeCms.Modules.*` 不得持有 SQL 文本、ORM Client、数据库连接或 Persistence implementation
- `WeCms.Shared` 不得引用其它生产工程
- `WeCms.Infrastructure` 不做数据库适配层，也不得持有 SQL 文本、ORM Client、数据库连接或 Persistence implementation

---

## 8. 迁移阶段计划

### M0：工程骨架验证

```text
.NET solution
Minimal API
ApiResult
ExceptionMiddleware
JsonSerializerContext
Health endpoint
SqlSugar ORM db-check
OpenAPI 生成
JIT publish gate
ThinkPHP migration spike
```

### M1：认证安全闭环

```text
登录
刷新 token
退出
/auth/me
登录日志
安全事件
```

### M2：用户、角色、菜单、权限

```text
用户管理
角色管理
菜单管理
权限管理
动态路由
按钮权限
```

### M3：系统基础模块

```text
配置
字典
文件
日志
安全中心
组织架构
通知公告
任务维护
```

### M4：CMS 内容模块

```text
栏目
文章
单页
媒体库
标签
发布/下架
版本/回收站
公开内容 API
```

---

## 9. 验收原则

1. 当前发布要求是 `build + test + publish` 全部通过。
2. OpenAPI 必须可生成，并作为后续前端 generated 类型的唯一契约来源。
3. SqlSugar 必须满足数据库边界与 SQL 纪律。
4. Minimal API、`CreateSlimBuilder`、契约优先继续保留。
5. 一期不得实现运行时 AI 功能。
6. 当前 M0-BE 阶段不修改 `frontend/**`，不运行 `pnpm`，不生成前端 TypeScript generated。
