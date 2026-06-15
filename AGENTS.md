# AGENTS.md — WeCMS Next Codex Agent Instructions

> 本文件是 WeCMS Next 项目的 Codex / AI Coding Agent 项目级指令。  
> 所有 Codex App / Codex CLI / 代码生成 / 自动修复 / PR 任务必须优先遵守本文件；工程原则、TDD、Spec 先行和质量门禁同样适用。  
> 如果本文件与普通任务描述冲突，以本文件为准；如果本文件与用户明确的新架构决策冲突，必须先停止并提示人工确认。

---

## 0. Agent 行为总原则

1. **先理解，再编码。** 任何新阶段开始前，必须先阅读 `docs/context` 和本文件。
2. **小步提交。** 每次任务只处理一个明确目标，不允许一次性生成或重构整个系统。
3. **先给计划，再改文件。** 编码任务开始前必须输出执行计划、影响文件、验证命令和风险点。
4. **改动必须可验证。** 每次代码变更后必须说明如何运行 build、test、AOT publish、frontend typecheck/build。
5. **不得绕过约束。** 不能为了“快速实现”引入违反 Native AOT、SqlSugar ORM、后端契约优先、安全规则的方案。
6. **遇到不确定必须暂停说明。** 尤其是认证、权限、数据库迁移、文件安全、AI 边界、生产数据处理。
7. **不处理敏感数据。** 不读取、不生成、不提交真实 secret、token、密码、2FA secret、生产数据 dump。
8. **AI 只作为开发辅助。** 一期不得实现运行时 AI 功能；AI 接入是二期独立项目。

---

## 1. 项目背景

WeCMS Next 是从现有 ThinkPHP CMS 系统完整迁移重构的新系统。

旧系统技术栈：

- ThinkPHP 8
- PHP 8.3+
- MySQL
- 服务端模板
- TailwindCSS / jQuery / iframe 后台
- Session + token 登录
- ThinkPHP Auth 风格 RBAC

新系统目标技术栈：

- ASP.NET Core Minimal APIs
- .NET 10
- Native AOT Only
- SqlSugar ORM
- MySQL
- SoybeanAdmin
- 后端契约优先
- 模块化单体
- API-first CMS Foundation

---

## 2. 必须优先阅读的项目文档

开始任何任务前，应优先理解以下文档：

```text
docs/context/01-thinkphp-system.md
docs/context/02-next-migration-plan.md
docs/context/03-engineering-delivery.md
docs/context/04-m0-skeleton-validation.md
AGENTS.md
code_review.md
```

说明：

- `docs/context/01-04` 是仓库内稳定兼容入口文件名，供 Codex / AI Agent / 自动化脚本优先读取。
- 当前真实正文仍维护在 `docs/context/` 下的中文命名文档中；`01-04` 入口文件会显式指向对应源文档。

如果这些文件不存在，应提醒用户或创建占位目录，但不得自行臆造旧系统业务细节。

---

## 3. 最高优先级硬约束


### 3.0 工程原则硬约束（OOP / SOLID / Agile / TDD / DoD）

任何新增功能、修复 bug、重构或文档以外的代码变更，无论作者是人还是 AI，**必须**同时满足本节原则。违反任一项的 PR 应直接打回，除非 PR 明确标记为“纯文档变更”且不影响代码、接口、数据库、配置、构建或部署行为。

#### 3.0.1 面向对象编程（OOP / SOLID）

1. **接口先行。** 凡新增有副作用的服务类，必须先定义 `I*` 抽象接口，再写实现。这里的有副作用包括：数据库 IO、文件 IO、网络请求、缓存、邮件、存储、配置加载、进程交互、时间/随机数、加密、任务调度、外部服务调用。
2. **构造函数注入。** 依赖必须通过构造函数传入，禁止在业务类内部 `new` 出有副作用的依赖。值对象、纯函数 helper、小型不可变 options 除外。
3. **构造参数必须是抽象。** 业务模块构造函数参数不得出现具体实现类型；只允许接口、可序列化配置、纯参数对象。
4. **单一职责。** 单类不得同时承担两种以上职责，例如“采集 + 处理 + 输出”、“鉴权 + SQL + 审计”、“文件解析 + 存储 + 业务入库”。出现复合职责时必须拆分为独立类。
5. **不可变模型。** 跨阶段传递的数据模型、请求/响应 DTO、领域快照、迁移映射结果应优先使用 `record`、只读属性或不可变集合。禁止在处理管线中途隐式修改共享状态。
6. **依赖倒置。** 业务模块依赖抽象，不依赖具体基础设施实现。`System/Cms` 模块需要数据库时，应依赖 `WeCms.Shared` 或模块内抽象，由 `WeCms.Persistence` 提供数据访问实现；需要文件、缓存、邮件、时钟、存储等非数据库能力时，应依赖 `WeCms.Shared` 或模块内抽象，由 `WeCms.Infrastructure` 提供实现。
7. **可测试性优先。** 如果某段逻辑难以测试，必须先调整设计。禁止以“难测”为由免测。

