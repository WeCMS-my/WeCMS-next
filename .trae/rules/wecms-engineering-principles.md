# WeCMS Trae Engineering Principles

> 建议保存路径：`.trae/rules/wecms-engineering-principles.md`  
> 适用范围：WeCMS Next 一期开发，包括后端、前端、数据库迁移、测试、CI/CD、文档和 AI 协作。  
> 最高优先级：本规则与 `AGENTS.md`、`code_review.md`、`docs/context/*` 同时生效；如有冲突，以项目架构决议和后端契约优先原则为准。

---

## 0. 项目硬边界

任何新增功能、修复 bug、重构或文档以外的代码变更，无论作者是人还是 AI，必须遵守以下硬边界。违反任一项的 PR 评审应直接打回。

### 0.1 技术栈硬约束

- 后端采用 **ASP.NET Core Minimal APIs**。
- 运行目标为 **.NET 10**。
- 发布方式为 **Native AOT Only**。
- 必须使用 `CreateSlimBuilder()`。
- 数据访问采用 **Dapper / Dapper.AOT**。
- 前端采用 **SoybeanAdmin**。
- 前端一切数据格式以后端 DTO / OpenAPI 为准。
- SoybeanAdmin 只是 UI 模板，不是 API 契约来源。

### 0.2 禁止项

- 禁止 MVC Controller。
- 禁止 Razor。
- 禁止 EF Core。
- 禁止运行时 Endpoint 扫描。
- 禁止动态代理 AOP。
- 禁止 runtime code generation。
- 禁止业务路径使用 `dynamic`。
- 禁止 `SELECT *`。
- 禁止拼接用户输入 SQL。
- 禁止为了前端模板修改后端 API 契约。
- 禁止在一期实现运行时 AI 功能。

### 0.3 AI 二期边界

AI 接入作为二期独立项目，当前一期不得实现运行时 AI 能力。

一期禁止：

- 创建 `WeCms.Modules.Ai`。
- 创建 AI Provider。
- 创建 Prompt 模板表。
- 创建 RAG / Vector Store。
- 在 SoybeanAdmin 增加 AI 页面。
- 在 WeCMS Core 后端调用 DeepSeek / OpenAI / 其他模型 API。
- 将任何 AI Provider Key 写入 WeCMS 配置。

二期硬约束预留：

- AI 必须作为独立项目存在。
- AI 项目严禁直接连接、读取、写入 CMS 数据库。
- AI 项目严禁读取 CMS 数据库只读副本、视图、binlog、同步表。
- AI 项目严禁直接读取 CMS 文件存储。
- AI 项目如需 CMS 数据，只能通过 CMS Core API。
- AI 项目写回 AI 结果，也只能通过 CMS Core API。

---

## 1. 面向对象编程（OOP / SOLID）

### 1.1 接口先行

凡新增有副作用的服务类，必须先定义 `I*` 接口，再写实现。

有副作用的依赖包括但不限于：

- 数据库访问。
- 文件 IO。
- 网络请求。
- 邮件发送。
- Token 生成。
- 密码哈希。
- 配置加载。
- 缓存访问。
- 系统时间。
- 审计日志。
- 文件存储。
- 进程交互。
- AI 二期的外部服务调用客户端。

推荐命名示例：

```text
IDbConnectionFactory
IUnitOfWork
IPasswordHasher
ITokenService
IRefreshTokenService
ICurrentUser
IClock
IAuditWriter
IFileStorage
IMailSender
ISettingProvider
IPermissionChecker
ICmsApiClient    # 仅供二期独立 AI 项目通过 API 访问 CMS 时使用
```

禁止：

```csharp
public sealed class UserService
{
    private readonly UserRepository _repo = new(); // 禁止
}
```

允许：

```csharp
public sealed class UserService(
    IUserRepository users,
    IPermissionChecker permissionChecker,
    IAuditWriter auditWriter,
    IClock clock)
{
}
```

### 1.2 构造函数注入

依赖必须通过构造函数传入。

