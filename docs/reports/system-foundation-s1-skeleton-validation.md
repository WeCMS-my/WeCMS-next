# WeCMS-next 系统基础破坏性升级 S1 骨架验收报告

> 任务来源：`docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md` Sprint 1。  
> 验收日期：2026-06-20。  
> 范围：仅核查新项目骨架、基础引用、空注册扩展、架构测试；不迁移业务逻辑。

## 1. 总体结论

S1「新项目骨架」已达到当前计划定义的骨架完成状态：平台项目、8 个业务模块项目、7 个模块 SqlSugar 适配项目均已创建并纳入 `backend/WeCms.slnx`；业务模块的生产项目引用（ProjectReference）仅为 `WeCms.Shared`；适配项目引用对应业务模块、`WeCms.Data.SqlSugar` 与 `WeCms.Shared`；架构测试已覆盖业务模块不得引用 `.SqlSugar` 适配项目、`.SqlSugar` 适配项目不得横向引用其他 `.SqlSugar` 适配项目、`WeCms.Shared` 不得引用生产项目等约束。

本报告不代表 S2+ 的 EndpointDefinition、业务迁移、Data.SqlSugar 平台能力、AOP、缓存或 EventBus 已完成。

## 2. S1 子任务完成状态

| 子任务 | 状态 | 证据 |
|---|---|---|
| S1-T01 创建平台项目 | 已完成 | `backend/src/WeCms.Data.SqlSugar`、`backend/src/WeCms.Caching`、`backend/src/WeCms.EventBus`、`backend/src/WeCms.Aop` 均存在 `csproj` 与 `AssemblyMarker.cs`，并纳入 `backend/WeCms.slnx`。 |
| S1-T02 创建业务模块项目 | 已完成 | Identity、AccessControl、Organization、Configuration、Audit、Security、FileCenter、Platform 8 个业务模块均存在 `Contracts`、`Endpoints`、`Services`、`Permissions`、`Repositories`、`Records` 目录和空 DI / Endpoint 扩展。 |
| S1-T03 创建模块 SqlSugar 适配项目 | 已完成 | Identity、AccessControl、Organization、Configuration、Audit、Security、FileCenter 7 个 `.SqlSugar` 项目均存在 `Entities`、`Repositories`、`CodeFirst` 目录和空 DI 扩展；Platform 暂不创建 `.SqlSugar` 项目，符合计划说明。 |
| S1-T04 更新依赖矩阵测试 | 已完成 | `LayerDependencyTests` 已包含 allowed reference 矩阵、业务模块禁止引用适配项目、适配项目禁止横向引用其他适配项目、`WeCms.Shared` 无生产项目引用等测试。 |

## 3. 依赖边界核查

当前 S1 骨架依赖目标如下：

- `WeCms.Data.SqlSugar`、`WeCms.Caching`、`WeCms.EventBus` 仅依赖 `WeCms.Shared`。
- `WeCms.Aop` 依赖 `WeCms.Shared`、`WeCms.Caching`、`WeCms.EventBus`。
- `WeCms.Modules.*` 业务模块的生产项目引用（ProjectReference）仅依赖 `WeCms.Shared`；允许为了 Minimal API Endpoint 扩展使用 `Microsoft.AspNetCore.App` FrameworkReference。
- `WeCms.Modules.*.SqlSugar` 仅依赖对应业务模块、`WeCms.Data.SqlSugar`、`WeCms.Shared`。
- 迁移期 `WeCms.Modules.System` 与 `WeCms.Persistence` 仍保留并可继续编译，后续 S9 删除。

## 4. 验证与审计结果

已运行的可用脚本检查：

```text
bash scripts/checks/check-system-foundation-s1-skeleton.sh           PASS
bash scripts/checks/check-no-controller.sh                         PASS
bash scripts/checks/check-layer-dependency.sh                       PASS
bash scripts/checks/check-sqlsugar-boundary.sh                      PASS
git diff --check                                                    PASS
故障注入：删除 Identity AssemblyMarker 后 S1 骨架脚本失败      PASS
```

受当前容器环境限制，以下完整 .NET 验证未能在本地完成：

```text
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj -p:SkipFrontendBuild=true
```

原因：当前容器缺少 `dotnet` 命令。

```text
bash scripts/quality-gate-backend.sh
```

原因：当前容器未配置 `WECMS_TEST_MYSQL_CONNECTION_STRING`，脚本要求 MySQL 集成测试连接串。

## 5. S1 收口判断

S1 仅要求创建项目骨架和依赖测试，不要求迁移业务逻辑。因此当前判断为：

- S1-T01：已完成。
- S1-T02：已完成。
- S1-T03：已完成。
- S1-T04：已完成。

S1 可以进入人工复核与 CI 补跑阶段；CI / 本地具备 .NET SDK 与 MySQL 测试库后，仍需补跑完整后端质量门禁。

## 6. 后续任务边界

下一大任务应从 S2「Minimal API Endpoint 平台」开始，且仍需按单任务串行推进。S2 开始前不得顺带迁移 Identity、AccessControl 或其他业务模块；业务迁移应等待对应 Sprint。