典型接口示例：

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        Stream stream,
        StoreFileRequest request,
        CancellationToken cancellationToken);
}

public interface IRefreshTokenHasher
{
    string Hash(string token);
    bool Verify(string token, string hash);
}
```

#### 3.0.2 高内聚低耦合

1. **依赖矩阵。** 推荐依赖方向：

```text
WeCms.Api
  -> WeCms.Modules.System / WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Persistence
  -> WeCms.Shared

WeCms.Modules.System / WeCms.Modules.Cms
  -> WeCms.Shared
  -> 必要时依赖模块内抽象

WeCms.Persistence
  -> WeCms.Shared
  -> WeCms.Modules.System / WeCms.Modules.Cms（仅实现模块暴露的持久化抽象）
  -> SqlSugar ORM / MySqlConnector

WeCms.Infrastructure
  -> WeCms.Shared
  -> 第三方基础设施包

WeCms.Shared
  -> 不得引用其它生产工程
```

2. `WeCms.Shared` 不得引用其它生产工程。
3. `WeCms.Infrastructure` 不得反向引用 `WeCms.Api`、`WeCms.Modules.System`、`WeCms.Modules.Cms`。
4. `WeCms.Persistence` 是适配器层 / 数据访问实现层，不是传统 DAL；只允许实现模块或 `WeCms.Shared` 暴露的持久化抽象，不得承载业务规则、权限判断、审计编排或 HTTP 逻辑。
5. `WeCms.Modules.System` 与 `WeCms.Modules.Cms` 不得互相直接引用内部实现。跨模块交互必须通过 `WeCms.Shared` 契约、显式 Application Service 或 API 边界。
6. 测试工程可以引用被测生产工程；`InternalsVisibleTo` 仅允许暴露给同名或明确对应的 `*.Tests` 工程，禁止暴露给其它生产工程。
7. **单文件 ≤ 600 行。** 超过 600 行的 `.cs`、`.ts`、`.vue` 文件必须拆分。生成文件、迁移 SQL、OpenAPI 产物例外，但不得人工维护超大业务文件。
8. **命名空间与目录一致。** C# 命名空间必须匹配目录结构，Roslyn `IDE0130` 应视为 error。
9. **横切关注点集中。** `Clock`、`IdGenerator`、`PermissionChecker`、`UrlRedactor`、`AuditContext`、`ErrorCodes`、`DiagnosticCode`、`SlugHelper` 等通用能力应沉淀到 `WeCms.Shared` 或 `WeCms.Infrastructure`，不得在业务模块内重复实现。
10. **慎引第三方包。** 新增 NuGet / npm 依赖必须在 PR 描述中说明：为什么不能内置实现、AOT 兼容性、License、维护状态、替代方案。

#### 3.0.3 拒绝隐式兼容与隐藏兜底

1. **边界即合约。** 只在系统边界做校验：HTTP DTO、OpenAPI、配置加载、数据库 migration、旧系统迁移、文件上传、外部服务调用、CLI/脚本入参。内部模块之间通过强类型契约协作，禁止重复防御性兜底掩盖错误。
2. **不符合契约直接抛错。** 参数越界、配置缺失、权限元数据缺失、前置条件不满足时必须 fail-fast，抛出明确异常或返回统一错误码。禁止 `?? defaultValue`、静默 `catch { return null; }`、吞异常、隐式降级。
3. **禁止自动迁移老格式。** API、配置、种子数据、前端路由、权限码、菜单 component key、旧系统迁移格式发生 breaking change 时，必须通过 spec / migration 明确处理。禁止在运行时代码里悄悄兼容旧写法。
4. **禁止 dead-fallback。** “理论上不会发生”的不可测兜底分支应删除或改为明确异常。不可测兜底违反 TDD 可测性原则。
5. **删除即彻底删除。** 移除 API、字段、权限码、菜单 key、命令时必须同步删除代码、测试、文档和前端引用。禁止保留空壳转发、无期限 `[Obsolete]`、`// removed:` 注释或 legacy 分支。
6. **迁移例外。** ThinkPHP 到 WeCMS 的兼容逻辑只能存在于 `database/legacy-migration` 或专用迁移工具中，不得进入运行时业务路径。跨 major release 的迁移期兼容必须有 `docs/specs/<change-id>/` 说明迁移窗口、移除时间和验收标准。

