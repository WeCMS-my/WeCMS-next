# WeCMS Trae Engineering Principles

> 适用范围：WeCMS Next 一期开发，包括后端、前端、数据库迁移、测试、CI/CD、文档和 AI 协作。  
> 最高优先级：本规则与 `AGENTS.md`、`code_review.md`、`docs/context/*` 同时生效。

---

## 0. 项目硬边界

### 0.1 技术栈硬约束

- 后端采用 **ASP.NET Core Minimal APIs**
- 运行目标为 **.NET 10**
- 运行时基线为 **JIT publish/runtime**
- 必须使用 `CreateSlimBuilder()`
- 数据访问采用 **SqlSugar ORM**
- 前端采用 **SoybeanAdmin**
- 前端一切数据格式以后端 DTO / OpenAPI 为准

### 0.2 禁止项

- 禁止 MVC Controller
- 禁止 Razor
- 禁止 EF Core
- 禁止运行时 Endpoint 扫描
- 禁止动态代理 AOP
- 禁止 runtime code generation
- 禁止业务路径使用 `dynamic`
- 禁止 `SELECT *`
- 禁止拼接用户输入 SQL
- 禁止为了前端模板修改后端 API 契约
- 禁止在一期实现运行时 AI 功能

### 0.3 AI 二期边界

- 不得创建 `WeCms.Modules.Ai`
- 不得创建 AI Provider
- 不得在后端调用模型 API
- 不得在前端增加 AI 业务页面
- AI 二期若存在，必须是独立项目，且只能通过 CMS Core API 获取数据

---

## 1. OOP / SOLID

1. 新增有副作用服务类必须先定义 `I*` 接口。
2. 依赖必须通过构造函数注入。
3. 禁止在业务类内部 `new` 出有副作用依赖。
4. 单类不得承担复合职责。
5. 跨阶段模型优先使用只读或不可变模型。
6. 业务模块依赖抽象，不依赖具体基础设施实现。
7. 若难以测试，必须先调整设计。

---

## 2. 高内聚低耦合

### 2.1 依赖矩阵

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Persistence
  -> WeCms.Shared

WeCms.Modules.System / WeCms.Modules.Cms
  -> WeCms.Shared

WeCms.Persistence
  -> WeCms.Shared
  -> WeCms.Modules.System / WeCms.Modules.Cms（仅用于实现 repository port）

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> 不得引用其它生产工程
```

### 2.2 数据库边界

- `WeCms.Persistence` 是唯一允许直接引用 ORM / 数据库连接器的生产项目
- `WeCms.Modules.*` 不得出现 SQL 文本
- `WeCms.Modules.*` 不得直接引用 `SqlSugar ORM`、`MySqlConnector`
- `WeCms.Modules.*` 不得依赖 `WeCms.Persistence` 的具体实现

---

## 3. 敏捷与 Spec 先行

以下改动必须先建立 `docs/specs/<change-id>/{spec.md,tasks.md,checklist.md}`：

- diff ≥ 200 行
- 新增公共 API / OpenAPI 契约
- 新增数据库表或 migration
- 新增权限码、菜单、状态机
- 修改认证、授权、Token、审计、文件上传、安全策略
- 修改前后端契约、OpenAPI、generated 类型

禁止跳过 spec 直接实现。

---

## 4. TDD

- 逻辑变更遵循 Red → Green → Refactor
- bugfix 先写可复现失败测试
- Repository / SQL 用集成测试
- Minimal API 用 Endpoint 集成测试
- OpenAPI / generated 类型用契约测试

---

## 5. Definition of Done

任何 PR 在打开评审前，作者必须确认：

- [ ] 已遵循 Red → Green → Refactor，或明确 N/A
- [ ] 任务按单任务串行闭环推进
- [ ] 如使用 `sub agent`，其仅用于辅助分析/证据收集，最终改动、验证和审计由主 agent 负责
- [ ] 本地已通过 `bash scripts/quality-gate-backend.sh` 或等效命令
- [ ] 新增有副作用服务类已暴露为 `I*` 接口
- [ ] 依赖通过构造函数注入
- [ ] 改动文件均 ≤ 600 行，或有明确拆分理由
- [ ] 命名空间匹配目录
- [ ] 跨工程引用未违反依赖矩阵
- [ ] 未引入 EF Core / MVC Controller / Razor
- [ ] 未引入 `dynamic` / `SELECT *` / SQL 拼接用户输入
- [ ] DTO 已加入 `JsonSerializerContext`
- [ ] Endpoint 已显式注册
- [ ] 除 AllowAnonymous 外，业务 Endpoint 已绑定权限码
- [ ] 写操作已记录审计
- [ ] 当前任务对应审计已通过
- [ ] 全部任务完成后已完成本次改动范围的最终总审计

等效命令：

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

前端阶段或修改 `frontend/**` 时，额外执行：

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

---

## 6. 不得降低门禁

AI 不得为通过门禁而：

- 降低覆盖率阈值
- 跳过测试
- 删除测试
- 注释失败测试
- 关闭 publish 验证
- 移除安全扫描
- 放宽权限检查
- 删除审计要求
- 改低 lint / format 标准

规则文档例外：

- 仅修改 `AGENTS.md`、`code_review.md`、`.trae/rules/*`、`docs/context/*` 中的规则/流程文档时，可不跑 build/test/publish/typecheck/lint 门禁
- 即使适用该例外，也必须完成规则一致性检查与文档审计
- 只要混入生产代码、测试代码、脚本或生成产物改动，就不得使用该例外

---

## 7. 最终硬规则

以下规则不可被单个 PR 推翻：

1. JIT runtime baseline
2. Minimal APIs Only
3. SqlSugar ORM Only
4. 后端契约优先
5. SoybeanAdmin 只作为 UI 模板
6. AI 二期独立项目
7. AI 严禁直连 CMS 数据库
8. 所有业务 Endpoint 必须权限化
9. 所有写操作必须审计
