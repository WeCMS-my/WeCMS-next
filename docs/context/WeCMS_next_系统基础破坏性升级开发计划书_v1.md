# WeCMS-next 系统基础破坏性升级开发计划书 v1

> 依据文档：`WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`  
> 适用仓库：`WeCMS-my/WeCMS-next`  
> 适用阶段：无生产环境使用、允许大范围破坏性升级、暂不实现 CMS 内容模块，只升级系统基础功能模块。  
> 核心决策：**继续使用 ASP.NET Core Minimal API；明确不引入 Controller Web API / MVC Controller / ControllerBase。**  
> 本文定位：供 Codex / C# 开发者直接按任务执行的开发计划书。  
> 本文不修改仓库代码，仅定义开发任务、拆分粒度、测试策略、验收标准和执行顺序。

---

## 0. 总体结论

本次升级不是从 0 重写，而是一次 **不兼容旧结构的系统基础平台重构**。核心策略是：

```text
保持 Minimal API
拆分 System 大模块
拆分 Persistence 大模块
重置系统基础数据库 baseline
建立 SqlSugar 数据平台
建立统一权限平台
建立统一缓存抽象
引入 AOP 事务与缓存
统一审计日志 / 异常日志 / SQL 日志
引入 Swagger/Scalar、MiniProfiler、限流、国际化基础设施
引入 EventBus + Outbox，为后续 CMS 内容发布预留
```

### 0.1 最高优先级架构决策

| 编号 | 决策 | 说明 |
|---|---|---|
| D-001 | 不引入 Controller Web API | 禁止 `Controller`、`ControllerBase`、`AddControllers()`、`MapControllers()`。 |
| D-002 | 继续 Minimal API | 使用 `MapGroup`、`MapGet`、`MapPost`、Endpoint Filter、Endpoint Metadata。 |
| D-003 | 允许破坏性升级 | 不考虑生产兼容，不保留旧 System / Persistence 长期结构。 |
| D-004 | 暂不实现 CMS 内容模块 | 不做站点、栏目、内容、审核流、发布流、搜索索引。 |
| D-005 | 拆分 System | 拆成 Identity、AccessControl、Organization、Configuration、Audit、Security、FileCenter、Platform。 |
| D-006 | 拆分 Persistence | 拆成 `WeCms.Data.SqlSugar` + 各模块 `.SqlSugar` 适配层。 |
| D-007 | CodeFirst + Migration 双轨 | CodeFirst 做建模与验证，Migration 做固化与可审查变更。 |
| D-008 | QueryFilter 做运行时治理 | 软删除、租户、站点、数据权限统一由 QueryFilter / Filter Builder 治理。 |
| D-009 | DI / IOC / AOP 可用 | 允许 Autofac / DynamicProxy，但只作用于 Application Service 接口。 |
| D-010 | 严格 TDD | 所有逻辑变更先写测试，遵循 Red → Green → Refactor。 |

---

## 1. 开发原则与 C# 规范

### 1.1 C# 基础规范

1. 所有新项目启用：
   - `TargetFramework=net10.0`
   - `Nullable=enable`
   - `ImplicitUsings=enable`
   - `TreatWarningsAsErrors=true`
2. 所有异步 API 使用 `async/await`，禁止 `.Result`、`.Wait()`、`Task.WaitAll()`。
3. 所有 Repository、Service、Handler、Endpoint Handler 必须接受并传递 `CancellationToken`。
4. DTO、Query、Command、Result 优先使用 `record` 或不可变对象。
5. 有副作用服务必须定义接口，命名为 `I*`。
6. 构造函数注入必须优先，禁止业务代码使用服务定位器获取依赖。
7. 禁止在业务代码中直接 `new`：数据库连接、SqlSugar Client、HttpClient、文件存储、缓存、时钟、随机数、Token 服务、Repository 实现。
8. 每个文件建议不超过 600 行；超过必须拆分。
9. 命名空间必须与目录结构一致。
10. 禁止吞异常；除非有明确降级策略和审计记录，否则 catch 后必须 rethrow。

### 1.2 Minimal API 规范

1. Endpoint 只能通过 Minimal API 显式注册。
2. Endpoint 文件只处理：路由、绑定、权限、限流、审计 metadata、调用 Application Service、返回 `ApiResult<T>`。
3. Endpoint Handler 禁止写业务规则、SQL、事务、审计细节。
4. 每个 Endpoint 必须包含：
   - 路由名 `WithName`
   - 分组 `WithTags`
   - OpenAPI 响应元数据
   - 权限或 `AllowAnonymous/InternalOnly`
   - 写操作审计 metadata
   - 高危写操作限流 metadata
5. 禁止 `AddControllers()` 和 `MapControllers()`。
6. 禁止新增 MVC Filter 作为业务 API 主入口。

### 1.3 分层职责规范

| 层 | 允许职责 | 禁止职责 |
|---|---|---|
| `WeCms.Api` | Host、Middleware、Endpoint convention、OpenAPI、Swagger/Scalar、MiniProfiler、RateLimiting、Composition Root | 业务规则、SQL、Repository 实现 |
| `WeCms.Modules.*` | Endpoint、Application Service、DTO、Permission、Repository 接口、领域规则 | SqlSugar、MySQL、SQL 文本、持久化实现 |
| `WeCms.Modules.*.SqlSugar` | 对应模块 Repository 实现、实体映射、SQL/Queryable | 业务编排、HTTP、权限决策 |
| `WeCms.Data.SqlSugar` | SqlSugar 客户端、多库、多租户、CodeFirst、Migration、QueryFilter、SQL 审计、UoW | 模块业务规则、HTTP |
| `WeCms.Caching` | 缓存抽象与实现 | 业务规则 |
| `WeCms.Aop` | AOP Attribute、Interceptor、Autofac 注册 | 业务规则、Repository 实现 |
| `WeCms.EventBus` | EventBus、Outbox、Dispatcher、Handler 抽象 | 业务规则 |
| `WeCms.Shared` | 通用契约、接口、基础模型、错误码、结果类型 | 具体基础设施实现 |

### 1.4 TDD 工作流

每个任务必须执行：

```text
Red：先新增或修改测试，确认失败原因符合预期。
Green：实现最小代码让测试通过。
Refactor：重构命名、职责、重复代码，并保证测试继续通过。
```

Codex 执行任务时必须在输出中说明：

```text
1. 新增/修改了哪些测试
2. 哪个测试先失败
3. 如何实现通过
4. 是否重构
5. 运行了哪些命令
6. 仍有哪些风险或未完成项
```

---

## 2. 目标项目结构

### 2.1 生产代码目标结构

```text
backend/src/
  WeCms.Api/
  WeCms.Shared/
  WeCms.Infrastructure/
  WeCms.Data.SqlSugar/
  WeCms.Caching/
  WeCms.EventBus/
  WeCms.Aop/

  WeCms.Modules.Identity/
  WeCms.Modules.Identity.SqlSugar/

  WeCms.Modules.AccessControl/
  WeCms.Modules.AccessControl.SqlSugar/

  WeCms.Modules.Organization/
  WeCms.Modules.Organization.SqlSugar/

  WeCms.Modules.Configuration/
  WeCms.Modules.Configuration.SqlSugar/

  WeCms.Modules.Audit/
  WeCms.Modules.Audit.SqlSugar/

  WeCms.Modules.Security/
  WeCms.Modules.Security.SqlSugar/

  WeCms.Modules.FileCenter/
  WeCms.Modules.FileCenter.SqlSugar/

  WeCms.Modules.Platform/
```

### 2.2 测试目标结构

暂时保留现有三类测试项目，但按模块拆目录：

```text
backend/tests/
  WeCms.Tests.Unit/
    Identity/
    AccessControl/
    Organization/
    Configuration/
    Audit/
    Security/
    FileCenter/
    Platform/
    DataSqlSugar/
    Caching/
    Aop/
    EventBus/

  WeCms.Tests.Integration/
    Identity/
    AccessControl/
    Organization/
    Configuration/
    Audit/
    Security/
    FileCenter/
    Platform/
    DataSqlSugar/
    EventBus/

  WeCms.Tests.Architecture/
    NoControllerTests.cs
    LayerDependencyTests.cs
    ModuleBoundaryTests.cs
    PersistenceBoundaryTests.cs
    SqlSugarBoundaryTests.cs
    DiBoundaryTests.cs
    EndpointCoverageTests.cs
    PermissionCoverageTests.cs
    AuditCoverageTests.cs
```

### 2.3 暂不启用 CMS 模块

本次计划不实现 CMS 内容域。执行策略：

1. `WeCms.Modules.Cms` 不参与 `WeCms.Api` 引用。
2. `WeCms.Modules.Cms` 不参与 `WeCms.Data.SqlSugar` 引用。
3. `WeCms.Modules.Cms` 不参与 OpenAPI。
4. `WeCms.Modules.Cms` 不参与系统基础质量门禁功能覆盖。
5. 如果保留目录，必须有 README 说明：系统基础能力不得放入 CMS 模块。

---

## 3. 敏捷发布计划总览

### 3.1 Sprint 总览