#### 3.0.4 敏捷开发与 Spec 先行

1. **Spec 先行。** 满足任一条件的改动必须先建立 `docs/specs/<change-id>/{spec.md,tasks.md,checklist.md}` 三件套：
   - 预计 diff ≥ 200 行；
   - 新增公共 API / OpenAPI 契约；
   - 新增数据库表或 migration；
   - 新增权限码、菜单、状态机；
   - 修改认证、授权、Token、审计、文件上传、安全策略；
   - 修改前后端契约、OpenAPI、generated 类型；
   - 涉及 AI 二期边界或服务间数据访问规则。
2. **小 PR。** 单个 PR diff 目标 ≤ 400 行。超过必须在 PR 描述写明 `Reason: oversized because ...`，并说明为何不能继续拆分。
3. **可追溯。** PR 描述必须包含 `Closes #<issue>` 或 `Spec: docs/specs/<change-id>/` 二选一。
4. **主干常绿。** `main` / `develop` 必须始终通过质量门禁，不允许“先合再修”。
5. **需求澄清优于猜测。** 模糊需求必须先提出问题或输出澄清清单，禁止“我猜用户想要……”直接实现。
6. **文档同步。** 用户可见行为、后端契约、权限码、错误码、数据库结构、部署方式、安全边界变化时，必须同步更新相关文档。

#### 3.0.5 TDD（测试驱动开发）

1. **Red → Green → Refactor 三步不可省。**
   - Red：先写一个失败测试，确认失败原因与预期一致。
   - Green：写最小实现让测试通过。
   - Refactor：在测试保护下重构命名、结构、重复代码。
2. **Bug 必先复现。** 任何 bugfix 的首个代码 commit 应是能稳定复现 bug 的失败测试，commit message 建议形如 `test: reproduce <bug-id>`。
3. **覆盖率门禁。** 质量门禁应强制行覆盖率 ≥ 80%。禁止通过 `[ExcludeFromCodeCoverage]`、删除断言、降低阈值来绕过门禁。
4. **测试命名。** 后端测试建议命名为 `MethodUnderTest_Should<Behavior>_When<Condition>`。
5. **测试一一对应。** 每个生产类 `Foo.cs` 原则上至少有对应 `FooTests.cs`。复杂边界可拆 `FooEdgeCasesTests.cs` / `FooIntegrationTests.cs`。
6. **测试类型。**
   - 纯业务规则：单元测试。
   - Repository / SQL：集成测试。
   - Minimal API：Endpoint 集成测试。
   - 前端组件：组件测试或页面行为测试。
   - OpenAPI / generated：契约测试。
7. **可测性是设计要求。** 若业务逻辑难以测试，应优先拆分接口、服务、纯函数和依赖注入，而不是免测。

#### 3.0.6 Definition of Done（任务完成硬门槛）

任何 PR 打开评审前，作者必须确认：

```text
[ ] 已遵循 Red → Green → Refactor，或明确标记 N/A：纯文档/纯配置且不影响运行逻辑
[ ] 本地已通过 scripts/quality-gate.sh，或逐项运行等效命令
[ ] 新增有副作用服务类已暴露为 I* 接口并通过构造函数注入
[ ] 改动文件均 ≤ 600 行，命名空间匹配目录
[ ] 跨工程引用未越过依赖矩阵
[ ] 未引入隐式兼容兜底、静默 catch、legacy 分支、dead fallback
[ ] ≥ 200 行或公共契约变更已有 docs/specs/<change-id>/ 三件套
[ ] 用户/开发者/运维/API/数据库文档已同步更新，如涉及
[ ] PR 描述含 Closes # 或 Spec: 链接
[ ] Native AOT publish 已实际运行并通过
[ ] 前端 typecheck / build 已实际运行并通过，如涉及前端
[ ] 未实现一期禁止的 AI runtime 能力
```

