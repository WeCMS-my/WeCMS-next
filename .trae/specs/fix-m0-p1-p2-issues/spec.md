# Fix M0 P1-P2 Issues Spec

## Why
M0 代码审计发现 13 个 P1 级别问题（数据完整性/安全逻辑）和 11 个 P2 级别问题（代码质量/架构）。这些必须在 M0 阶段修复，以确保系统在进入 M1 之前具备正确的基础安全性和代码质量。

## What Changes

### P1 — 数据完整性与安全逻辑
- Logout 改为仅吊销当前 Refresh Token（非全部）
- Refresh Token 复用检测 + 安全事件记录
- TOTP 重放保护（检查 `two_factor_last_used_ts`）
- 文件上传添加双扩展名检测
- 文件存储路径改为绝对路径/可配置
- 菜单创建添加循环检测
- 菜单删除级联处理子节点
- 删除/禁用用户检查最后一个超级管理员
- 系统角色（is_system=1）不可删除
- 字典类型删除级联软删除字典值
- i18n 改为软删除
- db-check 端点不泄露异常详情
- UserService.UpdateAsync 修改角色后递增 permission_version

### P2 — 代码质量与架构
- 多步数据库操作添加事务包裹
- PermissionEndpointFilter 添加权限缓存（is_super_admin 放入 JWT claims）
- Endpoint 中内联 SQL 提取到 Service/Repository
- 添加 ICurrentUser 抽象
- 添加 IClock 抽象
- ISecurityEventLogger 改为必需依赖
- 添加 GET /health/ready 端点
- 前端 token 存储改用 httpOnly cookie（生产建议）+ 保留 localStorage（开发兼容）
- LoginAsync 更新 last_login_at / last_login_ip

## Impact
- Affected specs: m0-skeleton, fix-m0-p0-issues
- Affected code: AuthService.cs, AuthEndpoints.cs, TwoFactorService.cs, TwoFactorEndpoints.cs, FileService.cs, MenuService.cs, UserService.cs, RoleService.cs, DictService.cs, I18nService.cs, SystemEndpoints.cs, PermissionEndpointFilter.cs, TokenService.cs, ServiceCollectionExtensions.cs, Program.cs, 前端 request/index.ts

## ADDED Requirements

### Requirement: Logout 仅吊销当前 Refresh Token
系统 SHALL 在用户登出时仅吊销当前请求关联的 Refresh Token，而非该用户的所有 Refresh Token。

#### Scenario: 单设备登出
- **WHEN** 用户从设备 A 登出
- **THEN** 仅设备 A 的 Refresh Token 被吊销，设备 B 的会话保持有效

### Requirement: Refresh Token 复用检测
系统 SHALL 在 Refresh Token 被重复使用时检测到复用攻击，记录安全事件，并吊销整个 token family。

#### Scenario: 复用检测
- **WHEN** 一个已被撤销的 Refresh Token 被用于刷新
- **THEN** 该 token family 全部吊销，记录 `token_reuse_detected` 安全事件

### Requirement: TOTP 重放保护
系统 SHALL 在 TOTP 验证时检查 `two_factor_last_used_ts`，同一时间片内的 TOTP 码只能使用一次。

#### Scenario: 重放拦截
- **WHEN** 同一 TOTP 码在 30 秒窗口内第二次使用
- **THEN** 返回验证失败

### Requirement: 文件上传双扩展名检测
系统 SHALL 拒绝包含双扩展名（如 `.php.jpg`）的文件上传。

#### Scenario: 双扩展名拒绝
- **WHEN** 上传文件名为 `shell.php.jpg`
- **THEN** 返回 "File type not allowed" 错误

### Requirement: 文件存储绝对路径
系统 SHALL 使用可配置的绝对路径存储上传文件，不从当前工作目录推导。

#### Scenario: 配置化存储路径
- **WHEN** 配置 `Storage:BasePath=/data/wecms`
- **THEN** 文件存储在 `/data/wecms/files/2026/06/xxx.jpg`

### Requirement: 菜单循环检测
系统 SHALL 在创建/更新菜单时检测是否会形成循环引用，并拒绝该操作。

#### Scenario: 循环检测
- **WHEN** 将菜单 A 的 parent_id 设为其子孙节点
- **THEN** 返回 "Cannot create circular menu reference" 错误

### Requirement: 菜单删除级联
系统 SHALL 在删除菜单时级联软删除所有子孙节点。

