# AGENTS.md — WeCMS Next Codex Agent Instructions

> 本文件是 WeCMS Next 项目的 Codex / AI Coding Agent 项目级指令。  
> 所有 Codex App / Codex CLI / 代码生成 / 自动修复 / PR 任务必须优先遵守本文件。  
> 如与用户明确的新架构决策冲突，必须先停止并提示人工确认。

---

## 0. Agent 总原则

1. 先理解，再编码。开始新阶段前必须先阅读 `docs/context/*`、`AGENTS.md`、`code_review.md`、`.trae/rules/wecms-engineering-principles.md`。
2. 小步提交。一次只解决一个明确目标。
3. 先给计划，再改文件。编码任务开始前必须说明执行计划、影响文件、验证命令和风险。
4. 改动必须可验证。每次代码变更后必须说明如何运行 `build`、`test`、`publish`、前端 `typecheck/build`。
5. 不得绕过约束。不能为了“快速实现”引入违反 Minimal API、SqlSugar、后端契约优先、安全规则的方案。
6. 遇到不确定必须暂停说明。尤其是认证、权限、数据库迁移、文件安全、生产数据处理。
7. 不处理敏感数据。不读取、不生成、不提交真实 secret、token、密码、2FA secret、生产数据 dump。
8. AI 只作为开发辅助。一期不得实现运行时 AI 功能；AI 接入是二期独立项目。
9. 允许使用 `sub agent` 辅助拆分、检索、只读分析或并行收集证据，但主 agent 必须负责最终决策、实际改动、验证结论与审计结论。
10. 多任务必须串行推进。任何时刻只允许执行一个明确任务项；当前任务未完成测试、门禁和审计前，不得启动下一项任务。

---

## 1. 当前项目基线

WeCMS Next 是从 ThinkPHP CMS 迁移重构的新系统。

当前目标技术栈：

- ASP.NET Core Minimal APIs
- .NET 10
- JIT publish/runtime
- SqlSugar ORM
- MySQL
- SoybeanAdmin
- 后端契约优先
- 模块化单体

说明：

- 运行时基线已从 Native AOT 切换为 JIT。
- Minimal API 与 `WebApplication.CreateSlimBuilder(args)` 继续保留。
- 这次运行时基线调整 **不等于** 改成 MVC Controller。
- ADR-0017 `docs/adr/0017-minimal-api-remains-controller-forbidden.md` 已明确：继续 Minimal API，禁止 Controller / ControllerBase / AddControllers / MapControllers。

---

## 2. 必须优先阅读的文档

```text
docs/context/01-thinkphp-system.md
docs/context/02-next-migration-plan.md
docs/context/03-engineering-delivery.md
docs/context/04-m0-skeleton-validation.md
AGENTS.md
code_review.md
.trae/rules/wecms-engineering-principles.md
```

说明：

- `docs/context/01-04` 是稳定入口文件名。
- 真实正文维护在 `docs/context/` 下对应中文文档。
- 同一会话中可复用已读取上下文；只有文档更新、删除、缺失、会话重启或用户明确要求时才必须重读。

---

## 3. 最高优先级硬约束

### 3.1 工程原则

任何新增功能、修复 bug、重构或文档以外的代码变更，无论作者是人还是 AI，必须同时满足：

- OOP / SOLID
- 高内聚低耦合
- Spec 先行
- TDD
- Definition of Done

### 3.2 OOP / SOLID

1. 新增有副作用的服务类必须先定义 `I*` 接口，再写实现。
2. 依赖必须通过构造函数注入。
3. 业务代码内部不得 `new` 出数据库、文件、网络、缓存、邮件、存储、时钟、随机数等有副作用依赖。
4. 单类不得同时承担采集、处理、输出、鉴权、SQL、审计等复合职责。
5. 跨阶段数据模型优先使用 `record`、只读属性或不可变集合。
6. 业务模块依赖抽象，不依赖具体基础设施实现。
7. 以可测试性为设计要求，不允许以“难测”为由免测。

### 3.3 依赖矩阵

系统基础破坏性升级后的目标依赖矩阵：