禁止在业务类内部 `new` 出有副作用的依赖：

- Repository。
- HTTP Client。
- File System。
- Cache Client。
- Token Service。
- Password Hasher。
- Mail Sender。
- Storage Client。
- AI Client。

允许内部创建：

- 值对象。
- 纯函数 helper。
- 局部 DTO。
- 不持有外部资源的不可变对象。

### 1.3 单一职责

单类不得同时承担两种以上主要职责。

典型职责边界：

| 层 | 职责 |
|---|---|
| Endpoint | HTTP 路由绑定、请求响应映射 |
| Service / UseCase | 业务规则、事务、权限对象级校验 |
| Repository | SQL 与数据访问 |
| Validator | 边界输入校验 |
| Policy / Guard | 业务不变量、权限策略 |
| Mapper | DTO 与领域模型映射 |
| Middleware / Filter | 横切处理 |
| Job Handler | 后台任务执行 |

禁止单类同时承担：

```text
HTTP 绑定 + SQL
SQL + 业务规则
文件上传 + 权限判断 + 审计写入
菜单构建 + 权限缓存 + 路由适配
```

出现复合职责时必须拆分。

### 1.4 不可变模型

跨阶段传递的数据模型应使用只读模型：

- `record`
- `readonly record struct`
- 只读属性 DTO
- 不可变集合或只读集合

适用对象：

```text
CurrentUser
PermissionSnapshot
MenuTreeItem
RouteInfo
ArticleDraft
FileDescriptor
ApiResult<T>
PagedResult<T>
```

禁止在业务管线中途修改已传递模型。需要变更时创建新对象。

---

## 2. 高内聚低耦合

### 2.1 WeCMS 依赖矩阵

生产项目依赖必须遵守以下方向。

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Modules.System
  -> WeCms.Shared
  -> WeCms.Infrastructure.Abstractions 或明确允许的 Infrastructure 接口层

WeCms.Modules.Cms
  -> WeCms.Shared
  -> WeCms.Infrastructure.Abstractions 或明确允许的 Infrastructure 接口层

WeCms.Infrastructure
  -> WeCms.Shared

WeCms.Shared
  -> 不得引用其它生产项目
```

前端依赖边界：

```text
frontend/soybean-admin
  -> artifacts/openapi/wecms-api-v1.json
  -> frontend/src/service/generated
  -> 不得引用后端源码
```

二期 AI 边界：

```text
wecms-ai
  -> CMS Core API / generated API client
  -> 自有 AI 数据库 / 向量库
  -> 不得引用 WeCms.Infrastructure.Db
  -> 不得引用 CMS Repository
  -> 不得连接 CMS Database