推荐质量门禁命令：

```bash
bash scripts/quality-gate.sh
```

在 `scripts/quality-gate.sh` 尚未创建前，必须逐项运行：

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

#### 3.0.7 AI 协作硬指令

当 AI 协作者（Codex / Trae / Claude / Copilot / DeepSeek 辅助审查等）处理本仓库时，必须：

1. 动手前读取 `AGENTS.md`、`code_review.md`、`docs/context/*`。
2. 对 ≥ 200 行变更或公共契约变更走 `docs/specs/<change-id>/` spec 流程，禁止跳过 spec 直接实现。
3. 触碰 `.cs` 生产代码时先遵循 TDD：Red → Green → Refactor。
4. 声称完成前必须实际运行质量门禁或等效命令并查看输出，禁止凭直觉断言成功。
5. 不允许为通过门禁而调低覆盖率阈值、删除测试、绕过 AOT publish、移除安全检查。
6. 禁止以“兼容旧系统”为理由加入运行时 legacy 分支、静默默认值、吞异常或 `[Obsolete]` 转发。旧系统兼容只能在迁移工具中存在。
7. 不确定需求、契约、权限、数据库迁移、安全边界时，必须先提出澄清问题或输出风险清单，等待人工确认。

#### 3.0.8 DB-BOUNDARY 数据库边界硬约束

1. DB-BOUNDARY-001：`WeCms.Persistence` 为唯一允许直接引用数据库/ORM/连接器的项目。
2. DB-BOUNDARY-002：`WeCms.Modules.*` 不得包含 SQL 文本。
3. DB-BOUNDARY-003：`WeCms.Modules.*` 不得直接引用 `SqlSugar ORM`、`MySqlConnector`。
4. DB-BOUNDARY-004：`WeCms.Modules.*` 不得依赖 `WeCms.Persistence` 的具体实现。
5. DB-BOUNDARY-005：`WeCms.Modules.*` 的 Service/UseCase 仅通过抽象（例如 `IUnitOfWork`）控制事务边界，不得直接使用 `DbConnection` 或 `DbTransaction`。
6. DB-BOUNDARY-006：任何突破数据库边界规则的 PR，默认 `BLOCK`。

### 3.1 后端技术硬约束

1. 只允许使用 **ASP.NET Core Minimal APIs**。
2. 只允许使用 **.NET 10**。
3. 只允许 **Native AOT 编译发布**。
4. 必须使用 `WebApplication.CreateSlimBuilder(args)`。
5. 禁止使用 MVC Controller。
6. 禁止使用 Razor / Razor Pages。
7. 禁止运行时 Endpoint 自动扫描。
8. 禁止动态代理 AOP。
9. 禁止 runtime code generation。
10. 禁止在核心业务路径使用 Newtonsoft.Json。
11. 所有请求/响应 DTO 必须纳入 `System.Text.Json` Source Generator。
12. 所有 Endpoint 必须显式注册。
13. 除 `AllowAnonymous` 接口外，所有业务 Endpoint 必须绑定权限码或内部访问策略。
14. 新增 NuGet 包必须经过 AOT 兼容性说明，不能自行随意添加。

### 3.2 数据访问硬约束

1. 使用 SqlSugar ORM。
2. 禁止 EF Core。
3. 禁止 `dynamic 查询/返回`。
4. 禁止 `SELECT *`。
5. 禁止拼接用户输入到 SQL。
6. SQL 必须显式列出字段。
7. Repository 只负责 SQL 和数据映射。
8. Service / UseCase 层负责业务规则和事务边界。
9. 所有 Repository 方法必须支持 `CancellationToken`。
10. 排序字段必须后端白名单映射。
11. 分页参数必须后端校验。
12. 默认最大 `pageSize` 不超过 100。
13. 写操作必须检查 affected rows。
14. 批量操作必须限制最大数量。
15. 默认删除策略为软删除，除非文档明确允许硬删除。

