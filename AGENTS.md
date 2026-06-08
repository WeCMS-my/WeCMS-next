# WeCMS Next Agent Instructions

## 项目目标

WeCMS 从 ThinkPHP 迁移重构为：

- ASP.NET Core Minimal APIs
- .NET 10
- Native AOT Only
- Dapper / Dapper.AOT
- MySQL
- SoybeanAdmin
- 后端契约优先

## 最高优先级规则

1. 前端一切数据格式以后端为准，不可随意修改。
2. SoybeanAdmin 只是 UI 模板，不是 API 契约来源。
3. AI 接入是二期独立项目，当前不得实现运行时 AI 功能。
4. 当前不得创建 WeCms.Modules.Ai、AI Provider、RAG、Prompt 模板、Vector Store。
5. AI 项目后期只能通过 CMS Core API 获取数据，严禁直接读取 CMS 数据库。

## 后端规则

1. 只允许 ASP.NET Core Minimal APIs。
2. 只允许 .NET 10 Native AOT 发布。
3. 必须使用 CreateSlimBuilder。
4. 禁止 MVC Controller。
5. 禁止 Razor。
6. 禁止运行时 Endpoint 扫描。
7. 禁止 EF Core。
8. 禁止 dynamic。
9. 禁止 SELECT *。
10. 禁止拼接用户输入 SQL。
11. 所有 DTO 必须加入 JsonSerializerContext。
12. 除 AllowAnonymous 外，所有业务 Endpoint 必须绑定权限码。
13. 所有写操作必须审计。
14. 所有列表必须分页。

## 数据访问规则

1. 使用 Dapper / Dapper.AOT。
2. Repository 只负责 SQL。
3. Service / UseCase 负责业务事务。
4. 所有 SQL 显式字段。
5. 排序字段必须白名单。
6. 分页 pageSize 最大 100。
7. 所有 Repository 方法必须支持 CancellationToken。

## 前端规则

1. 使用 SoybeanAdmin。
2. OpenAPI / 后端 DTO 是 TypeScript 类型来源。
3. generated 目录禁止手写。
4. 不得使用 SoybeanAdmin mock 类型作为正式契约。
5. request interceptor 不得重塑业务 data。
6. 动态菜单只能来自后端菜单 DTO。
7. 按钮权限只能来自后端 permissions。

## 验证要求

每次代码变更后必须说明如何运行：

- dotnet build -warnaserror
- dotnet test
- dotnet publish -c Release -r linux-x64 /p:PublishAot=true
- pnpm typecheck
- pnpm lint
- pnpm build