| Sprint | 名称 | 目标 | 是否可并行 | 核心验收 |
|---|---|---|---|---|
| S0 | 规则破坏性升级 | 修改规则、ADR、测试和门禁，确认不引入 Controller | 否 | 架构规则更新通过 |
| S1 | 新项目骨架 | 创建新模块和平台项目，不迁移业务 | 否 | build + layer tests |
| S2 | Minimal API Endpoint 平台 | 建立 EndpointDefinition、统一 metadata、validation/audit 扩展 | 否 | 无 Controller，Endpoint 测试通过 |
| S3 | Data.SqlSugar 平台骨架 | 建立 SqlSugar 多连接、UoW、CodeFirst 基础骨架 | 否 | DB boundary + connection tests |
| S4 | Identity 迁移 | 迁移 Auth、Users、TwoFactor | 否 | 登录、Me、用户接口通过 |
| S5 | AccessControl 迁移 | 迁移 Roles、Permissions、Menus，增强权限模型 | 否 | 权限检查、角色菜单权限通过 |
| S6 | Organization 迁移 | 迁移 Departments，Posts 改 Positions | 否 | 部门/岗位接口通过，无 Post 命名残留 |
| S7 | Configuration 迁移 | 迁移 Settings、Dicts、I18n，加入缓存失效点 | 否 | 设置/字典/i18n 通过 |
| S8 | Audit / Security / FileCenter / Platform 迁移 | 拆完剩余系统模块 | 否 | 审计、安全、文件、健康检查通过 |
| S9 | 删除旧 System / Persistence | 删除旧项目和 namespace，重置引用 | 否 | rg 检查为空，架构测试通过 |
| S10 | SqlSugar 数据平台完整升级 | CodeFirst、Migration、QueryFilter、多库、多租户、SQL 审计 | 否 | schema/queryfilter/sql audit tests |
| S11 | 缓存 + AOP | 统一缓存、事务拦截、缓存拦截 | 否 | 事务 rollback、缓存命中/失效通过 |
| S12 | EventBus + Outbox | 系统基础事件异步处理 | 否 | Outbox 写入、Dispatcher、幂等通过 |
| S13 | Swagger / Scalar / MiniProfiler | 不引入 Controller 的 API 文档与诊断 | 否 | Swagger/Scalar、OpenAPI、MiniProfiler 通过 |
| S14 | 最终清理与验收 | 全量质量门禁、文档、架构报告 | 否 | quality gate 全绿 |

### 3.2 迭代原则

1. 每个 Sprint 必须小步提交。
2. 每个 Sprint 必须先写/改架构测试或单元测试。
3. 不允许同时迁移多个业务模块，防止大范围失败难以定位。
4. 不允许为通过测试降低规则；必须修复实现。
5. 每个 Sprint 结束必须更新 `README`、`AGENTS.md`、`code_review.md` 或 ADR 中受影响部分。
6. 每个 Sprint 必须生成一份变更报告，说明完成项、未完成项、风险和下一步。

---

## 4. Sprint 0：规则破坏性升级

### 4.1 目标

解除旧 AOT 限制中的不合理部分，同时明确继续 Minimal API、不引入 Controller Web API。为后续模块拆分、AOP、CodeFirst、Swagger/Scalar、MiniProfiler、EventBus 做规则准备。

### 4.2 影响文件

```text
AGENTS.md
code_review.md
docs/adr/001X-minimal-api-remains-controller-forbidden.md
docs/adr/001X-system-foundation-module-split.md
docs/adr/001X-sqlsugar-data-platform.md
docs/adr/001X-aop-cache-transaction.md
backend/tests/WeCms.Tests.Architecture/*
scripts/checks/*
scripts/quality-gate-backend.sh
```

### 4.3 开发任务

#### S0-T01：新增 Minimal API 决策 ADR

**目标**：明确继续 Minimal API，不引入 Controller。

子任务：

1. 新增 ADR 文件：`docs/adr/001X-minimal-api-remains-controller-forbidden.md`。
2. 写明保留 Minimal API 的原因：显式 Endpoint、权限 metadata、Endpoint Filter、OpenAPI 覆盖、质量门禁适配。
3. 写明禁止项：`Controller`、`ControllerBase`、`AddControllers()`、`MapControllers()`、MVC Controller Attribute Routing。
4. 写明允许项：Swagger/Scalar、Endpoint Filter、Endpoint Metadata、EndpointDefinition、Endpoint Convention。
5. 写明验收：架构测试扫描无 Controller 相关调用。

测试要求：

1. 新增 `NoControllerArchitectureTests`。
2. 测试扫描生产代码禁止出现：
   - `: ControllerBase`
   - `: Controller`
   - `AddControllers(`
   - `MapControllers(`
   - `[ApiController]`

验收标准：

```text
ADR 存在且状态为 Accepted
AGENTS/code_review 引用该 ADR
NoControllerArchitectureTests 先失败后通过
```

Codex 执行提示：

```text
先添加架构测试，使当前规则中 Controller 禁令可自动验证；再新增 ADR 和文档规则；最后运行 architecture tests。
```

---

#### S0-T02：新增系统基础模块拆分 ADR

**目标**：确定 `WeCms.Modules.System` 必须拆分。

子任务：

1. 新增 `docs/adr/001X-system-foundation-module-split.md`。
2. 明确拆分为：Identity、AccessControl、Organization、Configuration、Audit、Security、FileCenter、Platform。
3. 写明 `Posts` 改名 `Positions` 的原因。
4. 写明旧 `WeCms.Modules.System` 最终删除。
5. 写明 `WeCms.Modules.Cms` 暂不启用。
6. 写明模块间依赖规则。

测试要求：

1. 新增 `NoSystemGodModuleArchitectureTests`。
2. 初始阶段允许旧项目存在，但测试应支持迁移期间用 allow-list 过渡。
3. 最终阶段测试必须要求 `WeCms.Modules.System` 不存在。

验收标准：

```text
ADR 存在
拆分映射表清晰
最终验收规则定义清楚
```

---

#### S0-T03：新增 SqlSugar 数据平台 ADR

**目标**：确定 CodeFirst + Migration + QueryFilter + 多库多租户 + SQL 审计路线。

子任务：

1. 新增 `docs/adr/001X-sqlsugar-data-platform.md`。
2. 明确 `CodeFirst` 负责建模和验证。
3. 明确 `Migration` 负责固化和审查。
4. 明确当前无生产环境，允许重置 baseline。
5. 明确 `QueryFilter` 负责软删除、租户、数据权限。
6. 明确 SQL 日志必须脱敏。
7. 明确 SqlSugar 只允许在 `WeCms.Data.SqlSugar` 和 `*.SqlSugar` 项目中使用。

测试要求：

1. 修改或新增 `SqlSugarBoundaryTests`。
2. 禁止模块层使用 `ISqlSugarClient`、`SqlSugarScope`、`Ado`、SQL 文本。
3. 允许 `WeCms.Data.SqlSugar` 和 `WeCms.Modules.*.SqlSugar` 使用 SqlSugar。

验收标准：

```text
ADR 存在
DB boundary 新规则明确
旧 WeCms.Persistence 不作为长期合法项目
```

---

#### S0-T04：修改 AGENTS.md

**目标**：让 Codex 后续执行时遵循新规则。

子任务：

1. 保留 Minimal API only。
2. 删除或改写“不允许动态代理 AOP”的旧限制。
3. 明确允许 Autofac / DynamicProxy，但只允许拦截 Application Service 接口。
4. 明确允许 CodeFirst 建模，但禁止业务层直接访问 SqlSugar。
5. 明确允许 Swagger/Scalar/MiniProfiler。
6. 明确禁止 Controller。
7. 增加模块拆分后的依赖矩阵。
8. 增加“无生产环境，允许重置数据库 baseline”的说明。
9. 增加“CMS 模块暂不实现”的说明。

测试要求：

1. 新增/修改规则文本扫描测试。
2. 检查 AGENTS.md 包含关键短语：
   - `Minimal API`
   - `禁止 Controller`
   - `允许 Autofac`
   - `CodeFirst 建模`
   - `WeCms.Modules.System 最终删除`

验收标准：

```text
AGENTS 与本开发计划一致
没有旧 AOT-only 阻断规则残留
```

---

#### S0-T05：修改 code_review.md

**目标**：让代码审查基线支持新架构。

子任务：

1. 保留 P0：禁止 Controller、禁止 EF Core、禁止 SELECT *、禁止 SQL 拼接。
2. 删除 P0：禁止动态代理 AOP / runtime code generation 的绝对表达。
3. 新增 P0：AOP 只能用于 Application Service 接口。
4. 新增 P0：业务模块不得引用 `WeCms.Data.SqlSugar` 或 `*.SqlSugar`。
5. 新增 P0：新增 Endpoint 无权限/审计 metadata 阻断。
6. 新增 P0：SqlAudit 未脱敏阻断。
7. 新增模块边界 review 项。
8. 新增 TDD review 项：重构必须先有架构测试保护。

验收标准：

```text
code_review 支持新架构
Controller 仍为阻断项
AOP/CodeFirst 不再被旧规则误杀
```

---

#### S0-T06：调整 Architecture Tests 基础框架

**目标**：让架构测试支持新项目结构。

子任务：

1. 修改 `LayerDependencyTests` 允许新项目矩阵。
2. 修改 `PersistenceBoundaryTests` 支持 `WeCms.Data.SqlSugar` 与 `*.SqlSugar`。
3. 修改 `DiBoundaryTests` 允许 Autofac 模块注册代码，但禁止业务层服务定位。
4. 新增 `NoControllerArchitectureTests`。
5. 新增 `ModuleBoundaryTests`。
6. 新增 `NoSystemGodModuleArchitectureTests`。
7. 新增 `NoCmsModuleActiveReferenceTests`。

