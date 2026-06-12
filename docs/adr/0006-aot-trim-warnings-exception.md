# ADR-0006：Native AOT / Trim 警告例外管理

## 状态

Accepted

## 背景

P1-001 修复中删除了 `WeCms.Modules.System.csproj` 的 `<NoWarn>IL2026;IL3050</NoWarn>` 以及 Endpoint 文件中的 `#pragma warning disable IL2026, IL3050`，让真实 AOT 警告暴露出来。

经过修复后，WeCMS 自有代码的真实 AOT 警告可被直接追踪；平台级 `MapGet/MapPost` 警告已通过路由绑定方式调整进行清零，不再作为待清零平台例外保留。

## 分析

### 自有代码：已解决

`PermissionEndpointFilter.cs` 原先使用 `Results.Json(value, statusCode:)` 重载（需要 `JsonSerializerOptions`），在 AOT 下不安全。已改为使用 `TypedResults.Json(value, JsonTypeInfo, statusCode:)` 重载，通过注入 `JsonSerializerContext` 获取 `JsonTypeInfo<ApiResult<object?>>`。

### 平台级路由警告：ASP.NET Minimal API `MapGet` 风险警报

`MapGet` 的 `Delegate` 重载在标注了 `RequiresUnreferencedCode` / `RequiresDynamicCode` 的场景下会触发 IL2026/IL3050。我们已将端点注册改为 `RequestDelegate` 重载（将依赖从 `HttpContext.RequestServices` 注入），避免该警告再次出现。

**验证**（当前环境）：
- `dotnet build backend/src/WeCms.Api/WeCms.Api.csproj -warnaserror`：2026-06-10 在当前仓库状态下通过，`SystemEndpoints` 无 IL2026/IL3050 新增。
- `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true`：2026-06-10 在本机通过，产物为 `backend/src/WeCms.Api/bin/Release/net10.0/osx-arm64/publish/`。
- 在 macOS 宿主机上强制 `-r linux-x64` 仍会受 `llvm-objcopy/objcopy` 与 Linux linker 参数限制影响，属于交叉 AOT 工具链限制，不再作为本地质量门禁默认路径。

### 第三方库：Dapper 2.1.66

Dapper 2.1.66 未标记 `IsTrimmable`，导致 Native AOT 编译器报告 assembly-level 警告：

- `IL2104`: Assembly 'Dapper' produced trim warnings
- `IL3053`: Assembly 'Dapper' produced AOT analysis warnings

Dapper 是 WeCMS 数据访问层的核心依赖，不可替换。Dapper.AOT 1.0.31 已提供 source-generated 的 AOT-safe 路径，但 Dapper 核心库本身尚未标记 `IsTrimmable`。

## 决策

1. **自有代码零容忍**：WeCMS 所有项目（`WeCms.Shared`、`WeCms.Infrastructure`、`WeCms.Persistence`、`WeCms.Modules.System`、`WeCms.Modules.Cms`）保持 `IsAotCompatible=true`，不屏蔽 IL2026/IL3050。
2. **平台误报处置闭环**：`MapGet` 调用不再使用 `Delegate` 重载，并保留 `warnaserror` 直接可见性；不依赖局部或项目级抑制。
3. **第三方库例外**：仅在 publish 项目 `WeCms.Api.csproj` 中，对 Dapper assembly 的 IL2104/IL3053 进行针对性抑制。

## 例外详情

### 平台级异常（已清零）

| 项目 | 警告码 | 来源 | 处理结果 | 风险 |
|---|---|---|---|---|
| WeCms.Modules.System | IL2026 / IL3050 | `SystemEndpoints` `MapGet(Delegate)`（已重构前） | 2026-06-10 已清零：改为 `MapGet(RequestDelegate)` + `-warnaserror` 验证通过 | 低（仅历史遗留背景） |

移除条件已满足：平台级 `MapGet` 告警路径已重构为 `RequestDelegate`，未保留抑制。

### 第三方库

