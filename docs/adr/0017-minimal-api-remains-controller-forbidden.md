# ADR-0017：继续 Minimal API 并禁止 Controller Web API

## 状态

Accepted

## 背景

WeCMS Next 当前进入系统基础破坏性升级准备阶段。升级会拆分系统基础模块、调整持久化边界，并引入 EndpointDefinition、Swagger/Scalar 等后台基础设施，但这不意味着 HTTP API 架构切换到 MVC Controller。

现有 Minimal API 路线已经服务于以下治理目标：

- Endpoint 显式注册，便于架构测试和质量门禁扫描。
- 权限 metadata 与 Endpoint Filter 可以直接绑定到业务 Endpoint。
- OpenAPI 覆盖可以从 Endpoint 元数据生成和验证。
- Endpoint Handler 保持薄层，业务逻辑进入 Application Service。

## 决策

1. 后端 HTTP API 继续使用 ASP.NET Core Minimal API。
2. 继续使用 `MapGroup`、`MapGet`、`MapPost`、`MapPut`、`MapDelete` 显式注册 Endpoint。
3. 允许后续引入 EndpointDefinition、Endpoint Convention、Endpoint Filter、Endpoint Metadata、Swagger/Scalar。
4. 禁止 Controller。
5. 禁止 ControllerBase。
6. 禁止 AddControllers。
7. 禁止 MapControllers。
8. 禁止 `[ApiController]`。
9. 禁止 MVC Controller Attribute Routing。
10. 禁止用 MVC Action Filter 作为业务 API 主入口。

## 影响

### 正向影响

- 保持后端契约、权限和审计门禁的可扫描性。
- 避免系统基础升级期间混入两套 HTTP API 编程模型。
- 让后续模块拆分仍能通过统一的 Minimal API Endpoint metadata 治理。

### 代价

- 不能直接套用 Controller Web API 模板或代码生成方案。
- Swagger/Scalar、OpenAPI 扩展和过滤器必须基于 Minimal API 元数据实现。
- 迁移旧 Endpoint 时需要显式维护路由、权限、审计和 OpenAPI 元数据。

## 验收

架构测试和质量门禁必须扫描生产代码并阻断以下内容：

```text
: ControllerBase
: Controller
AddControllers(
MapControllers(
[ApiController]
```

后续新增 Endpoint 必须继续满足：

- 显式 Minimal API 注册。
- 业务 Endpoint 绑定权限码或显式 `AllowAnonymous` / `InternalOnly` 策略。
- 写操作绑定审计 metadata。
- 高风险写操作绑定限流策略。
- Endpoint Handler 只做 HTTP 绑定和返回。

## 关联

- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