测试要求：

```bash
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:SkipFrontendBuild=true
```

验收标准：

```text
架构测试可以表达新目标结构
暂未创建新项目时允许 pending/transition 规则
```

---

#### S0-T07：调整质量门禁脚本

**目标**：使 gate 支持新规则和新项目。

子任务：

1. 更新 `check-layer-dependency.sh`。
2. 更新 `check-db-boundary.sh`。
3. 更新 `check-di-boundary.sh`。
4. 新增 `check-no-controller.sh`。
5. 新增 `check-no-system-god-module.sh`。
6. 新增 `check-sqlsugar-boundary.sh`。
7. 新增 `check-minimal-api-endpoint-metadata.sh`。
8. 更新 `quality-gate-backend.sh` 调用顺序。
9. 中间阶段允许使用 transition flag，但最终验收必须关闭。

验收标准：

```text
脚本可运行
失败信息清晰
不会误杀新架构合法项目
```

---

### 4.4 Sprint 0 完成标准

```text
[ ] 新 ADR 已建立
[ ] AGENTS.md 已更新
[ ] code_review.md 已更新
[ ] Controller 禁令仍明确
[ ] AOP / CodeFirst / Swagger / MiniProfiler 已被允许
[ ] 架构测试已支持新目标结构
[ ] 质量门禁脚本已支持新目标结构
[ ] 未修改业务逻辑
```

---

## 5. Sprint 1：新项目骨架

### 5.1 目标

创建所有新项目、基础引用、AssemblyMarker、空注册扩展，不迁移业务逻辑。

### 5.2 新增项目

```text
WeCms.Data.SqlSugar
WeCms.Caching
WeCms.EventBus
WeCms.Aop
WeCms.Modules.Identity
WeCms.Modules.Identity.SqlSugar
WeCms.Modules.AccessControl
WeCms.Modules.AccessControl.SqlSugar
WeCms.Modules.Organization
WeCms.Modules.Organization.SqlSugar
WeCms.Modules.Configuration
WeCms.Modules.Configuration.SqlSugar
WeCms.Modules.Audit
WeCms.Modules.Audit.SqlSugar
WeCms.Modules.Security
WeCms.Modules.Security.SqlSugar
WeCms.Modules.FileCenter
WeCms.Modules.FileCenter.SqlSugar
WeCms.Modules.Platform
```

### 5.3 开发任务

#### S1-T01：创建平台项目

子任务：

1. 创建 `WeCms.Data.SqlSugar`。
2. 创建 `WeCms.Caching`。
3. 创建 `WeCms.EventBus`。
4. 创建 `WeCms.Aop`。
5. 每个项目设置 `net10.0`、Nullable、ImplicitUsings、TreatWarningsAsErrors。
6. 每个项目添加 `AssemblyMarker.cs`。
7. 更新 solution / slnx。

项目引用：

| 项目 | 引用 |
|---|---|
| `WeCms.Data.SqlSugar` | `WeCms.Shared` |
| `WeCms.Caching` | `WeCms.Shared` |
| `WeCms.EventBus` | `WeCms.Shared` |
| `WeCms.Aop` | `WeCms.Shared`、后续 Autofac 包 |

测试：

```bash
dotnet restore backend/WeCms.slnx
dotnet build backend/WeCms.slnx -warnaserror -p:SkipFrontendBuild=true
```

---

#### S1-T02：创建业务模块项目

子任务：

1. 创建 8 个业务模块项目。
2. 每个模块项目只引用 `WeCms.Shared`。
3. 每个模块添加目录：
   - `Endpoints`
   - `Services`
   - `Contracts`
   - `Permissions`
   - `Repositories`
   - `Records`
4. 每个模块添加 `System*ServiceCollectionExtensions` 的新命名，例如：
   - `IdentityServiceCollectionExtensions`
   - `AccessControlServiceCollectionExtensions`
5. 每个模块添加空 Endpoint 注册扩展。

验收：

```text
模块项目不引用 SqlSugar
模块项目不引用 *.SqlSugar
模块项目不引用 WeCms.Api
模块项目不引用 WeCms.Infrastructure
```

---

#### S1-T03：创建模块 SqlSugar 适配项目

子任务：

1. 创建 7 个 `.SqlSugar` 项目。
2. 每个 `.SqlSugar` 项目引用：
   - 对应业务模块
   - `WeCms.Data.SqlSugar`
   - `WeCms.Shared`
3. 每个 `.SqlSugar` 项目添加目录：
   - `Entities`
   - `Repositories`
   - `CodeFirst`
4. 每个 `.SqlSugar` 项目添加 DI 注册扩展，例如 `AddWeCmsIdentitySqlSugar()`。

注意：`Platform` 默认不需要 `.SqlSugar` 项目，除非后续有平台状态查询 Repository。

---

#### S1-T04：更新依赖矩阵测试

子任务：

1. 更新 `LayerDependencyTests` 中 allowed references。
2. 新增非法引用用例。
3. 确认 `WeCms.Modules.*` 不能引用 `.SqlSugar`。
4. 确认 `.SqlSugar` 不能引用其他模块 `.SqlSugar`。
5. 确认 `WeCms.Shared` 无生产项目引用。

验收：

```bash
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:SkipFrontendBuild=true
```

---

### 5.4 Sprint 1 完成标准

```text
[ ] 所有新项目创建完成
[ ] solution 更新完成
[ ] build 通过
[ ] LayerDependencyTests 通过
[ ] 旧业务未迁移
[ ] 旧 System/Persistence 仍可编译
```

---

## 6. Sprint 2：Minimal API Endpoint 平台

### 6.1 目标

在不引入 Controller 的前提下，建立模块化 Endpoint Definition、统一 Endpoint metadata、验证、审计、OpenAPI 扩展。

### 6.2 开发任务

#### S2-T01：新增 EndpointDefinition 抽象

影响文件：

```text
WeCms.Shared/Endpoints/IEndpointDefinition.cs
WeCms.Api/Endpoints/EndpointDefinitionRegistry.cs
WeCms.Api/Endpoints/EndpointMappingExtensions.cs
```

子任务：

1. 新增 `IEndpointDefinition`。
2. 新增 `MapEndpointDefinitions` 扩展。
3. 暂不使用运行时自动扫描，先显式传入 definition 实例或模块注册方法。
4. 编写测试确保 EndpointDefinition 可以注册 route。
5. 禁止 `MapControllers`。

测试：

```text
EndpointDefinitionMappingTests
NoControllerArchitectureTests
```

---

#### S2-T02：新增 Endpoint Metadata 模型

新增文件：

```text
WeCms.Shared/Endpoints/EndpointModuleMetadata.cs
WeCms.Shared/Endpoints/EndpointAuditMetadata.cs
WeCms.Shared/Endpoints/EndpointPermissionMetadata.cs
WeCms.Shared/Endpoints/EndpointRateLimitMetadata.cs
```

子任务：

1. 定义模块 metadata。
2. 定义审计 metadata：`module/resource/action`。
3. 定义权限 metadata：`permissionCode/kind`。
4. 定义限流 metadata。
5. 定义 OpenAPI 扩展字段名称常量。
6. 添加单元测试验证 metadata 可从 Endpoint 读取。

---

#### S2-T03：新增 Endpoint Convention 扩展

新增文件：

```text
WeCms.Api/Endpoints/EndpointConventionExtensions.cs
WeCms.Api/Endpoints/EndpointAuditExtensions.cs
WeCms.Api/Endpoints/EndpointPermissionExtensions.cs
WeCms.Api/Endpoints/EndpointValidationExtensions.cs
WeCms.Api/Endpoints/EndpointOpenApiExtensions.cs
```

子任务：

1. 实现 `.ProducesApi<T>()`。
2. 实现 `.Audit(module, resource, action)`。
3. 实现 `.RequirePermission(permissionCode)`。
4. 实现 `.RequireButtonPermission(permissionCode)`。
5. 实现 `.RequireUrlPermission(permissionCode)`。
6. 实现 `.Validate<TRequest>()`。
7. 实现 `.WithModule(moduleName)`。
8. 为每个扩展写单元测试。

验收：

```text
所有扩展可以组合使用
metadata 不覆盖已有 metadata
错误参数 fail-fast
```

---

#### S2-T04：新增 ValidationEndpointFilter

子任务：

1. 新增 `IRequestValidator<TRequest>`。
2. 新增 `ValidationResult` / `ValidationError`。
3. 新增 `ValidationEndpointFilter<TRequest>`。
4. Filter 从参数中解析 request。
5. 校验失败返回统一 `ApiResult`。
6. 支持多个 validator。
7. 支持无 validator 时通过。

测试：

```text
ValidationEndpointFilter_ReturnsValidationError_WhenInvalid
ValidationEndpointFilter_CallsNext_WhenValid
ValidationEndpointFilter_DoesNotThrow_WhenNoValidator
```

---

#### S2-T05：新增 AuditEndpointFilter 最小版

子任务：

1. 新增 `AuditEndpointFilter`。
2. Filter 读取 `EndpointAuditMetadata`。
3. 对写操作记录 started/completed/failed 状态。
4. 暂时调用 `IAuditWriter` 抽象，先提供 Noop 实现。
5. 不在 Filter 中写 SQL。

测试：

```text
AuditEndpointFilter_WritesSuccessAudit
AuditEndpointFilter_WritesFailureAudit
AuditEndpointFilter_Skips_WhenNoAuditMetadata
```