| 项目 | 警告码 | 来源 | 原因 | 风险评估 |
|---|---|---|---|---|
| WeCms.Api | IL2104 | Dapper.dll | Dapper 未标记 IsTrimmable | 低 — Dapper.AOT 已提供 source-gen 路径 |
| WeCms.Api | IL3053 | Dapper.dll | Dapper 未标记 IsTrimmable | 低 — Dapper.AOT 已提供 source-gen 路径 |

## 移除条件

### 平台误报

当以下条件满足时可评估关闭该警告：

1. 已完成 `MapGet(RequestDelegate)` 重构。
2. 2026-06-10 执行 `dotnet build backend/src/WeCms.Api/WeCms.Api.csproj -warnaserror` 后，在源代码层面无 IL2026/IL3050 回归。

### 第三方库

当以下任一条件满足时移除此例外：

1. Dapper 发布标记 `IsTrimmable=true` 的新版本。
2. 迁移到完全 AOT-safe 的替代数据访问方案。
3. .NET 10 GA 后重新评估。

## 验证依据

- 本地 `bash scripts/quality-gate-backend.sh` 现默认按宿主 RID 执行 Native AOT publish；在 Apple Silicon/macOS 上对应 `osx-arm64`，已于 2026-06-11 实测通过。
- CI 继续通过 `ubuntu-latest` workflow 的 `linux-x64` 发布步骤进行实机验收；Linux runner 自带/安装 AOT 所需工具链后，`linux-x64` publish 仍为硬门禁。
- WeCMS 自有代码当前不再依赖 `#pragma/NoWarn` 来隐藏端点级 AOT 警告；`MapGet` 已通过 `RequestDelegate` 重载规避 IL2026/IL3050 触发。
- 所有 Endpoint 使用 source-generated JSON serializer context。
- Dapper 调用通过 Dapper.AOT source generator 处理，不依赖运行时反射。

## 持续跟踪机制（ADR-0006 持续有效性）

### 版本基线

- Dapper：`2.1.66`
- Dapper.AOT：`1.0.31`
- 基线定义文件：`scripts/checks/aot-exception-baseline.env`

### 重新评估触发条件（硬规则）

1. `backend/src/WeCms.Persistence/WeCms.Persistence.csproj` 中 `Dapper` 版本变更。
2. `backend/src/WeCms.Persistence/WeCms.Persistence.csproj` 中 `Dapper.AOT` 版本变更。
3. CI 再次出现非自有代码之外的新 IL2104/IL3053/IL2026/IL3050 变化。

### 执行机制

- `.github/workflows/backend-quality-gate.yml` 已新增 `AOT exception baseline check` 步骤，执行 `scripts/checks/check-aot-exception-baseline.sh`。
- `.github/workflows/backend-quality-gate.yml` 与 `scripts/quality-gate-backend.sh` 新增 `check-no-self-aot-suppression` 步骤，执行 `scripts/checks/check-no-self-aot-suppression.sh`；
  该检查会拒绝在自有源码中新增 `IL2026`/`IL3050` 的 `NoWarn`、`#pragma`、`[UnconditionalSuppressMessage]`、`[SuppressMessage]`，并阻断 `DynamicDependency` 等 Trim/AOT 依赖保留属性进入主干，确保告警持续可见。
- 升级前提不满足基线时，脚本会阻断 CI，并提示：
  - 更新 `scripts/checks/aot-exception-baseline.env`；
  - 在 `ADR-0006` 中复核是否仍需保留 IL2104/IL3053 例外。

通过该机制，Dapper/Dapper.AOT 的版本变更会触发 ADR 复审。

## 影响

- `WeCms.Api.csproj` 新增 Dapper assembly 的 IL2104/IL3053 抑制。
- Endpoint 文件不再使用 `[UnconditionalSuppressMessage]`，保持警告可见性。
- `WeCms.Persistence` 是 Dapper/Dapper.AOT/MySQL 的数据访问实现适配器层，不是传统 DAL；业务规则和事务编排仍由模块服务层通过抽象完成。
- 其他项目（Shared、Infrastructure、Persistence、Modules）不受影响，保持零抑制。
- AOT publish 结果受工具链约束影响；待本地/CI 完整环境安装符号剥离工具后复核。