```text
WeCms.Api
  -> WeCms.Modules.Identity / WeCms.Modules.AccessControl
  -> WeCms.Modules.Organization / WeCms.Modules.Configuration
  -> WeCms.Modules.Audit / WeCms.Modules.Security
  -> WeCms.Modules.FileCenter / WeCms.Modules.Platform
  -> WeCms.Modules.*.SqlSugar
  -> WeCms.Infrastructure
  -> WeCms.Data.SqlSugar
  -> WeCms.Caching
  -> WeCms.EventBus
  -> WeCms.Aop
  -> WeCms.Shared

WeCms.Modules.*
  -> WeCms.Shared
  -> 必要时依赖其他模块 Contracts 抽象

WeCms.Modules.*.SqlSugar
  -> 对应 WeCms.Modules.*
  -> WeCms.Data.SqlSugar
  -> WeCms.Shared

WeCms.Data.SqlSugar / WeCms.Caching / WeCms.EventBus
  -> WeCms.Shared

WeCms.Aop
  -> WeCms.Shared
  -> WeCms.Caching
  -> WeCms.EventBus

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> 不得引用其它生产工程
```

迁移期说明：

- `WeCms.Modules.System 最终删除`，迁移期间只作为 allow-list 暂存。
- `WeCms.Persistence 最终删除`，迁移期间只作为 allow-list 暂存。
- `WeCms.Modules.Cms` / CMS 模块暂不实现，不参与系统基础升级 API、OpenAPI 或质量门禁功能覆盖。

### 3.4 拒绝隐式兼容

1. 只在系统边界做校验。
2. 契约不满足时必须 fail-fast。
3. 禁止静默兜底、吞异常、legacy 分支、dead fallback。
4. 删除 API/字段/权限码/菜单 key 时必须同步删除代码、测试、文档和前端引用。

### 3.5 Spec 先行

满足任一条件必须先建立 `docs/specs/<change-id>/{spec.md,tasks.md,checklist.md}`：

- 预计 diff ≥ 200 行
- 新增公共 API / OpenAPI 契约
- 新增数据库表或 migration
- 新增权限码、菜单、状态机
- 修改认证、授权、Token、审计、文件上传、安全策略
- 修改前后端契约、OpenAPI、generated 类型

### 3.6 TDD

1. Red → Green → Refactor 三步不可省。
2. Bugfix 必须先有可复现失败测试。
3. 逻辑变更必须有对应测试。
4. Repository / SQL 用集成测试。
5. Minimal API 用 Endpoint 集成测试。
6. OpenAPI / generated 类型用契约测试。

### 3.7 Definition of Done

评审前至少满足：

```text
[ ] 已遵循 Red → Green → Refactor，或明确 N/A
[ ] 已运行 `scripts/quality-gate-backend.sh` 或等效命令
[ ] 新增有副作用服务类已暴露为 I* 接口并通过构造函数注入
[ ] 改动文件均 ≤ 600 行
[ ] 命名空间匹配目录
[ ] 跨工程引用未越过依赖矩阵
[ ] 未引入隐式兼容兜底、静默 catch、legacy 分支、dead fallback
[ ] ≥ 200 行或公共契约变更已有 docs/specs/<change-id>/ 三件套
[ ] 文档已同步更新，如涉及
[ ] PR 描述含 Closes # 或 Spec: 链接
[ ] 前端 typecheck / build 已实际运行并通过，如涉及前端
[ ] 当前开发任务对应测试和质量门禁已实际运行并通过
[ ] 当前开发任务对应审计已实际运行并通过
[ ] 全部任务完成后已对本次改动范围执行最终总审计
[ ] 未实现一期禁止的 AI runtime 能力
```

---

## 4. 后端技术硬约束

1. 只允许使用 ASP.NET Core Minimal APIs。
2. 只允许使用 .NET 10。
3. 当前运行时基线为 JIT publish/runtime。
4. 必须使用 `WebApplication.CreateSlimBuilder(args)`。
5. 禁止使用 MVC Controller。
6. 禁止使用 Razor / Razor Pages。
7. 禁止运行时 Endpoint 自动扫描。
8. 允许 Autofac / DynamicProxy，但 AOP 只能拦截 Application Service 接口。
9. 禁止业务运行时 code generation；DynamicProxy 仅限 ADR 批准的 Application Service AOP 场景。
10. 禁止在核心业务路径使用 Newtonsoft.Json。
11. 所有请求/响应 DTO 必须纳入 `System.Text.Json` Source Generator。
12. 所有 Endpoint 必须显式注册。
13. 除 `AllowAnonymous` 接口外，所有业务 Endpoint 必须绑定权限码或内部访问策略。
14. 新增 NuGet 包必须说明运行时兼容性、License、维护状态和替代方案。