---

#### S2-T06：更新现有 Endpoint 的迁移示例

选择一个低风险接口，例如 `system/ping` 或 `secure-ping`，迁移为 EndpointDefinition 风格。

子任务：

1. 新建 `PlatformEndpointDefinition`。
2. 迁移 `ping` 或 `secure-ping`。
3. 保留路由兼容不是目标；本次可调整路由，但应记录。
4. 更新 OpenAPI 测试。
5. 更新 endpoint coverage 测试。

---

### 6.3 Sprint 2 完成标准

```text
[ ] EndpointDefinition 可用
[ ] Metadata 扩展可用
[ ] ValidationEndpointFilter 可用
[ ] AuditEndpointFilter 最小版可用
[ ] 至少一个接口使用新模式
[ ] 无 Controller 相关代码
```

---

## 7. Sprint 3：Data.SqlSugar 平台骨架

### 7.1 目标

建立新的 `WeCms.Data.SqlSugar` 平台项目，先实现基础连接、UoW、Migration、CodeFirst 骨架，不迁移所有 Repository。

### 7.2 开发任务

#### S3-T01：迁移 UnitOfWork 到 Data.SqlSugar

子任务：

1. 从旧 Persistence 迁移 `SqlSugarUnitOfWork`。
2. 从旧 Persistence 迁移 `SqlSugarTransactionContext`。
3. 保持 `IUnitOfWork` 在 `WeCms.Shared.Data`。
4. 支持 async commit/rollback/dispose。
5. 异常时 rollback 后 rethrow。
6. 禁止同步阻塞。

测试：

```text
SqlSugarTransactionContext_CommitsOnce
SqlSugarTransactionContext_RollsBackOnDispose_WhenNotCommitted
SqlSugarUnitOfWork_BeginsTransaction
```

---

#### S3-T02：新增多连接配置模型

新增：

```text
DatabaseConnectionOptions
DatabaseConnectionRole
DatabasePlatformOptions
DatabaseOptionsReader
```

子任务：

1. 支持 `main/log/audit/file/tenant` role。
2. 支持 connection name。
3. 支持 connection string name。
4. 支持 enabled。
5. 支持 command timeout。
6. 配置缺失 fail-fast。
7. 非法 db type fail-fast。

测试：

```text
DatabaseOptionsReader_ReadsSingleConnection
DatabaseOptionsReader_Fails_WhenDefaultMissing
DatabaseOptionsReader_Fails_WhenDuplicateConnectionName
DatabaseOptionsReader_Fails_WhenInvalidTimeout
```

---

#### S3-T03：新增 SqlSugarConnectionRegistry

子任务：

1. 根据配置建立 `ConnectionConfig` 集合。
2. 默认只支持 MySQL，后续预留其他 DbType。
3. 支持主连接、日志连接、审计连接。
4. 支持根据 name 获取连接配置。
5. 禁止返回 disabled 连接。

测试：

```text
SqlSugarConnectionRegistry_ResolvesDefaultConnection
SqlSugarConnectionRegistry_ResolvesNamedConnection
SqlSugarConnectionRegistry_Fails_WhenConnectionDisabled
```

---

#### S3-T04：升级 SqlSugarClientFactory

子任务：

1. 新接口：
   - `Create()`
   - `Create(string connectionName)`
   - `CreateForTenant(long tenantId)` 先抛 NotImplemented 或使用 resolver。
2. 使用 `SqlSugarScope` 多连接。
3. 注册 QueryFilter registrar hook。
4. 注册 SQL Audit registrar hook。
5. 注册 command timeout。

测试：

```text
SqlSugarClientFactory_CreatesDefaultClient
SqlSugarClientFactory_CreatesNamedClient
SqlSugarClientFactory_RegistersAuditHooks
```

---

#### S3-T05：迁移 MigrationRunner 到 Data.SqlSugar

子任务：

1. 迁移 `DbMigrationRunner`。
2. 迁移 `SeedRunner`。
3. 迁移 migration options。
4. 确保 checksum drift 逻辑保留。
5. 支持新 baseline 文件名。
6. 更新命名空间。

测试：

```text
DbMigrationRunner_AppliesNewMigration
DbMigrationRunner_SkipsAppliedMigration
DbMigrationRunner_FailsOnChecksumDrift
SeedRunner_ReplacesAdminPasswordHashSafely
```

---

#### S3-T06：新增 CodeFirst 基础骨架

新增：

```text
ICodeFirstModelProvider
ICodeFirstModelRegistry
SqlSugarCodeFirstRunner
SqlSugarSchemaValidator
```

子任务：

1. 定义实体模型提供者接口。
2. 支持模块 `.SqlSugar` 注册实体类型。
3. `ValidateAsync` 只校验，不改库。
4. `InitializeDevelopmentAsync` 仅开发/测试可用。
5. 加入环境保护。

测试：

```text
CodeFirstRunner_FailsInProductionLikeMode
CodeFirstRunner_CollectsModelsFromProviders
SchemaValidator_ReturnsMissingTableResult
```

---

### 7.3 Sprint 3 完成标准

```text
[ ] Data.SqlSugar 可编译
[ ] UnitOfWork 迁移完成
[ ] 多连接配置模型可用
[ ] MigrationRunner 迁移完成
[ ] CodeFirst 骨架可用
[ ] 旧 Persistence 暂时仍存在但不再新增平台能力
```

---

## 8. Sprint 4：Identity 模块迁移

### 8.1 目标

迁移 Auth、Users、TwoFactor 到 `WeCms.Modules.Identity`；迁移对应 Repository 实现到 `WeCms.Modules.Identity.SqlSugar`。

### 8.2 迁移范围

```text
Auth
Users
TwoFactor
AccountProfile
RefreshToken
LoginFailureLimiter
PasswordHasher
AccessToken
AuthSessionIssuer
LogoutTokenRevoker
```

### 8.3 开发任务

#### S4-T01：迁移 Identity Contracts / DTO / Records

子任务：

1. 移动 Auth DTO。
2. 移动 User DTO。
3. 移动 TwoFactor DTO。
4. 移动 AccountProfile DTO。
5. 移动 Records。
6. 更新命名空间为 `WeCms.Modules.Identity.*`。
7. 删除旧 using。
8. 更新 JSON context 或新的 OpenAPI/JSON 策略。

测试：

```text
编译测试
DTO serialization tests
OpenAPI schema tests
```

---

#### S4-T02：迁移 Identity Services

子任务：

1. 迁移 `AuthService`。
2. 迁移 `UserService`。
3. 迁移 TwoFactor 相关服务。
4. 迁移 AccountProfile 服务。
5. 确认所有依赖都是接口。
6. 将角色/权限/菜单查询从 AuthRepository 中剥离，改为依赖 AccessControl 抽象；如 AccessControl 未迁移，先定义接口并用临时 adapter。
7. 保留业务规则测试。

验收：

```text
Identity service 不依赖 AccessControl 具体实现
Identity service 不直接引用 SqlSugar
Identity service 不依赖旧 System namespace
```

---

#### S4-T03：迁移 Identity Endpoints

子任务：

1. 创建 `AuthEndpointDefinition`。
2. 创建 `AccountEndpointDefinition`。
3. 创建 `UserEndpointDefinition`。
4. 用 Minimal API `MapGroup`。
5. 所有 endpoint 添加权限、审计、限流、OpenAPI metadata。
6. 登录/刷新/2FA 接口按安全策略添加限流。
7. 禁止 Controller。

测试：

```text
AuthEndpointTests
UserEndpointPermissionMetadataTests
UserEndpointAuditMetadataTests
NoControllerArchitectureTests
```

---

#### S4-T04：迁移 Identity Repository 接口

子任务：

1. `IAuthRepository` 移至 `WeCms.Modules.Identity.Repositories`。
2. `IUserRepository` 移至 `WeCms.Modules.Identity.Repositories`。
3. `IUserTwoFactorRepository` 移至 Identity。
4. 拆分过大的 Repository 接口：
   - `IAuthUserRepository`
   - `IRefreshTokenRepository`
   - `ILoginFailureRepository`
   - `IUserRepository`
   - `IUserCredentialRepository`
5. 确认接口不暴露 SqlSugar 类型。

测试：

```text
ModuleRepositoryInterfaceBoundaryTests
```

---

#### S4-T05：迁移 Identity.SqlSugar Repository 实现

子任务：

1. 从旧 `WeCms.Persistence.Modules.System.Auth` 移动实现。
2. 从旧 `Users` 持久化移动实现。
3. 从旧 TwoFactor 持久化移动实现。
4. 命名空间改为 `WeCms.Modules.Identity.SqlSugar.*`。
5. 注入 `ISqlSugarClient` 或更推荐 `ISqlSugarClientFactory`。
6. 所有 SQL 保留参数化。
7. 所有写操作检查 affected rows。
8. 所有方法传递 `CancellationToken`。

测试：

```text
AuthRepositoryIntegrationTests
UserRepositoryIntegrationTests
RefreshTokenRepositoryIntegrationTests
TwoFactorRepositoryIntegrationTests
```

---

#### S4-T06：Identity DI 注册

子任务：

1. 新增 `AddWeCmsIdentity()`。
2. 新增 `AddWeCmsIdentitySqlSugar()`。
3. 注册 Service、Repository、Auth handler、Token service、PasswordHasher、Clock 依赖。
4. 更新 `Program.cs`。
5. 移除旧 `AddWeCmsSystemAuth`、`AddWeCmsSystemUsers`、`AddWeCmsSystemTwoFactor` 调用。

