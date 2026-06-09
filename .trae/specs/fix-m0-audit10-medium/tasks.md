# Tasks

## MEDIUM 修复（7 个可并行）

- [x] Task 1: M1 — Permissions.cs 常量补全
  - 补充 17 个 dict/file/setting/log/security/i18n 权限码常量
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 2: M2 — SensitiveKeys 补充
  - `SettingService.cs` 添加 `jwt_signing_key`, `db_password`, `smtp_user`, `sms_secret_key`
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 3: M3 — 排序字段白名单（Role/Log/Security）
  - `RoleService`: 添加 `SortFields` 静态白名单 + 注释
  - `LogService`: 添加安全注释
  - `SecurityService`: 添加安全注释
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 4: M4 — GetCurrentUser 合并查询
  - `AuthService.GetCurrentUserAsync` 重构为可读多行格式，添加 MySQL MARS 注释
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 5: M7 — DB 连接池显式配置
  - `DbConnectionFactory` 使用 `MySqlConnectionStringBuilder` 配置 Pooling/MaxPoolSize/MinPoolSize/ConnectionLifeTime
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 6: M9 — ForgotPassword 安全事件
  - `ForgotPasswordAsync` 添加 `ISecurityEventLogger` 参数，记录 `password_reset_requested`
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 7: M10 — ConfirmPassword 校验
  - `ChangePasswordRequest` 新增 `ConfirmPassword` 字段
  - `ChangePasswordAsync` 校验 `NewPassword == ConfirmPassword`
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 8: 全量验证 + code_review.md
  - `dotnet build -warnaserror` ✅
  - `dotnet test` ✅
  - `dotnet publish -c Release -r win-x64 /p:PublishAot=true` ✅
  - code_review.md 审查 ✅

# Task Dependencies
- Task 1-7 无相互依赖，可全部并行
- Task 8 依赖 Task 1-7