#### Scenario: 级联删除
- **WHEN** 删除有子菜单的父菜单
- **THEN** 父菜单及所有子孙菜单被软删除

### Requirement: 最后一个超级管理员保护
系统 SHALL 在删除或禁用超级管理员时检查是否为最后一个可登录的超级管理员，若是则拒绝操作。

#### Scenario: 最后一个管理员保护
- **WHEN** 尝试删除系统中唯一的超级管理员
- **THEN** 返回 "Cannot remove the last super admin" 错误

### Requirement: 系统角色保护
系统 SHALL 拒绝删除 `is_system=1` 的角色。

#### Scenario: 系统角色保护
- **WHEN** 尝试删除 `is_system=1` 的角色
- **THEN** 返回 "Cannot delete system role" 错误

### Requirement: 字典类型级联删除
系统 SHALL 在软删除字典类型时同步软删除关联的字典值。

#### Scenario: 级联软删除
- **WHEN** 删除字典类型
- **THEN** 字典类型和关联字典值同时被软删除

### Requirement: i18n 软删除
系统 SHALL 对 i18n 消息使用软删除（`deleted_at`），而非物理删除。

#### Scenario: 软删除
- **WHEN** 删除 i18n 消息
- **THEN** 设置 `deleted_at` 而非物理删除行

### Requirement: db-check 不泄露错误详情
系统 SHALL 在 db-check 端点失败时返回通用错误消息，不暴露异常详情。

#### Scenario: 错误隐藏
- **WHEN** 数据库连接失败
- **THEN** 返回 "DB connection failed" 而非包含连接字符串的异常消息

### Requirement: 用户角色变更更新 permission_version
系统 SHALL 在通过 UserService 修改用户角色时递增 `permission_version`。

#### Scenario: 版本递增
- **WHEN** 通过 UserService.UpdateAsync 修改用户角色
- **THEN** 用户 `permission_version` 递增

### Requirement: 数据库事务包裹
系统 SHALL 对多步数据库写操作使用事务包裹，确保原子性。

#### Scenario: 事务原子性
- **WHEN** 创建用户时角色分配失败
- **THEN** 用户创建回滚，不产生孤儿数据

### Requirement: 权限缓存
系统 SHALL 在 JWT claims 中包含 `is_super_admin` 标志，PermissionEndpointFilter 优先从 claims 读取，减少数据库查询。

#### Scenario: 缓存命中
- **WHEN** 超级管理员请求受保护端点
- **THEN** 权限检查不查询数据库，直接从 JWT claims 判断

### Requirement: ICurrentUser 抽象
系统 SHALL 提供 `ICurrentUser` 接口和实现，封装从 HttpContext 提取当前用户信息的逻辑。

#### Scenario: 获取当前用户
- **WHEN** Service 注入 `ICurrentUser`
- **THEN** 可通过 `currentUser.UserId`、`currentUser.Username` 等属性获取用户信息

### Requirement: IClock 抽象
系统 SHALL 提供 `IClock` 接口，封装 `DateTime.UtcNow`，使时间依赖可测试。

#### Scenario: 时间注入
- **WHEN** Service 注入 `IClock`
- **THEN** 可通过 `clock.UtcNow` 获取当前时间，测试中可 mock

### Requirement: ISecurityEventLogger 必需依赖
系统 SHALL 将 `ISecurityEventLogger` 作为必需依赖注入，不再使用可空参数。

#### Scenario: 必需注入
- **WHEN** AuthService 被构造
- **THEN** `ISecurityEventLogger` 必须已注册，否则 DI 抛出异常

### Requirement: /health/ready 端点
系统 SHALL 提供 `GET /health/ready` 端点，检查数据库连接是否正常。

#### Scenario: 就绪检查
- **WHEN** 数据库可连接
- **THEN** 返回 200，`{ "status": "ready", "database": "connected" }`

### Requirement: LoginAsync 更新登录信息
系统 SHALL 在登录成功后更新 `sys_user` 的 `last_login_at` 和 `last_login_ip` 字段。

#### Scenario: 登录信息更新
- **WHEN** 用户成功登录
- **THEN** `last_login_at` 和 `last_login_ip` 被更新

## MODIFIED Requirements

### Requirement: TokenService 包含 is_super_admin claim **BREAKING**
`TokenPrincipal` 和 JWT claims 新增 `IsSuperAdmin` 字段。

#### Scenario: JWT claims
- **WHEN** 超级管理员登录
- **THEN** JWT 包含 `is_super_admin: "true"` claim