测试：

```text
DiBoundaryTests
IdentityServiceResolutionTests
```

---

#### S4-T07：Identity 权限和 seed 更新

子任务：

1. `sys:user:*` 权限迁移到 Identity 权限定义。
2. 登录相关权限保持匿名/内部策略。
3. 用户写接口审计 metadata 更新为 `identity/user/action`。
4. 更新 seed 生成或 baseline SQL。
5. 更新 OpenAPI `x-wecms-permission`。

验收：

```text
用户权限码全部存在
super_admin 拥有 Identity 管理权限
OpenAPI 权限元数据正确
```

---

### 8.4 Sprint 4 完成标准

```text
[ ] Auth / Users / TwoFactor 已迁移
[ ] 登录、刷新、Me、用户 CRUD、2FA 测试通过
[ ] 无 WeCms.Modules.System.Auth / Users / TwoFactor 引用
[ ] Identity 不引用 SqlSugar
[ ] SqlSugar 只在 Identity.SqlSugar
```

---

## 9. Sprint 5：AccessControl 模块迁移与权限平台升级

### 9.1 目标

迁移 Roles、Permissions、Menus，并升级为 RBAC + URL 权限 + 按钮权限模型。

### 9.2 开发任务

#### S5-T01：迁移 Role / Permission / Menu DTO 与 Records

子任务：

1. 移动 Role DTO / Records。
2. 移动 Permission DTO / Records。
3. 移动 Menu DTO / Records。
4. 更新命名空间。
5. 更新 OpenAPI schema。
6. 删除旧 System using。

---

#### S5-T02：建立权限定义模型

新增：

```text
PermissionDefinition
PermissionGroupDefinition
PermissionKind
PermissionAction
PermissionDefinitionProvider
PermissionRegistry
```

子任务：

1. 定义权限类型：Menu、Page、Button、Url、Api、Data。
2. 定义权限 code 命名规范：`module:resource:action`。
3. 定义权限所属模块。
4. 定义权限显示名称和描述。
5. 支持从各模块注册权限定义。
6. 添加重复 code 检查。

测试：

```text
PermissionRegistry_FailsOnDuplicateCode
PermissionRegistry_GroupsByModule
PermissionDefinition_CodeFormatTests
```

---

#### S5-T03：建立 URL 权限模型

新增表/模型：

```text
sys_permission_endpoint
PermissionEndpointBinding
```

子任务：

1. 定义 `http_method`。
2. 定义 `route_pattern`。
3. 定义 `permission_code`。
4. Endpoint metadata 自动生成绑定信息。
5. OpenAPI 输出 `x-wecms-permission`。
6. 新增质量检查：每个受保护 endpoint 有 permission binding。

测试：

```text
EndpointPermissionBindingTests
OpenApiPermissionExtensionTests
PermissionCoverageTests
```

---

#### S5-T04：建立按钮权限模型

新增表/模型：

```text
sys_permission_button
ButtonPermissionDefinition
```

子任务：

1. 定义按钮 key。
2. 定义页面/菜单 code。
3. 定义按钮 permission code。
4. 用户 Me 接口返回按钮权限列表。
5. 前端可根据权限 code 控制按钮。

测试：

```text
ButtonPermissionDefinitionTests
AccessProfile_ReturnsButtonPermissions
```

---

#### S5-T05：迁移 Role Service / Repository

子任务：

1. 迁移 `RoleService`。
2. 迁移 `IRoleRepository`。
3. 迁移 `RoleRepository` 到 `AccessControl.SqlSugar`。
4. 保留 locked role 保护。
5. 保留 permission version bump。
6. 使用 `IAuditWriter` 替代直接写 sys_audit_log；如 Audit 未迁移完成，先用抽象 + adapter。

测试：

```text
RoleServiceTests
RoleRepositoryIntegrationTests
LockedRoleIntegrationTests
PermissionVersionTests
```

---

#### S5-T06：迁移 Permission Service / Repository

子任务：

1. 迁移 PermissionChecker。
2. 迁移 PermissionEndpointFilter。
3. 迁移 PermissionManagementService。
4. 迁移 `IPermissionRepository`。
5. 迁移 `PermissionRepository` 到 AccessControl.SqlSugar。
6. 将安全事件写入抽象化。
7. 保留权限拒绝记录。

测试：

```text
PermissionCheckerTests
PermissionEndpointFilterTests
PermissionRepositoryIntegrationTests
PermissionDeniedSecurityEventTests
```

---

#### S5-T07：迁移 Menu Service / Repository

子任务：

1. 迁移 Menu DTO / Service。
2. 迁移 MenuTreeBuilder。
3. 迁移 `IMenuRepository`。
4. 迁移 MenuRepository 到 AccessControl.SqlSugar。
5. 菜单和权限绑定统一建模。
6. 用户 AccessProfile 返回菜单树。

测试：

```text
MenuServiceTests
MenuTreeBuilderTests
MenuRepositoryIntegrationTests
AccessProfile_MenuTreeTests
```

---

#### S5-T08：建立 AccessProfileService

子任务：

1. 新增 `IAccessProfileService`。
2. 根据 userId 返回：
   - roles
   - permissions
   - menus
   - buttons
   - permissionVersion
3. Identity 的 Me 接口改为调用该服务。
4. 支持后续缓存。

测试：

```text
AccessProfileService_ReturnsRolesPermissionsMenusButtons
AuthMe_UsesAccessProfileService
```

---

### 9.3 Sprint 5 完成标准

```text
[ ] Roles / Permissions / Menus 已迁移
[ ] RBAC 正常
[ ] URL 权限绑定可生成
[ ] 按钮权限模型可用
[ ] AccessProfile 可返回菜单/权限/按钮
[ ] OpenAPI x-wecms-permission 正确
[ ] 写接口权限覆盖通过
```

---

## 10. Sprint 6：Organization 模块迁移

### 10.1 目标

迁移 Departments 和 Posts，并将 Posts 重命名为 Positions。

### 10.2 开发任务

#### S6-T01：重命名 Posts 为 Positions

子任务：

1. `PostDtos` → `PositionDtos`。
2. `PostService` → `PositionService`。
3. `IPostRepository` → `IPositionRepository`。
4. `PostRepository` → `PositionRepository`。
5. `PostPermissions` → `PositionPermissions`。
6. 路由 `/posts` 可改为 `/positions`。
7. 数据表 `sys_post` 改为 `sys_position`。
8. 关系表 `sys_user_post` 改为 `sys_user_position`。
9. 删除所有 `Post` 命名残留，除非明确是 CMS 内容域但本次不应出现。

测试：

```text
rg "sys_post|UserPost|PostService|IPostRepository|PostPermissions" backend/src 应返回空
PositionServiceTests
PositionRepositoryIntegrationTests
```

---

#### S6-T02：迁移 Department

子任务：

1. 移动 Department DTO / Records。
2. 移动 DepartmentService。
3. 移动 IDepartmentRepository。
4. 移动 DepartmentRepository 到 Organization.SqlSugar。
5. 保留部门树构建。
6. 添加部门删除前依赖检查。
7. 添加审计 metadata。

测试：

```text
DepartmentServiceTests
DepartmentTreeTests
DepartmentRepositoryIntegrationTests
DepartmentEndpointPermissionTests
```

---

#### S6-T03：迁移 Position

子任务：

1. 移动 Position DTO / Records。
2. 移动 PositionService。
3. 移动 IPositionRepository。
4. 移动 PositionRepository 到 Organization.SqlSugar。
5. 添加职位启用/禁用/删除逻辑。
6. 添加审计 metadata。

测试：

```text
PositionServiceTests
PositionRepositoryIntegrationTests
```

---

#### S6-T04：建立 OrganizationLookupService

子任务：

1. 新增 `IOrganizationLookupService`。
2. 提供部门存在检查。
3. 提供职位 ID 批量存在检查。
4. Identity 创建/更新用户调用该抽象。
5. 禁止 Identity 直接依赖 Organization Repository。

测试：

```text
IdentityUserService_UsesOrganizationLookup
OrganizationLookupServiceTests
LayerDependencyTests
```

---

### 10.3 Sprint 6 完成标准

```text
[ ] Departments 已迁移
[ ] Posts 已重命名为 Positions
[ ] 用户创建/更新仍可校验部门和职位
[ ] 代码中无旧 Post 系统岗位命名残留
```

---

## 11. Sprint 7：Configuration 模块迁移

### 11.1 目标

迁移 Settings、Dicts、I18n，并为缓存预留失效机制。

### 11.2 开发任务

#### S7-T01：迁移 Settings

子任务：

1. 移动 Settings DTO / Records。
2. 移动 SettingService。
3. 移动 ISettingRepository。
4. 移动 SettingRepository 到 Configuration.SqlSugar。
5. 保留敏感配置保护规则。
6. 写操作调用缓存失效抽象。
7. 更新权限码为 `config:setting:*` 或保留 `sys:setting:*` 但统一映射。

测试：

```text
SettingServiceTests
SettingSecurityTests
SettingRepositoryIntegrationTests
SettingCacheInvalidationTests
```

---

#### S7-T02：迁移 Dicts

子任务：

