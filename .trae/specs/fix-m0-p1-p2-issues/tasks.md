# Tasks

## P1 — 数据完整性与安全逻辑

- [x] Task 1: Logout 仅吊销当前 Refresh Token
  - [x] 修改 `AuthEndpoints.LogoutAsync`：从 Header 获取 `X-Refresh-Token`，仅吊销匹配的 token
  - [x] 修改 `AuthService.LogoutAsync`：接收 refreshToken 字符串，按 token_hash 匹配吊销
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 2: Refresh Token 复用检测
  - [x] 修改 `AuthService.RefreshTokenAsync`：检查 `revoked_at IS NOT NULL`，复用则吊销 family + 记录安全事件
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 3: TOTP 重放保护
  - [x] 修改 `TwoFactorEndpoints.VerifyAsync`：检查 `two_factor_last_used_ts`，同一时间步重复拒绝
  - [x] 修改 `AuthService.VerifyTwoFactorAndLoginAsync`：同样添加重放保护
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 4: 文件上传双扩展名检测 + 绝对路径
  - [x] 修改 `FileService.UploadAsync`：检测双扩展名（多于1个 `.` 且倒数第二个不在白名单则拒绝）
  - [x] 修改 `FileService`：使用 `Path.GetFullPath` 确保绝对路径
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 5: 菜单循环检测 + 级联删除
  - [x] 修改 `MenuService.CreateAsync`：新增 parentId 时检查祖先是否形成循环
  - [x] 修改 `MenuService.UpdateAsync`：支持修改 parent_id，同样检查循环
  - [x] 修改 `MenuService.DeleteAsync`：递归收集子孙节点，全部级联软删除
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 6: 最后一个超级管理员保护 + 系统角色保护
  - [x] 修改 `UserService.DeleteAsync`：检查 `activeSuperCount <= 1` 时拒绝
  - [x] 修改 `UserService.SetStatusAsync`：禁用时检查最后一个活跃超管
  - [x] 修改 `RoleService.DeleteAsync`：检查 `is_system` 标志
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 7: 字典类型级联删除 + i18n 软删除
  - [x] 修改 `DictService.DeleteTypeAsync`：同步软删除关联的 `sys_dict_value`
  - [x] 修改 `I18nService.DeleteAsync`：改为 `UPDATE SET deleted_at`
  - [x] 修改 `I18nService.ListAsync`：添加 `WHERE deleted_at IS NULL`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 8: db-check 不泄露错误详情 + LoginAsync 更新登录信息
  - [x] 修改 `SystemEndpoints.db-check`：`catch` 不暴露 `ex.Message`
  - [x] 修改 `AuthService.LoginAsync`：登录成功后更新 `last_login_at` 和 `last_login_ip`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 9: UserService.UpdateAsync 修改角色后更新 permission_version
  - [x] 修改 `UserService.UpdateAsync`：角色变更后递增 `permission_version`（已有此逻辑）
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

## P2 — 代码质量与架构

- [x] Task 10: 多步数据库操作添加事务包裹
  - [x] `UserService.CreateAsync`：user INSERT + user_role INSERT 包裹事务
  - [x] `UserService.UpdateAsync`：user UPDATE + role DELETE/INSERT 包裹事务
  - [x] `RoleService.AssignMenusAsync`：DELETE + INSERT 包裹事务
  - [x] `RoleService.AssignPermissionsAsync`：DELETE + INSERT 包裹事务
  - [x] `AuthService.RefreshTokenAsync`：revoke + insert 包裹事务
  - [x] `AuthManagementEndpoints.ResetPasswordAsync`：token used + password update 包裹事务
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 11: 权限缓存（is_super_admin 放入 JWT claims）
  - [x] 修改 `TokenPrincipal`：新增 `IsSuperAdmin` 字段
  - [x] 修改 `TokenService.GenerateAccessToken`：添加 `is_super_admin` claim
  - [x] 修改 `AuthService.LoginAsync`：传递 `IsSuperAdmin` 到 `TokenPrincipal`
  - [x] 修改 `PermissionEndpointFilter`：优先从 claims 读取 `is_super_admin`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 12: 添加 ICurrentUser 抽象
  - [x] 在 `WeCms.Shared/Contracts/` 创建 `ICurrentUser.cs`
  - [x] 在 `WeCms.Infrastructure/Security/` 创建 `CurrentUserProvider.cs`
  - [x] 在 `ServiceCollectionExtensions` 注册 `ICurrentUser`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 13: 添加 IClock 抽象
  - [x] 在 `WeCms.Shared/Contracts/` 创建 `IClock.cs`
  - [x] 在 `WeCms.Infrastructure/` 创建 `SystemClock.cs`
  - [x] 在 `ServiceCollectionExtensions` 注册 `IClock`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 14: ISecurityEventLogger 改为必需依赖
  - [x] 修改 `AuthService` 构造函数：`ISecurityEventLogger` 不可空
  - [x] 修改 `AuthManagementEndpoints`：`ISecurityEventLogger` 不可空
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 15: 添加 /health/ready 端点
  - [x] 在 `SystemEndpoints` 添加 `GET /health/ready`，检查数据库连接
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 16: Endpoint 内联 SQL 提取
  - [x] 将 `AuthEndpoints.LoginAsync` 中的 login log SQL 移到 `AuthService`
  - [x] 将 `AuthEndpoints.LogoutAsync` 中的 security event SQL 移到 `AuthService` 并简化为通过 Header 获取 RT
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

# Task Dependencies
- Task 11 依赖 Task 12（ICurrentUser 可用于权限缓存）
- Task 16 依赖 Task 14（ISecurityEventLogger 改为必需后可简化）
- Task 1-9（P1）之间无强依赖，可并行
- Task 10-16（P2）之间无强依赖，可并行
