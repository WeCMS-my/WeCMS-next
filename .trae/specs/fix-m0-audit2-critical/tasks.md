# Tasks

## CRITICAL

- [x] Task 1: 删除 2FA 遗留验证流程
  - [x] 从 `AuthService.VerifyTwoFactorAndLoginAsync` 删除 legacy flow 分支
  - [x] 从 `TwoFactorEndpoints.VerifyAsync` 删除 legacy flow 分支
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 2: 替换所有匿名类型为命名 record
  - [x] 创建 `WeCms.Shared/CommonResponses.cs`：6 个命名 record
  - [x] 修改 10 个 Endpoint 文件使用命名类型替代匿名对象
  - [x] 补全 `WeCmsJsonContext` 注册 21+ 个新类型
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 3: 修复密码重置
  - [x] 删除对 `sys_user.password_reset_token` 的引用
  - [x] 改为从 `sys_password_reset_token` 表读取 token_hash
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 4: 修复超管计数误判
  - [x] `UserService.DeleteAsync`：activeSuperCount 仅对超管检查
  - [x] `UserService.SetStatusAsync`：同上
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 5: 修复并发 Dapper 崩溃
  - [x] `AuthService.GetCurrentUserAsync`：顺序 await 替代 Task.WhenAll
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 6: 补写成功登录日志 + 种子超管用户
  - [x] `AuthService.LoginAsync`：登录成功后 INSERT sys_login_log
  - [x] `000001_base_seed.sql`：添加 admin 用户 INSERT
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 7: 修复文件存储 + MIME + 2FA Ticket 清理
  - [x] `FileService`：注入 IConfiguration，从 Storage:BasePath 读取路径
  - [x] `FileService.IsMimeMatch`：所有扩展名明确 MIME 匹配，_ => false
  - [x] `AuthService`：添加 Timer 清理过期 2FA Ticket
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

## HIGH

- [x] Task 8: Service 暴露 I* 接口 + PagedResult 统一
  - [x] 创建 9 个 I*Service 接口
  - [x] 所有 Endpoint 使用接口类型参数
  - [x] DI 注册改为接口模式
  - [x] Role/File/Log/Security Endpoints 使用 PagedResult<T>
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 9: Setting PUT 脱敏保护 + Forwarded Headers
  - [x] `SettingService.UpdateAsync`：拒绝 `***` 值
  - [x] `Program.cs`：ForwardedHeadersOptions + UseForwardedHeaders
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过