```

禁止：

- `WeCms.Shared` 引用 `WeCms.Infrastructure`。
- `WeCms.Modules.*` 直接跨模块读取其它模块数据库表。
- 前端直接复制后端 DTO 后手写维护。
- AI 项目引用 CMS Core 数据库访问层。
- AI 项目通过 SQL 读取 CMS 表。

### 2.2 单文件 ≤ 600 行

手写 `.cs` 文件超过 600 行必须拆分。

建议上限：

| 文件类型 | 建议上限 |
|---|---:|
| Endpoint 文件 | 400 行 |
| Service 文件 | 500 行 |
| Repository 文件 | 500 行 |
| Middleware 文件 | 300 行 |
| DTO 文件 | 400 行 |
| Test 文件 | 600 行 |

例外：

- 自动生成文件。
- OpenAPI generated 文件。
- 前端 generated 类型。
- 迁移 SQL 文件。

例外文件不得手写修改。

### 2.3 命名空间与目录一致

命名空间必须匹配目录结构。

例如：

```text
backend/src/WeCms.Modules.System/Users/UserService.cs
```

必须使用：

```csharp
namespace WeCms.Modules.System.Users;
```

Roslyn `IDE0130` 应视为 error。

### 2.4 `InternalsVisibleTo` 白名单

`InternalsVisibleTo` 只允许暴露给同名测试工程。

允许：

```csharp
[assembly: InternalsVisibleTo("WeCms.Modules.System.Tests")]
```

禁止：

```csharp
[assembly: InternalsVisibleTo("WeCms.Modules.Cms")]
[assembly: InternalsVisibleTo("WeCms.Api")]
[assembly: InternalsVisibleTo("SomeProductionProject")]
```

### 2.5 横切关注点集中

横切能力必须集中在共享层或基础设施层，不得在业务模块内重复实现。

应集中沉淀：

| 能力 | 建议位置 |
|---|---|
| `ApiResult` / `PagedResult` | `WeCms.Shared` |
| `ApiCodes` | `WeCms.Shared` |
| `Permissions` 常量 | `WeCms.Shared.Security` 或模块内集中常量 |
| `Clock` | `WeCms.Shared` / `Infrastructure` |
| `CurrentUser` | `WeCms.Shared.Security` |
| `AuditWriter` | `WeCms.Infrastructure` |
| `SlugHelper` | `WeCms.Shared` |
| `UrlRedactor` | `WeCms.Shared.Security` |
| `JsonSerializerContext` | `WeCms.Api.Json` |
| 错误码目录 | `docs/api/error-codes.md` |
| 权限矩阵 | `docs/security/permission-matrix.md` |

禁止业务工程内重复实现相同 helper。

### 2.6 慎引第三方包

新增 NuGet / npm 依赖必须在 PR 描述中说明：

```text
Dependency:
Why needed:
Why built-in implementation is insufficient:
AOT impact:
License:
Maintenance status:
Security risk:
Alternative considered:
```

后端新增 NuGet 包必须通过：

```bash
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
```

禁止为了少量 helper 引入大型框架。

---

## 2.5. 拒绝隐式兼容

### 2.5.1 边界即合约

只在系统边界做校验：

- HTTP Request DTO。
- OpenAPI 契约。
- 配置加载。
- 文件上传。
- 数据库 migration 输入。
- ThinkPHP 旧数据迁移。
- 第三方服务返回。
- 前端请求入口。
- AI 二期服务间 API。

内部模块之间相互信任已验证数据，禁止防御性重复校验。

### 2.5.2 不符合契约直接抛错

参数越界、配置缺失、前置条件不满足时必须 fail-fast。

禁止：

```csharp
var pageSize = request.PageSize ?? 20; // 未经边界 DTO 明确定义时禁止
try { ... } catch { return null; }
if (legacyMode) { ... }
if (string.IsNullOrEmpty(requiredConfig)) return default;
```

应改为：

```csharp
throw new AppException(ApiCodes.InvalidConfiguration, "配置缺失：Jwt:SigningKey");
```

### 2.5.3 禁止自动迁移老格式

ThinkPHP 旧格式、旧权限 CSV、旧菜单路径只能在明确的迁移脚本中处理。

运行时业务代码禁止：

```text
if old ThinkPHP format then ...
if legacy rule then ...
if old menu path then ...
```

迁移必须发生在：

```text
database/legacy-migration/
scripts/migration/
artifacts/reports/migration/
```

运行时只读新模型。

### 2.5.4 禁止 dead fallback

为“理论上不会发生”的分支写兜底属于不可测代码，应删除。

禁止：

```csharp
return result ?? new DefaultResult();
```

除非该默认值是业务契约明确要求，并有测试覆盖。

### 2.5.5 删除即彻底删除

移除 API / 字段 / 命令时必须删干净。

禁止：

- `[Obsolete]` 转发。
- 空壳重导出。
- `// removed:` 注释保留旧逻辑。
- 无调用路径的 legacy adapter。
- 为旧前端 mock 保留兼容字段。

### 2.5.6 例外条款

跨大版本迁移期兼容必须有对应 spec：

```text
.trae/specs/<change-id>/
  spec.md
  tasks.md
  checklist.md
```

spec 必须说明：

- 兼容原因。
- 迁移窗口。
- 移除时间点。
- 风险。
- 测试方案。
- 回滚方案。