---

## 5. 数据访问硬约束

1. 使用 SqlSugar ORM。
2. 禁止 EF Core。
3. 禁止 `dynamic 查询/返回`。
4. 禁止 `SELECT *`。
5. 禁止拼接用户输入到 SQL。
6. SQL 必须显式列出字段。
7. Repository 只负责 SQL 和数据映射。
8. Service / UseCase 层负责业务规则和事务边界。
9. 所有 Repository 方法必须支持 `CancellationToken`。
10. 排序字段必须白名单映射。
11. 分页参数必须校验，最大 `pageSize` 不超过 100。
12. 写操作必须检查 affected rows。
13. 批量操作必须限制最大数量。
14. Repository interface 只能定义在模块层或 `WeCms.Shared`，Repository implementation 迁移期可存在于 `WeCms.Persistence`，最终只能存在于 `WeCms.Modules.*.SqlSugar`。
15. Service / UseCase 获取 Repository、UnitOfWork、时钟、密码、Token、随机数等有副作用依赖时，必须通过接口 + DI，不得依赖具体实现。

### DB-BOUNDARY

1. 迁移期 `WeCms.Persistence` 可继续暂存数据库/ORM/连接器代码；最终只允许 `WeCms.Data.SqlSugar` 和 `WeCms.Modules.*.SqlSugar` 直接引用数据库/ORM/连接器。
2. `WeCms.Modules.*` 不得包含 SQL 文本。
3. `WeCms.Modules.*` 不得直接引用 `SqlSugar ORM`、`MySqlConnector`。
4. `WeCms.Modules.*` 不得依赖 `WeCms.Persistence`、`WeCms.Data.SqlSugar` 或 `WeCms.Modules.*.SqlSugar` 的具体实现。
5. `WeCms.Modules.*` 仅通过抽象控制事务边界，不得直接使用 `DbConnection` / `DbTransaction`。
6. `WeCms.Api`、`WeCms.Infrastructure`、`WeCms.Shared` 也不得直接持有 SQL 文本、ORM Client、数据库连接或 Repository implementation。
7. CodeFirst 建模仅允许在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 边界内使用；最终数据库结构以 migration baseline 为准。
8. 当前无生产环境，允许重置数据库 baseline；未来生产环境不得自动 DDL。

---

## 6. 前端硬约束

1. 使用 SoybeanAdmin。
2. 前端一切数据格式以后端 DTO / OpenAPI 为准。
3. `service/generated` 禁止手写修改。
4. `request interceptor` 只处理 token、`code`、`msg`、401、403，不得重塑业务 `data`。
5. 动态菜单来自后端菜单 DTO。
6. 按钮权限消费后端返回的 `permissions`。
7. 前端隐藏按钮不代表后端放行。

---

## 7. AI 边界硬约束

当前一期禁止：

- `WeCms.Modules.Ai`
- AI Provider
- Prompt / RAG / Vector Store / Agent Tool 运行时代码
- 后端调用任何模型 API
- SoybeanAdmin AI 业务页面
- AI Key 写入项目

二期 AI 必须作为独立项目存在，且只能通过 CMS Core API 获取或写回 CMS 数据。

---

## 8. 响应与契约规则

统一响应结构：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

错误响应必须包含 `traceId`。  
分页结构固定为：

```json
{
  "records": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

OpenAPI 是前后端契约交付物。

---

## 9. 开发流程

### 9.1 正式编码前

必须先完成只读任务：

```text
M0-00：Codex 项目文档熟悉与开发拆分报告
```

### 9.2 编码任务流程

1. 读取规则与上下文
2. 拆分任务列表，定义当前只执行的单一任务项
3. 输出执行计划
4. 列出修改文件、验证命令、审计命令和风险
5. 需要时可使用 `sub agent` 做辅助分析，但不得把多个实现任务并行推进
6. 修改代码或文档
7. 运行当前任务对应测试
8. 运行当前任务对应质量门禁
9. 对当前任务改动执行代码审计或规则审计
10. 只有测试、门禁、审计全部通过，才可关闭当前任务并进入下一项
11. 所有任务完成后，对项目本次改动范围执行一次最终总审计
12. 输出验证结果、审计结果、风险和后续事项

### 9.2.1 Sub Agent 使用规则

1. `sub agent` 只能作为辅助执行单元，不得替代主 agent 的最终判断。
2. 可以把检索、只读对比、日志归纳、独立证据收集交给 `sub agent`。
3. 涉及实际改代码、合并方案、是否通过验证、是否允许进入下一项任务，必须由主 agent 统一收口。
4. 即使存在多个 `sub agent`，主线任务状态仍然只能有一个 `in progress` 任务项。
5. 若 `sub agent` 结论与仓库现状、测试结果或规则冲突，以仓库实证和主 agent 复核结果为准。

### 9.3 每次后端变更必须说明

```text
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

