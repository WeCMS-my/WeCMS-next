# ADR-0009：运行时基线从 Native AOT 切换为 JIT

## 状态

Accepted

## 背景

WeCMS Next 现阶段继续采用：

- ASP.NET Core Minimal API
- `.NET 10`
- `WebApplication.CreateSlimBuilder(args)`
- OpenAPI 契约优先
- SqlSugar 作为数据库适配方向

但项目不再以 Native AOT 作为强制运行时与发布门禁。此前围绕 AOT 建立的项目属性、警告治理、发布脚本、ADR 和 Spike 研究，已经与当前目标不一致。

## 决策

1. 后端运行时基线改为 `.NET 10 JIT publish/runtime`。
2. 不将本次变更扩展为 MVC Controller 架构迁移；继续使用 Minimal API。
3. 不将本次变更扩展为 `CreateBuilder` 迁移；继续使用 `CreateSlimBuilder`。
4. SqlSugar 继续作为 ORM/数据库适配方向，但不再要求以 AOT 兼容性作为准入前提。
5. `WeCms.Persistence` 继续作为唯一允许直接引用 ORM / 数据库连接器的生产数据库适配层。
6. 所有 AOT 专属门禁、分析器、例外基线、发布要求从现行治理中移除。

## 保留不变的规则

- Minimal API Only
- 显式注册 Endpoint
- 禁止运行时 Endpoint 扫描
- 禁止 MVC Controller、Razor、EF Core
- OpenAPI 作为前后端契约来源
- `WeCms.Modules.*` 不得直接引用 SqlSugar / MySQL 连接器
- 禁止 `dynamic`
- 禁止 `SELECT *`
- 禁止拼接用户输入到 SQL

## 影响

### 正向影响

- 降低运行时基线复杂度。
- 取消与当前目标不一致的 AOT-only 文档和脚本约束。
- 允许后端与 SqlSugar 按常规 JIT 路径推进。

### 代价

- 不再使用 Native AOT 相关警告分析作为当前主线门禁。
- 旧 AOT 研究资料必须明确归档，避免误导后续开发。

## 新的后端验收命令

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

## 替代关系

- 本 ADR 取代 `ADR-0006` 作为当前运行时基线治理依据。
- `docs/specs/sqlsugar-aot-spike/*` 仅保留为历史 AOT 调研材料，不再是现行准入门槛。