### 3.3 前端硬约束

1. 使用 SoybeanAdmin。
2. 前端一切数据格式以后端为准，不可随意修改。
3. SoybeanAdmin 是 UI 模板，不是 API 契约来源。
4. 后端 DTO / OpenAPI 是 TypeScript 类型来源。
5. `service/generated` 目录禁止手写修改。
6. 前端不得使用 SoybeanAdmin mock 类型作为正式业务契约。
7. `request interceptor` 只处理 token、`code`、`msg`、401、403，不得重塑业务 `data`。
8. 前端不得为了适配 UI 模板修改后端接口字段。
9. 动态菜单只能来自后端菜单 DTO。
10. 按钮权限只能消费后端返回的 `permissions`。
11. 前端隐藏按钮不代表拥有权限；后端仍必须校验。

### 3.4 AI 边界硬约束

1. AI 接入是二期建设，当前一期不得实现运行时 AI 功能。
2. 当前不得创建 `WeCms.Modules.Ai`。
3. 当前不得创建 AI Provider、Prompt 模板、RAG、Vector Store、Agent Tool 运行时代码。
4. 当前不得在 WeCMS 后端调用 DeepSeek / OpenAI / Azure OpenAI / 其他模型 API。
5. 当前不得在 SoybeanAdmin 增加 AI 业务页面。
6. 当前不得把 DeepSeek API Key、OpenAI API Key 或其他 AI Provider Key 写入项目。
7. AI 二期作为独立项目存在，不是 CMS Core 内部模块。
8. AI 二期如果需要 CMS 数据，只允许通过 CMS Core API 获取。
9. AI 二期严禁直接读取、写入、连接 CMS 数据库。
10. AI 二期严禁通过只读副本、数据库视图、binlog、同步表绕过 CMS API。
11. AI 二期严禁直接读取 CMS 文件存储。
12. CMS Core 未来只预留 AI Bridge / AI-facing API 边界，不实现一期 AI runtime。

---

## 4. 统一响应与契约规则

### 4.1 ApiResult

全系统统一响应结构：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

错误响应应包含 `traceId`，例如：

```json
{
  "code": 40001,
  "msg": "参数验证失败",
  "data": null,
  "traceId": "...",
  "fieldErrors": {
    "username": ["用户名不能为空"]
  }
}
```

规则：

1. 全系统只允许一种 `ApiResult<T>` 响应结构。
2. 全系统只允许一种分页结构。
3. 前端不得定义自己的业务错误码。
4. 错误码由后端 `ApiCodes` 统一定义。
5. OpenAPI 是前后端契约交付物。

### 4.2 分页结构

分页结果固定为：

```json
{
  "records": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

禁止前端改为：

```text
list/current/size/count
items/pageNo/limit/totalCount
rows/total
```

---

## 5. 后端项目结构约束

推荐结构：

```text
backend/
  src/
    WeCms.Api/
      Program.cs
      Json/
      Middleware/
      Extensions/

    WeCms.Shared/
      ApiResult.cs
      PagedResult.cs
      ApiCodes.cs
      Permissions.cs
      CurrentUser.cs

    WeCms.Infrastructure/
      Cache/
      Security/
      Storage/
      Mail/
      Jobs/

    WeCms.Persistence/
      Data/
      Migrations/
      Modules/

    WeCms.Modules.System/
      Auth/
      Users/
      Roles/
      Menus/
      Permissions/
      Settings/
      Files/
      Logs/
      Security/

    WeCms.Modules.Cms/
      Channels/
      Articles/
      Media/
      Pages/

  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