没有 spec，一律按 fail-fast 和删除即彻底删除处理。

---

## 3. 敏捷开发

### 3.1 Spec 先行

以下变更必须先建 spec：

```text
.trae/specs/<change-id>/
  spec.md
  tasks.md
  checklist.md
```

触发条件：

- 单次变更预计 ≥ 200 行。
- 新增公共 API。
- 修改 API 契约。
- 修改数据库 schema。
- 修改权限模型。
- 修改认证 / Token / 2FA。
- 修改文件上传 / 下载安全策略。
- 修改 SoybeanAdmin 路由生成。
- 修改迁移策略。
- 引入新 NuGet / npm 依赖。
- 引入二期 AI 相关架构边界。
- 跨大版本兼容或 breaking change。

PR 描述必须包含：

```text
Spec: .trae/specs/<change-id>/
```

### 3.2 小 PR

单个 PR diff ≤ 400 行。

超过必须在 PR 描述写：

```text
Reason: oversized because ...
```

并说明：

- 为什么不能拆分。
- 风险控制。
- 测试覆盖。
- 回滚方式。

### 3.3 可追溯

PR 描述必须包含以下二选一：

```text
Closes #<issue>
Spec: .trae/specs/<change-id>/
```

无追溯来源的 PR 不得进入评审。

### 3.4 主干常绿

`main` 分支必须始终通过：

```bash
bash scripts/quality-gate.sh
```

质量门禁至少包含：

```text
dotnet build -warnaserror
dotnet test
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
pnpm typecheck
pnpm lint
pnpm build
OpenAPI generate
format check
coverage check
smoke check
```

不允许“先合再修”。

### 3.5 需求澄清优于猜测

需求不清晰时必须先澄清。

AI 协作者必须使用 Trae 的需求澄清能力或 `AskUserQuestion` 收敛需求。

禁止：

```text
我猜用户想要...
看起来应该...
为了兼容我顺手...
```

### 3.6 文档同步

用户可见行为变化必须同步更新：

```text
docs/user/
docs/api/
docs/database/
docs/security/
docs/ops/
docs/context/
```

维护者契约变化必须同步更新：

```text
AGENTS.md
code_review.md
docs/architecture/
docs/adr/
docs/api/
docs/security/permission-matrix.md
```

前端 API 类型变化必须同步：

```text
artifacts/openapi/wecms-api-v1.json
frontend/soybean-admin/src/service/generated/
```

---

## 4. TDD（测试驱动开发）

### 4.1 Red → Green → Refactor 三步不可省

任何代码逻辑变更必须遵循：

1. **Red**  
   先在测试工程写失败测试，并运行目标测试确认失败。

2. **Green**  
   写最小实现让测试通过。

3. **Refactor**  
   在测试保护下重构、命名优化和去重，所有测试保持绿色。

命令示例：

```bash
dotnet test backend/tests/WeCms.Tests.Unit --filter FullyQualifiedName~UserServiceTests
```

### 4.2 Bug 必先复现

任何 bugfix 的首个 commit 必须是可稳定复现 bug 的失败测试。

commit message 格式：

```text
test: reproduce <bug-id>
```

### 4.3 覆盖率门禁

`scripts/quality-gate.sh` 强制行覆盖率 ≥ 80%。

禁止：

- 降低 `COVERAGE_THRESHOLD`。
- 临时移除测试。
- 使用 `[ExcludeFromCodeCoverage]` 绕过门禁。
- 把核心业务逻辑移动到不可测区域。

覆盖率阈值只能上调，不能为单个 PR 下调。

### 4.4 测试命名

测试命名格式：

```text
MethodUnderTest_Should<Behavior>_When<Condition>
```

示例：

```text
LoginAsync_ShouldReturnToken_WhenPasswordIsValid
CreateUserAsync_ShouldRejectDuplicateUsername_WhenUsernameExists
DeleteRoleAsync_ShouldFail_WhenRoleIsBuiltIn
```

### 4.5 测试一一对应