1. 移动 Dict DTO / Records。
2. 移动 DictService。
3. 移动 IDictRepository。
4. 移动 DictRepository 到 Configuration.SqlSugar。
5. 类型和值分离。
6. 字典变更触发缓存失效。

测试：

```text
DictServiceTests
DictRepositoryIntegrationTests
DictCacheInvalidationTests
```

---

#### S7-T03：迁移 I18n

子任务：

1. 移动 I18n DTO / Records。
2. 移动 I18nMessageService。
3. 移动 II18nMessageRepository。
4. 移动 I18nMessageRepository 到 Configuration.SqlSugar。
5. 公共消息接口保留 AllowAnonymous。
6. 用户切换语言接口保留权限/认证策略。
7. 引入 I18n 缓存失效接口。

测试：

```text
I18nServiceTests
I18nRepositoryIntegrationTests
PublicI18nEndpointTests
I18nCacheInvalidationTests
```

---

#### S7-T04：建立 ConfigCacheInvalidator 抽象

子任务：

1. 新增 `IConfigurationCacheInvalidator`。
2. 支持 setting/dict/i18n 三类缓存失效。
3. 当前可用 Noop 实现，Sprint 11 接入真实缓存。
4. 所有写操作调用失效接口。

测试：

```text
SettingWrite_CallsCacheInvalidator
DictWrite_CallsCacheInvalidator
I18nWrite_CallsCacheInvalidator
```

---

### 11.3 Sprint 7 完成标准

```text
[ ] Settings / Dicts / I18n 已迁移
[ ] 写操作已预留缓存失效
[ ] 公共 i18n 接口正常
[ ] Configuration 不依赖 SqlSugar
```

---

## 12. Sprint 8：Audit / Security / FileCenter / Platform 迁移

### 12.1 目标

迁移剩余系统基础模块，并建立审计、安全、文件、平台边界。

### 12.2 开发任务

#### S8-T01：迁移 Audit

子任务：

1. 移动 Logs DTO / Records。
2. 移动 LogService。
3. 移动 ILogRepository。
4. 移动 LogRepository 到 Audit.SqlSugar。
5. 新增 `IAuditWriter`。
6. 新增 `IExceptionAuditWriter`。
7. 新增 `ISqlAuditQueryService` 预留接口。
8. 更新审计日志 endpoint 为 `AuditEndpointDefinition`。

测试：

```text
AuditLogServiceTests
LoginLogServiceTests
AuditRepositoryIntegrationTests
AuditEndpointPermissionTests
```

---

#### S8-T02：迁移 Security

子任务：

1. 移动 Security DTO / Records。
2. 移动 SecurityBanService。
3. 移动 SecurityAlerting。
4. 移动 SecurityEventClassifier。
5. 移动 ISecurityBanRepository。
6. 移动 SecurityBanRepository 到 Security.SqlSugar。
7. 新增 `ISecurityEventWriter`。
8. RateLimit 安全事件写入使用该抽象。

测试：

```text
SecurityBanServiceTests
SecurityEventClassifierTests
SecurityRepositoryIntegrationTests
RateLimitSecurityEventTests
```

---

#### S8-T03：迁移 FileCenter

子任务：

1. 移动 File DTO / Records。
2. 移动 FileService。
3. 移动 FileUploadPolicies。
4. 移动 IFileRepository。
5. 移动 FileRepository 到 FileCenter.SqlSugar。
6. 保留文件存储实现于 Infrastructure。
7. FileCenter 只依赖 `IFileStorage` 和 `IFileScanService` 抽象。
8. 文件下载/预览必须鉴权。
9. 禁止返回物理文件路径。

测试：

```text
FileServiceTests
FileUploadPolicyTests
FileRepositoryIntegrationTests
FileEndpointSecurityTests
```

---

#### S8-T04：迁移 Platform

子任务：

1. 移动 SystemEndpointExtensions 为 Platform endpoint。
2. 移动 SystemRecords。
3. 移动 ISystemDatabaseProbe / ISystemMigrationProbe。
4. Repository / Probe 实现根据需要放入 Data.SqlSugar 或 Platform.SqlSugar。
5. 路由保留 `/health/live`、`/health/ready`、`/api/v1/system/ping` 可接受，但模块名为 Platform。
6. 添加平台依赖状态检查。

测试：

```text
PlatformEndpointTests
HealthCheckTests
DatabaseProbeTests
MigrationProbeTests
```

---

### 12.3 Sprint 8 完成标准

```text
[ ] Audit / Security / FileCenter / Platform 已迁移
[ ] 审计查询正常
[ ] 安全中心正常
[ ] 文件管理正常
[ ] 健康检查正常
[ ] 剩余 System 子目录大幅减少
```

---

## 13. Sprint 9：删除旧 System / Persistence 与重置 baseline

### 13.1 目标

彻底删除旧 `WeCms.Modules.System` 和旧 `WeCms.Persistence`，重置系统基础数据库 baseline。

### 13.2 开发任务

#### S9-T01：删除旧 WeCms.Modules.System

子任务：

1. 确认所有子目录已迁移。
2. 删除旧项目文件。
3. 删除旧 namespace 引用。
4. 更新 solution。
5. 更新 Program.cs。
6. 更新测试引用。
7. 更新 OpenAPI generator 引用。
8. 执行：

```bash
rg "WeCms.Modules.System" backend/src backend/tests
```

验收：

```text
rg 无结果或仅文档历史说明中出现
```

---

#### S9-T02：删除旧 WeCms.Persistence

子任务：

1. 确认 Data.SqlSugar 接管平台能力。
2. 确认各模块 `.SqlSugar` 接管 Repository 实现。
3. 删除旧 Persistence 项目。
4. 更新 solution。
5. 更新 using。
6. 执行：

```bash
rg "WeCms.Persistence" backend/src backend/tests
```

验收：

```text
rg 无生产代码结果
```

---

#### S9-T03：重置数据库 baseline

子任务：

1. 删除旧 `database/migrations/*.sql`。
2. 删除旧 `database/seeds/*.sql`。
3. 新增 `000001_baseline_system_schema.sql`。
4. 新增 `000002_seed_system_permissions.sql`。
5. 新增 `000003_seed_super_admin.sql`。
6. 将 `sys_post` 改为 `sys_position`。
7. 将 `sys_user_post` 改为 `sys_user_position`。
8. 新增权限 endpoint/button 表。
9. 新增 outbox 表可放 Sprint 12，也可先预留。
10. 更新 migration smoke test。

测试：

```text
MigrationAndSeedSmokeTests
BaselineSchemaTests
LockedRoleSeedTests
PermissionSeedCoverageTests
```

---

#### S9-T04：更新质量门禁最终规则

子任务：

1. 关闭 transition allow-list。
2. `NoSystemGodModuleTests` 改为强制。
3. `NoPersistenceGodModuleTests` 改为强制。
4. DB boundary 只允许 Data.SqlSugar / *.SqlSugar。
5. no Controller 测试强制。
6. 更新脚本文案。

验收：

```bash
bash scripts/quality-gate-backend.sh
```

如果没有 MySQL，允许中间报告说明并运行：

```bash
WECMS_SKIP_MYSQL_INTEGRATION_TESTS=true bash scripts/quality-gate-backend.sh
```

但最终验收必须跑完整 MySQL gate。

---

### 13.3 Sprint 9 完成标准

```text
[ ] 旧 System 删除
[ ] 旧 Persistence 删除
[ ] 新 baseline 可初始化数据库
[ ] 所有生产引用指向新模块
[ ] 架构测试强制执行新规则
```

---

## 14. Sprint 10：SqlSugar 数据平台完整升级

### 14.1 目标

完成 CodeFirst 建模、Migration 固化、QueryFilter、多库/多租户连接管理、SQL 日志与审计。

### 14.2 开发任务

#### S10-T01：实体基类与接口

新增：

```text
IEntity<TKey>
ISoftDeleteEntity
IAuditedEntity
ITenantEntity
ISiteScopedEntity
IDataScopedEntity
EntityBase
TenantEntityBase
SiteScopedEntityBase
```

子任务：

1. 定义基础接口在 `WeCms.Data.SqlSugar/Entities/Common` 或 `WeCms.Shared.Data`。
2. 若接口会被模块层感知，放 `WeCms.Shared.Data`。
3. 基类放 `WeCms.Data.SqlSugar`。
4. 为系统基础表设计实体。
5. 配置 SugarColumn 属性。
6. 添加索引属性。

测试：

```text
EntityMetadataTests
CodeFirstModelProviderTests
```

---

#### S10-T02：CodeFirst Model Provider

子任务：

1. 每个 `.SqlSugar` 模块实现 `ICodeFirstModelProvider`。
2. 返回本模块实体类型。
3. Data.SqlSugar 汇总 provider。
4. 重复表名检测。
5. 缺少 SugarTable 检测。

测试：

```text
CodeFirstModelRegistry_FailsOnDuplicateTable
CodeFirstModelRegistry_FailsOnMissingSugarTable
```

---

#### S10-T03：Schema Validator

子任务：

1. 对比实体和数据库表。
2. 检查表缺失。
3. 检查字段缺失。
4. 检查字段长度。
5. 检查 nullable。
6. 检查索引。
7. CI 输出报告。

测试：

```text
SchemaValidator_DetectsMissingTable
SchemaValidator_DetectsMissingColumn
SchemaValidator_DetectsIndexMismatch
```

---

#### S10-T04：QueryFilter Registrar

子任务：