```

规则：

1. `WeCms.Api` 只负责 Host、Endpoint 显式注册、中间件、JSON Source Generator 注册。
2. `WeCms.Shared` 放通用 DTO、结果模型、错误码、权限码、基础抽象。
3. `WeCms.Infrastructure` 放缓存、存储、邮件、任务、加密等非数据库基础设施实现。
4. `WeCms.Persistence` 放数据库连接、migration runner、SqlSugar ORM repository、权限检查器等数据访问实现；它是适配器层，不是传统 DAL。
5. `WeCms.Modules.System` 放系统管理模块。
6. `WeCms.Modules.Cms` 放 CMS 内容模块。
7. 不得把业务 SQL 写入 Endpoint。
8. 不得让 Repository 处理 HTTP、权限、审计。
9. 不得跨模块随意复用内部 DTO。

---

## 6. Endpoint 规则

### 6.1 Endpoint 基本要求

1. Endpoint 必须显式注册，不允许运行时扫描自动注册。
2. Endpoint 只负责 HTTP 绑定、参数接收、调用 Service、返回结果。
3. 业务规则必须放在 Service / UseCase 层。
4. SQL 必须放在 Repository 层。
5. Endpoint 输入输出类型必须可被 JSON Source Generator 覆盖。
6. 业务 Endpoint 必须绑定权限码。
7. 匿名接口必须显式标记 `AllowAnonymous` 并说明原因。
8. 写接口必须绑定审计标记。
9. 高风险接口必须绑定限流策略。

### 6.2 权限绑定示例

```csharp
group.MapGet("/system/users", GetUsers)
    .RequirePermission(Permissions.SystemUserList)
    .WithAudit("sys:user:list");