每个生产类 `Foo.cs` 至少要有对应测试：

```text
FooTests.cs
```

边界场景可拆分：

```text
FooEdgeCasesTests.cs
FooSecurityTests.cs
FooIntegrationTests.cs
```

Endpoint 必须有集成测试。  
Repository 必须有数据库集成测试。  
权限过滤器必须有 401 / 403 / allow 测试。

### 4.6 可测性是设计要求

如果某段逻辑难以测试，先重构使其可测。

禁止以“难测”为理由免测。

常见重构方式：

- 抽接口。
- 注入 `IClock`。
- 注入 `ICurrentUser`。
- 注入 `IPasswordHasher`。
- 注入 `ITokenService`。
- 把静态 helper 改为纯函数或服务。
- 把复杂流程拆成 UseCase。

---

## 5. Definition of Done（任务完成硬门槛）

任何 PR 在打开评审前，作者必须确认：

- [ ] 已遵循 Red → Green → Refactor，或 PR 模板勾选 `⚪ N/A — 本 PR 不涉及代码逻辑`。
- [ ] 本地已通过 `bash scripts/quality-gate.sh`。
- [ ] Native AOT publish 已通过。
- [ ] 新增有副作用的服务类已暴露为 `I*` 接口。
- [ ] 依赖通过构造函数注入。
- [ ] 改动文件均 ≤ 600 行，或有明确拆分理由。
- [ ] 命名空间匹配目录。
- [ ] 跨工程引用未违反依赖矩阵。
- [ ] 未引入 EF Core / MVC Controller / Razor。
- [ ] 未引入 `dynamic` / `SELECT *` / SQL 拼接用户输入。
- [ ] DTO 已加入 `JsonSerializerContext`。
- [ ] Endpoint 已显式注册。
- [ ] 除 AllowAnonymous 外，业务 Endpoint 已绑定权限码。
- [ ] 写操作已记录审计。
- [ ] 列表接口已分页。
- [ ] 未引入隐式兼容兜底。
- [ ] 边界违约均 fail-fast。
- [ ] ≥ 200 行变更已有 `.trae/specs/<change-id>/` 三件套。
- [ ] API 契约变化已更新 OpenAPI。
- [ ] 前端 generated 类型已重新生成。
- [ ] 用户/开发者文档已同步更新。
- [ ] PR 描述含 `Closes #` 或 `Spec:`。
- [ ] AI 一期禁止内容没有被实现。
- [ ] 没有 secret、token、真实生产数据进入代码、日志、测试或提交历史。

---

## 6. AI 协作硬指令

当 AI 协作者，包括 Trae、Claude、Codex、Copilot、DeepSeek 等，处理本仓库时，必须遵守本节。

### 6.1 动手前必须读取

AI 动手前必须读取：

```text
AGENTS.md
code_review.md
.trae/rules/wecms-engineering-principles.md
docs/context/01-thinkphp-system.md
docs/context/02-next-migration-plan.md
docs/context/03-engineering-delivery.md
docs/context/04-m0-skeleton-validation.md
```

### 6.2 ≥ 200 行变更必须走 `/spec`

对 ≥ 200 行变更、新公共 API、数据库 schema、权限、认证、安全、前端契约等变更，必须先走：

```text
/spec
```

并创建：

```text
.trae/specs/<change-id>/
  spec.md
  tasks.md
  checklist.md
```

禁止跳过 spec 直接实现。

### 6.3 触碰 `.cs` 文件必须执行 TDD

触碰 `.cs` 文件时必须执行 TDD 流程：

```text
Red -> Green -> Refactor
```

并在最终回复或 PR 描述中说明：

- 新增了哪些失败测试。
- 如何确认 Red。
- 如何实现 Green。
- 重构了什么。
- 最终测试命令与结果。

### 6.4 完成前必须实际验证

AI 在声称“完成”前必须实际运行：

```bash
bash scripts/quality-gate.sh
```

或运行等效命令组合：

