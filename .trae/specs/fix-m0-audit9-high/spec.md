# Fix M0 Audit Round 9 — 7 HIGH Issues Spec

## Why
深度审计发现 7 个 HIGH 优先级问题：密码/禁用后 access token 仍可用、refresh token 无会话上限、角色软删除未级联清理关联表、用户更新无用户名唯一校验、CORS 全开放、菜单后代收集 N+1 查询、菜单排序逐个 UPDATE。本 spec 全部修复。

## What Changes

### HIGH 修复
- H1: 密码修改/用户禁用时吊销所有 access token（JWT 黑名单/版本号方案）
- H2: 限制单用户活跃 refresh token 数量 ≤ 10
- H3: RoleService.DeleteAsync 级联软删除 sys_role_menu 和 sys_role_permission
- H4: UserService.UpdateAsync 用户名唯一性校验
- H5: CORS 收紧为配置化白名单 Origin
- H6: MenuService.CollectDescendants 改为单查询 + 内存递归
- H7: MenuService.SortAsync 改为批量 UPDATE CASE WHEN

## Impact
- Affected specs: fix-m0-audit8-critical, fix-m0-remaining
- Affected code: UserService.cs, RoleService.cs, AuthManagementEndpoints.cs, AuthService.cs, MenuService.cs, Program.cs, appsettings.json, appsettings.Development.json

## ADDED Requirements

### Requirement: H1 — 密码/禁用后 access token 立即失效
系统 SHALL 在密码修改或用户被禁用时，通过递增 `permission_version` 使已签发的 JWT 中的 `permission_version` 与数据库不匹配，配合 Endpoint Filter 校验实现即时失效。

#### Scenario: 修改密码后旧 token 失效
- **WHEN** 用户修改密码后，其 `permission_version` 自动递增
- **THEN** 旧 JWT 中 `permission_version` < 数据库值，下一个请求被拒绝（401）

#### Scenario: 禁用用户后旧 token 失效
- **WHEN** 管理员禁用某用户
- **THEN** 该用户所有已有 JWT 下一请求被拒绝

### Requirement: H2 — 单用户活跃 refresh token ≤ 10
系统 SHALL 在 `StoreRefreshToken` 中检查用户活跃 refresh token 数量，超过 10 个时删除最旧的未吊销 token。

#### Scenario: 超过限制
- **WHEN** 用户已有 10 个活跃 refresh token 后再次登录
- **THEN** 最旧的活跃 token 被自动清理，新 token 正常创建

### Requirement: H3 — 角色删除级联清理关联表
系统 SHALL 在软删除角色时同步清理 `sys_role_menu` 和 `sys_role_permission` 中的相关记录。

#### Scenario: 删除角色清理关联
- **WHEN** 删除一个角色（已分配了菜单和权限）
- **THEN** `sys_role_menu` 和 `sys_role_permission` 中该角色的记录被删除

### Requirement: H4 — 用户更新时校验用户名唯一性
系统 SHALL 在 `UserService.UpdateAsync` 中检查若修改了 username，新值不与已存在用户冲突。

#### Scenario: 用户名冲突被拒绝
- **WHEN** 更新用户 username 为已存在的值
- **THEN** 返回 "Username exists" 错误

### Requirement: H5 — CORS 配置化白名单
系统 SHALL 从配置文件 `Cors:AllowedOrigins` 读取允许的来源列表，生产环境禁止 `AllowAnyOrigin`。

#### Scenario: 开发环境
- **WHEN** `appsettings.Development.json` 配置 `"Cors": { "AllowedOrigins": ["http://localhost:5173"] }`
- **THEN** 仅允许该 Origin 的跨域请求

#### Scenario: 未配置时拒绝
- **WHEN** 未配置 `Cors:AllowedOrigins`
- **THEN** 启动时 fail-fast 抛出配置异常

### Requirement: H6 — 菜单后代收集消除 N+1 查询
系统 SHALL 在 `MenuService.CollectDescendants` 中先一次查询获取所有未删除菜单，再内存构建父子关系进行递归收集，消除 N+1 问题。

#### Scenario: 删除深层菜单
- **WHEN** 删除一个包含 10 层子菜单的父菜单
- **THEN** 仅执行 2 次 SQL（1 次查全量菜单 + 1 次批量软删除）

### Requirement: H7 — 菜单排序使用批量 UPDATE
系统 SHALL 在 `MenuService.SortAsync` 中使用 `CASE WHEN` 单条 SQL 批量更新排序值。

#### Scenario: 批量排序
- **WHEN** 传入 100 个菜单 ID 的排序数组
- **THEN** 仅执行 1 次批量 UPDATE 语句，而非 100 次单独 UPDATE