```

---

## 7. 认证与 Token 规则

1. Access Token 有效期短，建议 10~30 分钟。
2. Access Token 不得携带完整权限列表。
3. Refresh Token 必须是高强度随机值。
4. Refresh Token 数据库只保存 hash。
5. Refresh Token 必须支持轮换。
6. Refresh Token 刷新后旧 token 必须失效。
7. 登出必须吊销当前 Refresh Token。
8. 修改密码必须吊销该用户所有 Refresh Token。
9. 禁用用户必须吊销该用户所有 Refresh Token。
10. 修改角色或权限必须更新 `permission_version`。
11. 登录失败必须记录安全事件。
12. 超级管理员高风险登录事件必须重点审计。

---

## 8. 权限、对象级授权与数据权限规则

1. RBAC 只能证明“可执行动作”，不能证明“可操作对象”。
2. 所有带 id 的查询、修改、删除接口必须做对象级授权。
3. 用户不能删除自己。
4. 用户不能禁用自己。
5. 系统必须至少保留一个可登录超级管理员。
6. 非超级管理员不得修改超级管理员。
7. 非超级管理员不得授予自己未拥有的权限。
8. 菜单树不得形成循环。
9. 菜单 component 必须在前端白名单中。
10. 列表、详情、导出、统计接口必须支持 DataScope 过滤。
11. 数据权限必须由后端 `CurrentUserContext` 决定，不信任前端传入的 deptId / tenantId。

---

## 9. 数据库设计规则

所有核心表建议包含：

```text
id
created_at
created_by
updated_at
updated_by
deleted_at
deleted_by
row_version
legacy_id
```

规则：

1. 迁移自旧系统的数据必须尽量保留 `legacy_id`。
2. 软删除字段统一为 `deleted_at`、`deleted_by`。
3. 乐观并发字段统一为 `row_version`。
4. 字段命名使用 `snake_case`。
5. 表命名系统模块使用 `sys_` 前缀，CMS 模块使用 `cms_` 前缀。
6. migration 必须进入版本管理。
7. 禁止手工修改生产数据库结构。
8. 大表加索引必须评估锁表风险。
9. 字段删除必须先 deprecated，再观察，再删除。
10. 旧系统敏感字段默认不迁移。

---

## 10. 文件上传与存储规则

1. 上传文件必须存储在非 WebRoot 私有目录或对象存储。
2. 原始文件名只作为展示字段保存。
3. 存储文件名必须由系统生成。
4. 扩展名必须白名单。
5. MIME 必须白名单。
6. 可执行扩展名一律禁止。
7. 双扩展名必须拒绝。
8. 文件下载必须鉴权。
9. 文件预览必须鉴权。
10. 文件路径不得由前端传入。
11. 禁止把物理路径返回前端。
12. 文件删除默认软删除并记录审计。
13. 富文本附件必须建立引用关系。
14. 删除媒体前必须检查引用关系。

---

## 11. 日志、审计与脱敏规则

1. 所有写操作必须记录审计日志。
2. 所有权限变更必须记录审计日志。
3. 所有角色授权必须记录变更前后差异。
4. 所有用户状态变更必须记录审计日志。
5. 所有登录失败必须记录安全事件。
6. 所有 2FA 绑定、解绑、重置必须记录安全事件。
7. 所有文件上传、下载、删除必须记录审计日志。
8. 所有配置变更必须记录审计日志。
9. 超级管理员操作必须标记 highRisk。
10. 日志禁止记录 password、accessToken、refreshToken、twoFactorSecret、backupCode、SMTP password、数据库连接串。
11. 生产环境默认不记录请求 body 和响应 body。
12. 调试 body 日志必须白名单、限大小、脱敏。

---

## 12. 性能与 SQL 规则

1. 所有列表接口必须分页。
2. 默认 `pageSize` 不超过 20。
3. 最大 `pageSize` 不超过 100。
4. 所有列表查询必须有明确字段列表。
5. 所有列表查询必须有排序。
6. 高频查询必须有索引评估。
7. 禁止 N+1 查询。
8. 所有数据库操作必须支持 `CancellationToken`。
9. 所有 SQL timeout 必须显式配置。
10. 慢 SQL 必须进入性能日志。

---

## 13. OpenAPI 与前端类型生成规则

1. 后端必须生成 OpenAPI。
2. OpenAPI 必须进入 CI 验证。
3. OpenAPI 文件必须作为构建产物。
4. 前端 TypeScript 类型必须由 OpenAPI 生成。
5. OpenAPI diff 发现破坏性变更必须失败。
6. 生产环境 OpenAPI UI 默认关闭或仅管理员可访问。
7. 每个 Endpoint 必须定义响应类型。
8. 每个错误码必须有文档。
9. 每个权限码必须有文档。

---

## 14. SoybeanAdmin 规则

1. `src/service/generated` 禁止手写。
2. `src/service/api` 只封装请求，不重塑业务字段。
3. `src/service/adapters` 只允许用于路由对象、组件绑定层适配，不改变后端 DTO 含义。
4. 页面不得直接使用 mock 类型。
5. 动态路由事实源是后端菜单 DTO。
6. 未知 component key 必须降级到 404 或错误页。
7. 退出登录必须清理 Pinia store、动态路由、tabs、缓存页面。
8. 401 必须进入重新登录流程。
9. 403 必须显示无权限页面。
10. 前端表单校验只是体验，后端校验才是事实。

---

## 15. 旧系统迁移规则

1. 旧系统业务可参考，旧技术架构不继承。
2. `think_admin` 迁移到 `sys_user`。
3. `think_auth_group` 迁移到 `sys_role`。
4. `think_auth_group_access` 迁移到 `sys_user_role`。
5. `think_auth_rule` 拆分为 `sys_menu` 和 `sys_permission`。
6. `think_auth_group.rules` CSV 必须拆成 `sys_role_menu` / `sys_role_permission` 关系表。
7. `think_config` 迁移到 `sys_setting`。
8. `think_file` 迁移到 `sys_file`。
9. `think_log` 迁移到 `sys_login_log`。
10. `think_operate_log` 迁移到 `sys_audit_log`。
11. 旧 token 不迁移。
12. 旧 2FA secret 默认不迁移，迁移后要求重新绑定。
13. 旧 SMTP 密码、auth_key、真实 secret 不迁移。
14. 旧密码 hash 可保留兼容验证，登录成功后升级 hash。
15. 迁移脚本必须输出 row count 和异常数据报告。

---

## 16. AI 二期独立项目边界

### 16.1 一期禁止事项

当前一期禁止：

```text
WeCms.Modules.Ai
WeCms.Infrastructure.Ai
AI Provider
Prompt Template runtime
RAG runtime
Vector Store runtime
Agent Tool runtime
SoybeanAdmin AI 页面
DeepSeek/OpenAI/Azure OpenAI API 调用
AI Key 配置
```

### 16.2 二期 AI 独立项目原则

二期 AI 必须作为独立项目存在，例如：

```text
wecms-core       CMS Core API，拥有 CMS 数据库
wecms-ai         独立 AI Service，拥有 AI 数据库 / 向量库
wecms-admin      SoybeanAdmin
```

硬性规则：

1. CMS Database 只允许 WeCMS Core API 访问。
2. AI Service 不得保存 CMS Database 连接串。
3. AI Service 不得拥有 CMS Database 用户名和密码。
4. AI Service 不得使用 ORM/SQL 读取 CMS 表。
5. AI Service 不得读取 CMS 只读副本、视图、binlog、同步表。
6. AI Service 不得直接写 CMS 数据库。
7. AI Service 不得直接读取 CMS 文件存储。
8. AI Service 需要 CMS 数据时只能调用 CMS Core API。
9. AI Service 写回结果时只能调用 CMS Core API。
10. CMS Core 必须为 AI 提供受控、脱敏、可审计、可限流的 AI-facing API。

---

## 17. 安全红线

禁止向 Codex、DeepSeek 或任何外部模型提交：

```text
生产数据库 dump
真实用户手机号、邮箱、IP
password_hash
accessToken
refreshToken
2FA secret
backup code
SMTP password
JWT signing key
数据库连接串
生产服务器 SSH key
客户私有业务数据
```

允许提交：

```text
脱敏 schema
脱敏示例数据
架构文档
DTO
测试数据
无 secret 的配置模板
脱敏错误日志
SQL migration，不含真实数据
```

---

## 18. Codex 工作流

### 18.1 M0-00 必须先执行

正式编码前，必须先执行只读任务：

```text
M0-00：Codex 项目文档熟悉与开发拆分报告
```

该任务只允许阅读文档和输出报告，禁止修改文件。

### 18.2 编码任务流程

每个编码任务必须遵守：

1. 阅读 `AGENTS.md` 和相关 `docs/context`。
2. 输出执行计划。
3. 列出修改文件。
4. 修改代码。
5. 运行验证命令。
6. 输出验证结果。
7. 列出风险和后续事项。

### 18.3 每次后端变更必须说明

```text
dotnet build -warnaserror
dotnet test
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
```

### 18.4 每次前端变更必须说明

```text
pnpm typecheck
pnpm lint
pnpm build
```

---

## 19. 推荐阶段计划

### M0：工程骨架验证

目标：跑通 .NET AOT + SqlSugar ORM + OpenAPI + SoybeanAdmin 联通。

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
SoybeanAdmin request 封装
CI AOT publish
ThinkPHP migration spike
```

