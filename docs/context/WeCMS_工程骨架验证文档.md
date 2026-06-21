# WeCMS Next 工程骨架验证文档

> 文档版本：JIT 基线版  
> 适用阶段：M0-BE backend-only 工程启动阶段  
> 技术栈：ASP.NET Core Minimal APIs、.NET 10、JIT publish/runtime、SqlSugar ORM、MySQL、SoybeanAdmin

---

## 1. 文档定位

本文件用于指导 WeCMS Next 的第一个工程验证阶段：

```text
M0-BE：工程骨架搭建 + JIT 发布验证 + SqlSugar ORM 数据访问验证 + OpenAPI 契约闭环
```

M0 的目标不是做完整后台，而是证明：

- 技术栈可行
- 工程结构可行
- 数据访问边界可行
- 契约生成可行
- backend-only 交付路径可行

---

## 2. M0-BE 阶段目标

```text
1. 新仓库与目录结构创建完成
2. .NET 10 Minimal API 项目可运行
3. JIT publish 可成功
4. MySQL 可连接
5. SqlSugar ORM 强类型查询可运行
6. OpenAPI JSON 可生成
7. 登录、刷新、退出、/auth/me 最小闭环可运行
8. 权限码元数据和权限过滤器可运行
9. CI 能执行 build、test、publish
10. ThinkPHP 用户、角色、菜单、权限迁移 Spike 可输出报告
```

---

## 3. M0 非目标

```text
1. 不开发完整 CMS 文章模块
2. 不开发复杂多租户
3. 不做完整 2FA
4. 不做所有 SoybeanAdmin 页面
5. 不一次性迁移全部旧数据
6. 不为了 UI 细节修改后端契约
7. 不引入大量第三方包
8. 不把本次运行时基线切换扩大成 MVC 架构重构
9. 不修改 `frontend/**`
10. 不运行 `pnpm`
11. 不生成前端 TypeScript generated
```

---

## 4. 推荐结构

```text
wecms-next/
  backend/
    src/
      WeCms.Api/
      WeCms.Shared/
      WeCms.Infrastructure/
      WeCms.Data.SqlSugar/
      WeCms.Modules.Identity/
      WeCms.Modules.AccessControl/
      WeCms.Modules.Organization/
      WeCms.Modules.Configuration/
      WeCms.Modules.Audit/
      WeCms.Modules.Security/
      WeCms.Modules.FileCenter/
      WeCms.Modules.Platform/
      WeCms.Modules.*.SqlSugar/
      WeCms.Modules.Cms/   # 二期内容模块占位，系统基础升级期间不启用
    tests/
      WeCms.Tests.Unit/
      WeCms.Tests.Integration/
      WeCms.Tests.Architecture/
  frontend/
    soybean-admin/
  docs/
    context/
    specs/
    adr/
  artifacts/
    openapi/
```

---

## 5. 关键约束

1. Minimal API Only
2. `CreateSlimBuilder`
3. Endpoint 显式注册
4. OpenAPI 契约优先
5. SqlSugar ORM 只允许在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar`
6. `WeCms.Modules.*` 不得包含 SQL 文本、`SqlSugarClient`、`ISqlSugarClient`、`MySqlConnector`
7. 禁止 `dynamic`
8. 禁止 `SELECT *`

---

## 6. M0-BE 验证命令

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

说明：

- 当前 M0-BE 为 backend-only，不执行前端 `pnpm` 门禁
- 前端 typecheck / lint / build 仅在后续前端阶段适用

---

## 7. M0-BE 验收项

```text
[ ] Minimal API Host 可运行
[ ] ApiResult / PagedResult 可用
[ ] ExceptionMiddleware 可用
[ ] JsonSerializerContext 可用
[ ] Health endpoint 可用
[ ] SqlSugar ORM 查询成功
[ ] OpenAPI 可生成
[ ] JIT publish 通过
[ ] 数据库边界规则通过
[ ] 数据库/ORM/连接器集中在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar`
[ ] Repository interface / implementation 与接口 + DI 规则通过
[ ] `frontend/**` 无改动，未运行 `pnpm`
```

---

## 8. 风险提示

- SqlSugar 项目高度依赖 SQL 纪律，必须坚持显式字段和强类型 DTO。
- JIT 基线并不放松分层、契约和安全要求。
- Minimal API 需要持续保持 endpoint 显式注册，避免 `Program.cs` 膨胀。
- SoybeanAdmin 联通验证属于后续前端阶段，不应反向驱动当前 backend-only 阶段放宽契约或数据库边界。