### 9.4 每次前端变更必须说明

```text
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

### 9.5 阻断规则

1. 测试未运行或未通过：不得进入下一项任务。
2. 质量门禁未运行或未通过：不得进入下一项任务。
3. 审计未运行或未通过：不得进入下一项任务。
4. 门禁失败时，只允许修复当前任务相关问题，不得顺带推进后续任务。
5. 审计发现问题时，只允许修复当前任务相关问题，不得顺带推进后续任务。
6. 规则制定或规则文档修改属于文档治理例外：可不跑 build/test/publish/typecheck/lint 等门禁，但必须完成文档一致性检查和规则审计。
7. 第 6 条仅适用于规则文档和流程文档本身的修改；一旦包含生产代码、测试代码、脚本或生成产物改动，立即恢复正常测试与门禁要求。

---

## 10. 推荐阶段计划

### M0：工程骨架验证

目标：跑通 .NET 10 JIT publish + SqlSugar ORM + OpenAPI + MySQL 联通。

交付：

```text
工程目录
.NET solution
Minimal API
ApiResult
ExceptionMiddleware
JsonSerializerContext
Health endpoint
SqlSugar ORM db-check
OpenAPI 生成
CI publish 验证
ThinkPHP migration spike
```

说明：M1-BE 当前为 backend-only 系统管理 API 阶段，SoybeanAdmin request 封装属于后续前端阶段，不是本阶段交付项。

### M1-BE：系统管理 API

目标：在 M0-BE 后端底座之上完成系统管理核心 API。

交付：

```text
用户管理
角色管理
菜单管理
权限管理
部门管理
岗位管理
字典管理
系统设置
登录日志
操作审计日志
安全事件
文件基础能力
系统管理权限码 seed
系统管理菜单 seed
OpenAPI 契约增强
M1-BE quality gate
```

边界：

```text
不做 frontend/**
不运行 pnpm
不生成前端 TypeScript generated
不做 CMS 内容 API
CMS 模块暂不实现
不做旧系统数据迁移
不做旧系统兼容模式
不做 AI runtime
系统基础破坏性升级期间按 ADR-0019 迁移数据库边界，WeCms.Persistence 最终删除
所有业务 Endpoint 必须有权限码或显式内部访问策略
所有写操作必须记录审计
```

### M1：认证安全闭环

```text
登录
刷新 token
退出
/auth/me
Refresh Token hash 存储
登录日志
安全事件
```

### M2：用户、角色、菜单、权限

```text
用户管理
角色管理
菜单管理
权限管理
角色分配权限
用户分配角色
按钮权限
动态路由
```

---

## 11. PR 与验收要求

每个 PR 必须说明：

```text
变更内容
关联任务
是否符合 JIT 运行时基线
是否符合 SqlSugar ORM
是否符合后端契约优先
是否新增权限码
是否新增审计
是否新增 SQL
是否新增前端 generated 类型
是否误实现 AI runtime
验证命令和结果
风险说明
```

阻断合并的问题：

```text
build/test/publish 失败
引入 MVC Controller
引入 EF Core
使用 dynamic
出现 SELECT *
业务 Endpoint 无权限码
DTO 未进入 JsonSerializerContext
前端改写后端契约
日志泄露敏感信息
一期实现 AI runtime
AI 相关代码直连 CMS 数据库
```

---

## 12. 最终提醒

Codex 可以帮助实现 WeCMS，但不能替代：

```text
架构决策
安全 Review
权限 Review
数据库迁移审核
真实发布验证
CI/CD 门禁
人工最终批准
```