### M1：认证安全闭环

交付：

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

交付：

```text
用户管理
角色管理
菜单管理
权限管理
权限码同步
角色分配权限
用户分配角色
按钮权限
动态路由
```

### M3：系统基础模块

交付：

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

交付：

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

## 20. PR 与验收要求

每个 PR 必须说明：

```text
变更内容
关联任务
是否符合 AOT
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
AOT publish 失败
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

## 21. 最终提醒

Codex 可以帮助实现 WeCMS，但不能替代：

```text
架构决策
安全 Review
权限 Review
数据库迁移审核
AOT 真实发布验证
CI/CD 门禁
人工最终批准
```

以下规则在实现评审中为强制约束（DI 反例）：

- 业务模块必须通过接口 + DI 引入副作用服务，不得 `new`：
  - 数据访问：`IAuthRepository`, `IUserRepository`, `IRoleRepository`
  - 事务：`IUnitOfWork`
  - 时钟/时间：`IClock`
  - ID：`IIdGenerator`
  - 随机：`IRandomProvider`
  - 密码：`IPasswordHasher`, `IRefreshTokenHasher`
  - Token：`ITokenService`, `IRefreshTokenService`（如有分层）
  - 权限：`IPermissionChecker`
  - 文件：`IFileStorage`
  - 邮件：`IEmailSender`
  - 缓存：`ICacheService`
  - 当前用户：`ICurrentUserAccessor`
  - 审计上下文：`IAuditContextAccessor`
  - 外部服务：`IHttpClientFactory`/typed client

- 业务层禁止直接构造以下对象：
  - `new *Repository(...)`、ORM Client（如 `SqlSugarClient`、`MySqlConnection`）
  - `new JwtTokenService(...)`、`new Pbkdf2PasswordHasher(...)`
  - `new FileStorage(...)`、`new SmtpClient(...)`、`new HttpClient(...)`
  - `DateTime.UtcNow`、`Guid.NewGuid()`、`Random.Shared`

- 业务模块构造函数默认仅接收接口、不可变值对象和基础类型；不得使用 Service Locator（`IServiceProvider.GetRequiredService`）获取运行时服务。

任何看似“更快”的实现，只要违反本文件，必须拒绝。



