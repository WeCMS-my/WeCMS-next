# Fix M0 P0 Issues Spec

## Why
M0 代码审计发现 5 个 P0 级别问题，其中 3 个直接阻断 Native AOT 发布（Dapper.AOT 缺失、JsonSerializerContext 不完整、dynamic 查询），1 个导致限流完全无效，1 个导致 2FA 认证绕过。这些必须在 M0 阶段修复。

## What Changes
- 安装 Dapper.AOT NuGet 包 + 添加 `[module: DapperAot]` 声明
- 补全 `WeCmsJsonContext` 中所有缺失的 DTO 类型
- 修复 `PermissionEndpoints.ListAsync` 的 `dynamic` 查询为强类型
- 添加 `app.UseRateLimiter()` 调用
- 修复 2FA 绕过：启用 2FA 的用户登录时不签发 token，改用 twoFactorTicket 机制

## Impact
- Affected specs: m0-skeleton
- Affected code: `WeCms.Api.csproj`, `WeCms.Infrastructure.csproj`, `WeCms.Modules.System.csproj`, `WeCmsJsonContext.cs`, `Program.cs`, `AuthService.cs`, `AuthEndpoints.cs`, `AuthDtos.cs`, `PermissionEndpoints.cs`, `WeCms.Infrastructure/GlobalUsings.cs`

## ADDED Requirements

### Requirement: Dapper.AOT 编译支持
系统 SHALL 在所有使用 Dapper 的项目中安装 `Dapper.AOT` NuGet 包，并在 Infrastructure 项目中声明 `[module: DapperAot]`，确保 Native AOT 发布时 Dapper 查询可正常执行。

#### Scenario: AOT publish 成功
- **WHEN** 执行 `dotnet publish /p:PublishAot=true`
- **THEN** 发布成功，无 Dapper 相关 IL emit 错误

### Requirement: JsonSerializerContext 完整覆盖
系统 SHALL 在 `WeCmsJsonContext` 中注册所有 Endpoint 使用的请求/响应 DTO 类型，确保 Native AOT 下 JSON 序列化不抛出 `NotSupportedException`。

#### Scenario: 所有 Endpoint 响应正常序列化
- **WHEN** 调用任意已注册的 Endpoint
- **THEN** 响应 JSON 正常序列化，无 `NotSupportedException`

### Requirement: 限流中间件生效
系统 SHALL 在请求管道中调用 `app.UseRateLimiter()`，使 `AddRateLimiter` 中配置的 login（5次/分钟）和 password（3次/分钟）策略实际生效。

#### Scenario: 登录限流触发
- **WHEN** 同一用户在 1 分钟内连续登录失败超过 5 次
- **THEN** 返回 429 Too Many Requests

### Requirement: 2FA 认证安全闭环
系统 SHALL 在用户启用 2FA 时，登录接口不直接签发 Access Token 和 Refresh Token，而是返回一个临时的 `twoFactorTicket`。前端携带 `twoFactorTicket` + TOTP 码调用 `/auth/2fa/verify` 验证通过后，后端才签发真正的 token。

#### Scenario: 2FA 用户登录返回 ticket
- **WHEN** 启用 2FA 的用户使用正确密码登录
- **THEN** 返回 `requiresTwoFactor: true` 和 `twoFactorTicket`，不返回 `accessToken` 和 `refreshToken`

#### Scenario: 2FA 验证通过后签发 token
- **WHEN** 使用有效的 `twoFactorTicket` 和正确的 TOTP 码调用 `/auth/2fa/verify`
- **THEN** 返回 `accessToken` 和 `refreshToken`

### Requirement: 禁止 dynamic 查询
系统 SHALL 在所有 Dapper 查询中使用强类型泛型参数，禁止使用无泛型的 `QueryAsync` 重载。

#### Scenario: PermissionEndpoints 使用强类型查询
- **WHEN** 调用 `GET /api/v1/system/permissions`
- **THEN** 查询使用 `QueryAsync<PermissionItem>` 而非 `QueryAsync`

## MODIFIED Requirements

### Requirement: 登录接口响应结构变更 **BREAKING**
`LoginResponse` 中 `accessToken` 和 `refreshToken` 字段在 `requiresTwoFactor` 为 `true` 时变为 `null`，新增 `twoFactorTicket` 字段。

#### Scenario: 非 2FA 用户登录
- **WHEN** 未启用 2FA 的用户使用正确密码登录
- **THEN** 返回 `accessToken`、`refreshToken`、`expiresIn`，`requiresTwoFactor: false`，`twoFactorTicket: null`

### Requirement: 2FA Verify 接口变更 **BREAKING**
`POST /api/v1/auth/2fa/verify` 新增支持 `twoFactorTicket` 参数，验证通过后签发 token 并返回 `LoginResponse`。

#### Scenario: 2FA 验证通过
- **WHEN** 使用有效的 `twoFactorTicket` 和正确的 TOTP 码调用 verify
- **THEN** 返回完整的 `LoginResponse`（含 accessToken、refreshToken）