1. 新增 `IQueryFilterRegistrar`。
2. 实现 SoftDeleteFilter。
3. 实现 TenantFilter。
4. 实现 DataScopeFilter。
5. 实现可控绕过机制。
6. 绕过必须带 reason。
7. 绕过必须写 audit。

测试：

```text
SoftDeletedRowsHiddenByDefault
TenantRowsAreIsolated
DataScopeFiltersRows
BypassFilterRequiresReason
```

---

#### S10-T05：多库 / 多租户连接管理

子任务：

1. 支持 main/log/audit 连接。
2. 支持 tenant 连接解析接口。
3. 默认共享库 + tenant_id 模式。
4. 预留独立库模式。
5. 单个 UnitOfWork 默认只管理一个连接。
6. 跨库一致性使用 Outbox，不做分布式事务。

测试：

```text
DefaultConnectionResolutionTests
NamedConnectionResolutionTests
TenantSharedDbResolutionTests
TenantDedicatedDbResolutionTests_WhenConfigured
```

---

#### S10-T06：SQL 日志与审计

新增：

```text
ISqlAuditSink
SqlAuditRecord
SqlAuditRedactor
SqlSugarSqlAuditRegistrar
SqlAuditOptions
```

子任务：

1. 注册 SqlSugar AOP。
2. 记录慢 SQL。
3. 记录错误 SQL。
4. 记录 connection name。
5. 记录 traceId / userId / tenantId。
6. 参数脱敏。
7. 防递归。
8. 开发环境可记录全部 SQL，测试环境可验证，生产策略暂不考虑但保留配置。

测试：

```text
SqlAudit_RecordsSlowSql
SqlAudit_RecordsFailedSql
SqlAudit_RedactsSensitiveParameters
SqlAudit_DoesNotAuditItselfRecursively
```

---

### 14.3 Sprint 10 完成标准

```text
[ ] CodeFirst validate 可用
[ ] Migration baseline 与实体一致
[ ] QueryFilter 生效
[ ] 多库配置可解析
[ ] 多租户上下文可用
[ ] SQL 日志脱敏
[ ] SQL 审计测试通过
```

---

## 15. Sprint 11：统一缓存 + AOP

### 15.1 目标

建立统一缓存抽象，建立 AOP 事务与缓存拦截。

### 15.2 开发任务

#### S11-T01：统一缓存抽象

新增：

```text
ICache
ICacheSerializer
ICacheKeyBuilder
ICacheInvalidator
CacheOptions
CacheEntryOptions
```

子任务：

1. 定义同步/异步接口时优先异步。
2. 支持 Get/Set/Remove/GetOrCreate。
3. 支持 prefix remove 或 tag remove。
4. Key 必须包含：app、tenant、module、resource、version。
5. 序列化使用 System.Text.Json 或 MessagePack，先用 JSON。

测试：

```text
CacheKeyBuilder_IncludesTenantModuleResource
MemoryCache_GetSetRemove
CacheInvalidator_RemovesByPrefix
```

---

#### S11-T02：MemoryCache Provider

子任务：

1. 实现 MemoryCacheProvider。
2. 注册 `AddWeCmsCaching`。
3. 支持过期时间。
4. 支持 null 值缓存策略。
5. 添加并发 GetOrCreate 测试。

---

#### S11-T03：Redis Provider 预留

本 Sprint 可只定义接口和配置，不强制连接 Redis。

子任务：

1. 定义 Redis options。
2. 新增 RedisCacheProvider stub 或完整实现。
3. 如果未引入 Redis 包，必须记录 TODO 和 ADR。
4. 不允许业务代码直接依赖 Redis。

---

#### S11-T04：AOP Attribute

新增：

```text
[UnitOfWork]
[Cacheable]
[CacheEvict]
[Audited]
```

子任务：

1. Attribute 只放在 Application Service 接口或实现方法上。
2. 禁止放在 Repository。
3. 禁止放在 Endpoint Handler。
4. Attribute 支持 order 或由 interceptor order 控制。

测试：

```text
AopAttributeUsageTests
```

---

#### S11-T05：TransactionInterceptor

子任务：

1. 支持 async Task。
2. 支持 async Task<T>。
3. 支持 CancellationToken。
4. 成功 commit。
5. 异常 rollback。
6. rollback 后 rethrow。
7. 禁止同步阻塞。

测试：

```text
TransactionInterceptor_CommitsOnSuccess
TransactionInterceptor_RollsBackAndRethrowsOnException
TransactionInterceptor_DoesNotBlockSynchronously
```

---

#### S11-T06：CacheInterceptor

子任务：

1. 根据 `[Cacheable]` 生成 key。
2. 支持参数 hash。
3. 支持 tenant key。
4. 支持 miss 后执行方法。
5. 支持 `[CacheEvict]` 删除相关 key。
6. 支持异常时不写缓存。
7. 支持 null 缓存策略。

测试：

```text
CacheInterceptor_ReturnsCachedValue
CacheInterceptor_WritesOnMiss
CacheInterceptor_EvictsAfterMutation
CacheInterceptor_DoesNotCacheException
```

---

#### S11-T07：Autofac 注册

子任务：

1. 引入 Autofac 包。
2. 配置 `UseServiceProviderFactory(new AutofacServiceProviderFactory())`。
3. 只对 Application Service 接口启用拦截。
4. 禁止 Repository 拦截。
5. 更新 DI boundary tests。

测试：

```text
AutofacModule_RegistersApplicationServices
RepositoryTypes_AreNotIntercepted
```

---

### 15.3 Sprint 11 完成标准

```text
[ ] ICache 可用
[ ] MemoryCache 可用
[ ] AOP 事务可用
[ ] AOP 缓存可用
[ ] Autofac 注册可用
[ ] 无同步阻塞
[ ] Repository 未被 AOP 拦截
```

---

## 16. Sprint 12：EventBus + Outbox

### 16.1 目标

建立系统基础事件总线和 Outbox 机制，为后续 CMS 内容发布异步动作预留。

### 16.2 开发任务

#### S12-T01：事件抽象

新增：

```text
IIntegrationEvent
IntegrationEventBase
IEventHandler<TEvent>
IEventBus
IOutboxWriter
IOutboxDispatcher
```

子任务：

1. 事件包含 id、type、occurredAt、traceId、tenantId。
2. Handler 支持 CancellationToken。
3. EventBus 支持 PublishAsync。
4. 第一阶段 EventBus 可以是 InMemory + Outbox。

测试：

```text
EventBus_PublishesToHandlers
EventBus_HandlerFailureDoesNotSwallowException_WhenConfigured
```

---

#### S12-T02：Outbox 表和 Repository

新增 baseline 或 migration：

```text
sys_outbox_message
```

字段：

```text
id
event_id
event_type
aggregate_type
aggregate_id
payload_json
status
retry_count
available_at
locked_at
processed_at
error
created_at
```

子任务：

1. 新增 Outbox entity。
2. 新增 OutboxRepository。
3. 写事件时与业务事务同库同事务。
4. 支持 pending / processing / processed / failed。
5. 支持重试次数。

测试：

```text
OutboxWriter_WritesMessage
OutboxRepository_LocksPendingMessages
OutboxRepository_MarksProcessed
OutboxRepository_MarksFailedWithRetry
```

---

#### S12-T03：Outbox Dispatcher

子任务：

1. 实现轮询 dispatcher。
2. 支持 batch size。
3. 支持 retry delay。
4. 支持并发锁。
5. 支持幂等 handler。
6. 支持日志和审计。

测试：

```text
OutboxDispatcher_DispatchesPendingMessages
OutboxDispatcher_RetriesFailedMessages
OutboxDispatcher_DoesNotDoubleProcessLockedMessage
```

---

#### S12-T04：系统基础事件

首批事件：

```text
UserCreatedEvent
UserDisabledEvent
RolePermissionsChangedEvent
MenuChangedEvent
SettingChangedEvent
DictChangedEvent
I18nChangedEvent
SecurityBanCreatedEvent
```

子任务：

1. 事件由 Application Service 发布。
2. 事件写入 Outbox。
3. 设置/字典/i18n 事件触发缓存失效 handler。
4. 权限变更事件触发权限缓存失效。

测试：

```text
SettingChangedEvent_EvictsSettingCache
RolePermissionsChangedEvent_EvictsAccessProfileCache
EventHandlers_AreIdempotent
```

---

### 16.3 Sprint 12 完成标准

```text
[ ] EventBus 抽象可用
[ ] Outbox 表可用
[ ] Dispatcher 可处理事件
[ ] Handler 幂等
[ ] 权限/配置变更可通过事件触发缓存失效
```

---

## 17. Sprint 13：Swagger / Scalar / MiniProfiler / OpenAPI 元数据

### 17.1 目标

增强后台基础设施，但不引入 Controller。

### 17.2 开发任务

#### S13-T01：Swagger / Scalar UI

子任务：

1. 引入 Swagger/Scalar 所需包。
2. 只在 Development 或明确配置时启用。
3. 不调用 `AddControllers`。
4. 使用 Endpoint Metadata 生成文档。
5. 增加安全 scheme。
6. 显示权限码扩展。

测试：

```text
Swagger_IsNotUsingControllers
OpenApi_ContainsBearerAuth
OpenApi_ContainsPermissionExtensions
```

---

#### S13-T02：OpenAPI Metadata 生成升级

子任务：

