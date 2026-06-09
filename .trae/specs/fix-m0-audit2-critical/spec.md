# Fix M0 Audit Round 2 — Critical & High Issues Spec

## Why
第二轮代码审计发现 10 个 CRITICAL 问题（安全绕过、AOT 阻断、数据丢失）和 13 个 HIGH 问题。本 spec 集中修复影响系统可用性和安全的最高优先级问题。

## What Changes

### CRITICAL 修复
- 删除 2FA 遗留验证流程（无需密码即可获取 token）**BREAKING**
- 所有 Endpoint 匿名类型替换为命名 record，注册到 JsonSerializerContext
- 修复密码重置引用不存在的列 + hash 存储
- 修复 UserService 中超管计数检查误判普通用户删除/禁用
- 修复 `GetCurrentUserAsync` 并发 Dapper 崩溃
- 补写 `sys_login_log` 成功登录记录
- 修复文件存储路径（配置化绝对路径）
- 补充种子数据超管用户
- 2FA Ticket 改为一分钟后过期自动清理

### HIGH 修复
- MIME 验证 `_ => true` 改为 `_ => false`
- 10 个 Service 暴露 `I*` 接口
- Role/File/Log/Security Endpoints 统一使用 `PagedResult<T>`
- X-Forwarded-For 使用 Forwarded Headers Middleware
- Setting PUT 脱敏保护（拒绝值为 `***` 的输入）

## Impact
- Affected specs: m0-skeleton, fix-m0-p0-issues, fix-m0-p1-p2-issues
- Affected code: AuthService.cs, TwoFactorEndpoints.cs, UserService.cs, AuthManagementEndpoints.cs, 所有 Endpoints, WeCmsJsonContext.cs, FileService.cs, Seed SQL, ServiceCollectionExtensions.cs

## ADDED Requirements

### Requirement: 2FA 仅支持 Ticket 流程
系统 SHALL 仅通过 `twoFactorTicket` 机制完成 2FA 认证，删除允许仅凭用户名+TOTP码登录的遗留流程。

#### Scenario: 遗留流程被拒绝
- **WHEN** 调用 `/auth/2fa/verify` 不带 `twoFactorTicket`
- **THEN** 返回验证失败

### Requirement: 所有 API 响应使用命名类型
系统 SHALL 对所有 Endpoint 响应使用命名 record 类型，并在 `WeCmsJsonContext` 中注册，确保 Native AOT 兼容。

#### Scenario: AOT 兼容
- **WHEN** 执行 `dotnet publish /p:PublishAot=true`
- **THEN** 无 `NotSupportedException` 由匿名类型引起

### Requirement: 密码重置使用 token hash
系统 SHALL 使用 `sys_password_reset_token` 表存储 token 的 SHA256 hash，不使用不存在的 `sys_user.password_reset_token` 列。

#### Scenario: 重置密码
- **WHEN** 使用有效重置 token 调用 `/auth/reset-password`
- **THEN** 密码被更新，token 被标记为已使用

### Requirement: 超管计数仅对超管生效
系统 SHALL 仅在目标用户为超级管理员时才检查 `activeSuperCount`，不对普通用户误判。

#### Scenario: 删除普通用户
- **WHEN** 系统只有 1 个超管，删除普通用户
- **THEN** 操作成功，不抛出 "Cannot remove the last super admin"

### Requirement: 并发查询改为顺序执行
系统 SHALL 在同一 DbConnection 上顺序执行多个查询，不使用 `Task.WhenAll` 并行。

#### Scenario: GetCurrentUser
- **WHEN** 调用 `/auth/me`
- **THEN** 角色和权限查询顺序执行，无 `InvalidOperationException`

### Requirement: 成功登录写入 sys_login_log
系统 SHALL 在登录成功后向 `sys_login_log` 写入记录（login_type='password', status='success'）。

#### Scenario: 登录日志
- **WHEN** 用户成功登录
- **THEN** `sys_login_log` 新增一条 success 记录

### Requirement: 文件存储路径可配置
系统 SHALL 从 `IConfiguration` 读取 `Storage:BasePath`，默认为 `AppContext.BaseDirectory` 下的 `storage` 目录。

#### Scenario: 配置化路径
- **WHEN** 配置 `Storage:BasePath=/data/wecms`
- **THEN** 文件存储在 `/data/wecms/files/2026/06/xxx.jpg`

### Requirement:  种子数据包含超管用户
系统 SHALL 在种子数据中包含一个可登录的超级管理员用户（username=admin, password=admin123）。

#### Scenario: 种子后登录
- **WHEN** 执行种子 SQL 后
- **THEN** 可用 admin/admin123 登录

### Requirement: MIME 验证拒绝未知类型
系统 SHALL 对未明确匹配的扩展名拒绝上传（`_ => false`），不再默认放行。

#### Scenario: 拒绝未知 MIME
- **WHEN** 上传 `.txt` 文件且 MIME 为 `application/x-msdownload`
- **THEN** 返回 "MIME type mismatch"

### Requirement: 敏感配置脱敏保护
系统 SHALL 在 PUT Setting 时拒绝值为 `***` 的输入，防止脱敏值被写回。

#### Scenario: 脱敏保护
- **WHEN** 更新 sensitive key 且 value 为 `***`
- **THEN** 返回 "Cannot update with redacted value"