```bash
dotnet build -warnaserror
dotnet test
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

禁止凭直觉断言成功。

如果环境缺少工具链导致无法运行，必须明确说明：

```text
未运行，原因：...
风险：...
需要人工执行：...
```

不得谎称通过。

### 6.5 不得降低门禁

AI 不得为通过门禁而：

- 降低覆盖率阈值。
- 跳过测试。
- 删除测试。
- 注释失败测试。
- 关闭 AOT publish。
- 移除安全扫描。
- 放宽权限检查。
- 删除审计要求。
- 改低 lint / format 标准。

如需调整门禁，必须单独开 spec 走评审。

### 6.6 禁止隐式兼容实现

AI 禁止以“为了兼容”为理由加入：

- 静默 `try/catch`。
- 默认值兜底。
- legacy 分支。
- dead fallback。
- `[Obsolete]` 转发。
- 空壳重导出。
- 前端字段重命名适配。
- 后端为 SoybeanAdmin mock 改格式。

遇到 breaking change 时必须 fail-fast。  
不确定时必须使用 `AskUserQuestion` 确认是否走 spec 流程。

### 6.7 禁止接触敏感数据

AI 不得读取、处理、生成或保存：

- 真实生产数据库 dump。
- 真实用户手机号、邮箱、IP。
- password_hash。
- accessToken。
- refreshToken。
- 2FA secret。
- backup code。
- SMTP password。
- JWT signing key。
- 数据库连接串。
- 生产服务器 SSH key。
- 客户私有业务内容。

只允许处理：

- 脱敏 schema。
- 脱敏样例数据。
- DTO。
- 测试数据。
- 无 secret 的配置模板。
- 脱敏后的错误日志。
- 架构文档。

### 6.8 AI 二期禁止越界

一期开发中，AI 协作者不得：

- 创建运行时 AI 模块。
- 创建 AI Provider。
- 创建 Prompt 模板。
- 创建 RAG。
- 创建 Vector Store。
- 创建 AI 页面。
- 将 DeepSeek / OpenAI API Key 加入项目。
- 让 WeCMS Core 调用模型 API。

允许：

- 写 ADR 说明 AI 二期独立项目边界。
- 写文档说明 AI 只能通过 CMS API 访问数据。
- 在代码层保留不启用、不实现的架构说明，但不得产生运行时功能。

---

## 7. Trae 执行建议

### 7.1 开发第一步

第一步必须是只读理解任务：

```text
M0-00：Codex / Trae 项目文档熟悉与开发拆分报告
```

该任务禁止修改文件，禁止生成代码，只输出：

- 旧系统理解。
- 新架构理解。
- AI 二期边界理解。
- 模块拆分。
- M0/M1/M2/M3/M4/M5 开发计划。
- 第一批可执行任务。
- 禁止事项。

### 7.2 第二步才允许创建工程骨架

通过 M0-00 人工 Review 后，才允许执行：

```text
M0-01：创建 WeCMS Next 工程骨架
```

M0-01 必须通过：

```bash
dotnet build -warnaserror
dotnet test
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
```

### 7.3 小步提交

Trae / Codex 每次任务只允许处理一个明确目标：

```text
正确：
- 添加 ApiResult。
- 添加 ExceptionMiddleware。
- 添加 db-check endpoint。
- 添加 sys_user migration。
- 添加 login endpoint。

错误：
- 一次性实现整个 RBAC。
- 一次性生成全部 CMS。
- 一次性迁移全部旧数据。
```

---

## 8. 最终硬规则

以下规则不可被单个 PR 推翻：

1. Native AOT Only。
2. Minimal APIs Only。
3. Dapper / Dapper.AOT Only。
4. 后端契约优先。
5. SoybeanAdmin 只作为 UI 模板。
6. AI 二期独立项目。
7. AI 严禁直连 CMS 数据库。
8. 所有业务 Endpoint 必须权限化。
9. 所有写操作必须审计。
10. 所有代码逻辑变更必须测试先行。
11. 所有完成声明必须基于实际验证。