1. 从 Endpoint Metadata 读取：module、permission、audit、rate-limit。
2. 输出：
   - `x-wecms-module`
   - `x-wecms-permission`
   - `x-wecms-audit`
   - `x-wecms-rate-limit`
3. 移除或减少手工 endpoint descriptor。
4. 更新 OpenAPI coverage tests。

测试：

```text
OpenApiExport_IncludesModuleMetadata
OpenApiExport_IncludesAuditMetadataForWrites
OpenApiExport_IncludesRateLimitMetadata
```

---

#### S13-T03：MiniProfiler

子任务：

1. 引入 MiniProfiler。
2. 注册 HTTP request timing。
3. SQL audit 事件可写入 MiniProfiler timing。
4. 仅开发环境默认启用。
5. 不暴露敏感 SQL 参数。

测试：

```text
MiniProfiler_RegisteredInDevelopment
MiniProfiler_NotEnabledByDefaultInNonDevelopment
SqlTiming_DoesNotExposeSensitiveParameters
```

---

### 17.3 Sprint 13 完成标准

```text
[ ] Swagger/Scalar 可访问
[ ] OpenAPI export 继续通过
[ ] Metadata 扩展完整
[ ] MiniProfiler 可记录 HTTP/SQL timing
[ ] 无 Controller
```

---

## 18. Sprint 14：最终清理、文档与全量验收

### 18.1 目标

完成所有重构后的清理、文档、质量门禁和验收报告。

### 18.2 开发任务

#### S14-T01：全仓库命名清理

检查：

```bash
rg "WeCms.Modules.System" backend/src backend/tests
rg "WeCms.Persistence" backend/src backend/tests
rg "ControllerBase|AddControllers|MapControllers|ApiController" backend/src backend/tests
rg "sys_post|UserPost|PostService|IPostRepository|PostPermissions" backend/src backend/tests database
```

验收：

```text
无非法命名残留
文档历史说明除外
```

---

#### S14-T02：全量测试

命令：

```bash
dotnet restore backend/WeCms.slnx
dotnet build backend/WeCms.slnx -warnaserror -p:SkipFrontendBuild=true
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj -p:SkipFrontendBuild=true
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:SkipFrontendBuild=true
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings -p:SkipFrontendBuild=true
```

最终完整门禁：

```bash
bash scripts/quality-gate-backend.sh
```

---

#### S14-T03：更新文档

更新：

```text
README.md
AGENTS.md
code_review.md
docs/adr/*
docs/context/*
docs/ops/database-production.md 或新的 database-governance.md
docs/runbooks/*
```

内容：

1. 新模块结构。
2. Minimal API only 决策。
3. 如何新增 Endpoint。
4. 如何新增权限。
5. 如何新增 Repository。
6. 如何新增 CodeFirst entity。
7. 如何生成 migration baseline。
8. 如何运行测试和 quality gate。

---

#### S14-T04：生成最终验收报告

新增：

```text
docs/reports/system-foundation-upgrade-acceptance.md
```

包含：

1. 完成的 Sprint 列表。
2. 模块拆分结果。
3. 旧模块删除结果。
4. 数据库 baseline 结果。
5. 测试命令和结果。
6. 剩余风险。
7. 后续 CMS 内容模块建议入口。

---

### 18.3 Sprint 14 完成标准

```text
[ ] 全量 build 通过
[ ] 全量 unit tests 通过
[ ] 全量 architecture tests 通过
[ ] integration tests 通过
[ ] quality gate 通过
[ ] 文档更新完成
[ ] 验收报告完成
[ ] 可以进入 CMS 内容模块设计阶段
```

---

## 19. Codex 任务模板

每个任务交给 Codex 时必须使用以下模板。

```markdown
# Codex 任务：<任务编号> <任务名称>

## 背景
基于 WeCMS-next 系统基础破坏性升级开发计划书执行。
当前决策：继续 Minimal API，不引入 Controller Web API。

## 目标
<明确目标>

## 允许修改
<列出允许修改的文件/目录>

## 禁止修改
- 不得引入 Controller / ControllerBase。
- 不得调用 AddControllers / MapControllers。
- 不得修改 CMS 内容模块。
- 不得绕过架构测试。
- 不得删除测试来通过门禁。
- 不得在模块层写 SQL。

## 子任务
1. ...
2. ...
3. ...

## TDD 要求
1. 先新增或修改测试，使其失败。
2. 再实现最小代码。
3. 最后重构并确认测试通过。

## 验证命令
```bash
<命令>
```

## 验收标准
- [ ] ...
- [ ] ...

## 输出要求
请输出：
1. 修改文件列表。
2. 测试命令和结果。
3. 是否满足验收标准。
4. 剩余风险。
```

---

## 20. 全局 Definition of Done

每个 Sprint 和最终交付必须满足：

```text
[ ] 未引入 Controller / ControllerBase
[ ] 未调用 AddControllers / MapControllers
[ ] Endpoint 仍为 Minimal API
[ ] 新增 Endpoint 已绑定权限或 AllowAnonymous/InternalOnly
[ ] 写操作 Endpoint 已绑定审计 metadata
[ ] 高风险写操作已绑定限流
[ ] Service 只依赖接口
[ ] Repository 实现只在 *.SqlSugar
[ ] SqlSugar 只在 Data.SqlSugar 和 *.SqlSugar
[ ] 模块层无 SQL 文本
[ ] 无 SELECT *
[ ] 无 SQL 拼接用户输入
[ ] 事务支持 async 且异常重新 throw
[ ] 缓存 Key 包含租户/模块/资源维度
[ ] SQL 日志脱敏
[ ] EventBus Handler 幂等
[ ] 单元测试通过
[ ] 集成测试通过
[ ] 架构测试通过
[ ] OpenAPI 导出通过
[ ] quality gate 通过或明确列出暂时失败项和原因
[ ] 文档同步更新
```

---

## 21. 任务执行优先级建议

第一轮必须严格按顺序：

```text
S0 -> S1 -> S2 -> S3
```

原因：

1. 不先改规则，后续 AOP / CodeFirst / Swagger / MiniProfiler 会被旧规则阻断。
2. 不先建项目骨架，模块迁移没有目标位置。
3. 不先建 Endpoint 平台，迁移后的模块会继续复制旧 Endpoint 写法。
4. 不先建 Data.SqlSugar 骨架，拆 Persistence 会失控。

第二轮按模块迁移：

```text
S4 Identity
S5 AccessControl
S6 Organization
S7 Configuration
S8 Audit/Security/FileCenter/Platform
```

第三轮做平台增强：

```text
S9 删除旧结构与 baseline
S10 SqlSugar 数据平台完整升级
S11 缓存 + AOP
S12 EventBus + Outbox
S13 Swagger / MiniProfiler
S14 最终验收
```

---

## 22. 风险与缓解

| 风险 | 等级 | 缓解 |
|---|---|---|
| 模块拆分导致大量命名空间错误 | 高 | 每个模块单独迁移，禁止并行迁移多个模块。 |
| 权限码遗漏 | 高 | OpenAPI 权限覆盖测试 + seed 覆盖测试。 |
| 写操作审计遗漏 | 高 | 写接口审计覆盖脚本。 |
| System / Persistence 残留引用 | 高 | `rg` 检查 + 架构测试。 |
| AOP 事务吞异常 | 高 | Interceptor 单元测试强制 rollback + rethrow。 |
| QueryFilter 对 Ado SQL 不生效 | 高 | 新模块 Queryable 优先，旧 raw SQL 使用 Filter Builder 或逐步迁移。 |
| SQL 审计泄露敏感参数 | 高 | Redactor 测试覆盖 password/token/secret/2FA。 |
| EventBus 重复消费 | 中 | Handler 幂等测试。 |
| Swagger/Scalar 引入 Controller 依赖 | 中 | NoControllerArchitectureTests。 |
| 过度拆分导致复杂度上升 | 中 | 采用 8 个中等粒度模块，不按单实体拆项目。 |

---

## 23. 最终交付物清单

完成本计划后，仓库应具备：

```text
1. 新模块化项目结构
2. 新 Minimal API EndpointDefinition 体系
3. 新 Data.SqlSugar 数据平台
4. 新 CodeFirst 实体模型
5. 新 Migration baseline
6. QueryFilter 运行时治理
7. 多库 / 多租户连接管理
8. SQL 日志与审计
9. RBAC + URL 权限 + 按钮权限
10. 数据权限基础框架
11. 统一缓存抽象
12. AOP 事务与缓存
13. 审计 / 异常 / SQL 日志统一模型
14. Swagger/Scalar + OpenAPI metadata
15. MiniProfiler
16. EventBus + Outbox
17. 更新后的 AGENTS / code_review / ADR / quality gate
18. 完整测试和最终验收报告
```

---

## 24. 最终结论

本开发计划要求以 **Minimal API only** 为前提，执行一次系统基础平台破坏性升级。

核心执行路径是：

```text
规则先行
项目骨架先行
Endpoint 平台先行
Data.SqlSugar 骨架先行
再迁移业务模块
再删除旧结构
再增强数据平台、缓存、AOP、EventBus、Swagger、MiniProfiler
最后全量验收
```

最终目标不是把项目重写成另一个框架，而是将当前 WeCMS-next 升级为：

> **模块边界清晰、Minimal API 规范统一、SqlSugar 数据平台完善、权限/审计/缓存/AOP/EventBus 完整、符合 C# 工程规范、高内聚低耦合、可被 Codex 按任务稳定执行的系统基础平台。**
