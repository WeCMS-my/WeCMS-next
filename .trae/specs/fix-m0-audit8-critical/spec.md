# Fix M0 Audit Round 8 — 8 CRITICAL Issues Spec

## Why
第三轮深度审计发现 8 个 CRITICAL 问题：种子数据密码占位符导致无法登录、审计日志完全缺失、数据库 migration 缺失表、文件下载路径遍历、权限过滤器每请求查 DB 无缓存、异常中间件泄露详情并静默吞错、超管 security_stamp 硬编码、2FA ticket 静态字典多实例不共享。本 spec 全部修复。

## What Changes

### CRITICAL 修复
- C1: 种子数据超管密码 hash 替换为真实 PBKDF2 hash **BREAKING**
- C2: 创建 `IAuditWriter` + `AuditWriter`，所有写操作接入审计日志
- C3: 补充 `sys_file` 和 `sys_i18n_message` 表 migration
- C4: 文件存储路径改为相对路径，下载时校验在 BasePath 内
- C5: 权限过滤器实现内存缓存，在 permission_version 变化时失效
- C6: ExceptionMiddleware 注入 ILogger，异常详情写日志不返客户端
- C7: 种子数据超管 security_stamp 改为随机值
- C8: 2FA ticket 从静态 ConcurrentDictionary 迁移到数据库存储

## Impact
- Affected specs: m0-skeleton, fix-m0-audit2-critical, fix-m0-audit7, fix-m0-remaining
- Affected code: 000001_base_seed.sql, AuthService.cs, ExceptionMiddleware.cs, FileService.cs, FileEndpoints.cs, PermissionEndpointFilter.cs, WeCmsJsonContext.cs, ServiceCollectionExtensions.cs, Program.cs, 新增 IAuditWriter.cs, AuditWriter.cs, 新增 migration 000004

## ADDED Requirements

### Requirement: C1 — 种子数据超管密码为有效 hash
系统 SHALL 在种子 SQL 中提供可验证的有效 PBKDF2-SHA256 密码 hash，使 admin 用户能正常登录。

#### Scenario: 种子后登录
- **WHEN** 执行种子 SQL 后
- **THEN** 可用 admin/admin@123 成功登录

### Requirement: C2 — 所有写操作记录审计日志
系统 SHALL 提供 `IAuditWriter` 接口和 `AuditWriter` 实现，在 sys_audit_log 表中记录所有写操作（创建、修改、删除、状态变更），包含操作人、模块、动作、IP、结果。

#### Scenario: 创建用户审计
- **WHEN** 管理员创建新用户
- **THEN** `sys_audit_log` 新增一条记录，包含 operator_id、username、module='system'、action='user:create'、status=success

#### Scenario: 删除用户审计
- **WHEN** 管理员删除用户
- **THEN** `sys_audit_log` 新增一条记录，module='system'、action='user:delete'

### Requirement: C3 — sys_file 和 sys_i18n_message 表有 migration
系统 SHALL 通过 migration SQL 创建 `sys_file` 和 `sys_i18n_message` 表，确保全新数据库部署时文件上传和国际化功能可用。

#### Scenario: 全新数据库部署
- **WHEN** 执行所有 migration
- **THEN** `sys_file` 和 `sys_i18n_message` 表存在且结构正确

### Requirement: C4 — 文件存储使用相对路径并校验下载路径
系统 SHALL 在数据库中存储相对于 `Storage:BasePath` 的相对路径，下载时拼接 BasePath 后校验最终路径在 BasePath 目录范围内，防止路径遍历攻击。

#### Scenario: 路径遍历防护
- **WHEN** 数据库中 storage_path 被篡改为 `../../../etc/passwd`
- **THEN** 下载请求返回 404 或拒绝访问，不暴露系统文件

#### Scenario: 正常下载
- **WHEN** 请求下载已上传文件
- **THEN** 返回文件内容，Content-Disposition 正确

### Requirement: C5 — 权限过滤器有内存缓存
系统 SHALL 在 PermissionEndpointFilter 中实现内存权限缓存，缓存键为 (userId, permissionCode, permissionVersion)，在用户 permission_version 变化时自动失效。

#### Scenario: 缓存命中
- **WHEN** 用户第 2 次请求同一权限保护的路由
- **THEN** 不执行 DB 查询，直接从缓存返回结果

#### Scenario: 缓存失效
- **WHEN** 用户角色权限变更导致 permission_version 递增
- **THEN** 缓存对应的条目自动失效

### Requirement: C6 — ExceptionMiddleware 注入 ILogger 且不泄露细节
系统 SHALL 在 ExceptionMiddleware 中注入 `ILogger<ExceptionMiddleware>`，将完整异常信息写入日志，客户端仅返回用户友好的错误消息。

#### Scenario: 通用异常日志
- **WHEN** 未预期的 Exception 发生
- **THEN** 日志记录完整 `ex.ToString()`，客户端收到 "Internal server error"

#### Scenario: 业务异常
- **WHEN** `InvalidOperationException` 发生
- **THEN** 日志记录完整异常，客户端收到截断的安全消息（不暴露 SQL/表名）

### Requirement: C7 — 种子数据 security_stamp 为随机值
系统 SHALL 在种子 SQL 中为超管用户使用随机 security_stamp（`REPLACE(UUID(), '-', '')`）。

#### Scenario: 种子后 stamp
- **WHEN** 执行种子 SQL
- **THEN** admin 的 security_stamp 为 32 位随机 hex 字符串

### Requirement: C8 — 2FA ticket 存储到数据库
系统 SHALL 将 2FA 登录 ticket 从静态 ConcurrentDictionary 迁移到 MySQL 临时表 `sys_two_factor_ticket`，支持多实例部署。

#### Scenario: 多实例 2FA
- **WHEN** 登录请求打到实例 A，2FA 验证请求打到实例 B
- **THEN** 2FA 验证成功（通过共享 MySQL）

#### Scenario: Ticket 过期清理
- **WHEN** ticket 超过 5 分钟
- **THEN** 验证请求返回 "expired"
