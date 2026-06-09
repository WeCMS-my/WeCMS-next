# Tasks

## CRITICAL 修复（8 个，按依赖排序）

- [x] Task 1: C6 — ExceptionMiddleware 注入 ILogger 并安全化
  - 添加 `ILogger<ExceptionMiddleware>` 构造函数参数
  - 在 catch 块中记录 `ex.ToString()` 到日志
  - `InvalidOperationException` 客户端返回固定消息 "Business error" 替代 `ex.Message`
  - 通用 Exception 保持 "Internal server error"
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 2: C1 + C7 — 种子数据修复（密码 hash + 随机 stamp）
  - 生成 `admin@123` 的 PBKDF2-SHA256 hash（硬编码有效值）
  - 将 security_stamp 改为 `REPLACE(UUID(), '-', '')`
  - 补充缺失权限码的种子数据（`sys:role:assign-menu`, `sys:role:assign-permission`, `sys:menu:sort`, `sys:permission:sync`）
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 3: C3 — 补充 sys_file 与 sys_i18n_message migration
  - 创建 `database/migrations/000004_add_file_i18n_tables.sql`
  - 包含 `sys_file` 表（id, original_name, storage_name, storage_path, size, mime_type, extension, created_at, updated_at, deleted_at）
  - 包含 `sys_i18n_message` 表（id, locale, message_key, message_value, remark, created_at, updated_at, deleted_at）
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 4: C4 — 文件存储相对路径 + 下载路径校验
  - FileService.UploadAsync：storage_path 改为仅存相对路径（`files/yyyy/MM/{storageName}`）
  - FileService.GetDownloadInfoAsync：返回相对路径
  - FileEndpoints.DownloadAsync：拼接 `Storage:BasePath` + 相对路径，`Path.GetFullPath` 后校验以 `BasePath` 开头
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 5: C2 — 创建 IAuditWriter + AuditWriter，接入所有写操作
  - 在 `WeCms.Shared.Contracts` 新增 `IAuditWriter` 接口
  - 在 `WeCms.Infrastructure` 新增 `AuditWriter` 实现（写入 sys_audit_log）
  - ServiceCollectionExtensions 注册 `IAuditWriter` 为 Scoped
  - UserService：CreateAsync, UpdateAsync, DeleteAsync, SetStatusAsync 注入并调用 AuditWriter
  - RoleService：CreateAsync, UpdateAsync, DeleteAsync, AssignMenusAsync, AssignPermissionsAsync
  - MenuService：CreateAsync, UpdateAsync, DeleteAsync, SortAsync
  - SettingService：UpdateAsync
  - DictService：CreateTypeAsync, CreateValueAsync, DeleteTypeAsync, DeleteValueAsync
  - FileService：UploadAsync, DeleteAsync
  - I18nService：CreateAsync, UpdateAsync, DeleteAsync
  - AuthManagementEndpoints：ChangePasswordAsync, ResetPasswordAsync
  - WeCmsJsonContext：注册 AuditLogItem 类型
  - 验证：`dotnet build -warnaserror && dotnet test` ✅

- [x] Task 6: C5 — 权限过滤器内存缓存
  - PermissionEndpointFilter 注入 `IMemoryCache`
  - 缓存键格式：`perm:{userId}:{permissionCode}:{permissionVersion}`
  - 缓存有效期 5 分钟（滑动过期）
  - 缓存未命中时查 DB 并写入缓存
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 7: C8 — 2FA ticket 数据库存储
  - 创建 migration `database/migrations/000005_add_two_factor_ticket.sql` 包含 `sys_two_factor_ticket` 表
  - AuthService：移除 `static ConcurrentDictionary`、`static Timer`、`static _s_clock`
  - 2FA ticket 的创建、查找、删除改为操作 `sys_two_factor_ticket` 表
  - 过期 ticket 由 DB 定时任务或应用启动清理（`WHERE expires_at < NOW()`）
  - 清理 `static` 残留代码
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 8: 全量验证
  - `dotnet build -warnaserror` ✅
  - `dotnet test` ✅
  - `dotnet publish -c Release -r win-x64 /p:PublishAot=true` ✅
  - `pnpm typecheck` ✅
  - `pnpm build` ⚠️ 预置 pnpm 环境问题（esbuild build scripts 需要审批），非代码变更引起

# Task Dependencies
- Task 2 依赖 Task 1（种子数据修复需先完成，独立无依赖）
- Task 3 独立（纯 SQL migration）
- Task 4 独立
- Task 5 独立（审计日志接入，无其他任务依赖）
- Task 6 独立（权限缓存）
- Task 7 独立（2FA ticket 重构）
- Task 8 依赖 Task 1-7 全部完成

# 并行化建议
Tasks 1-7 可并行执行（无代码层面强依赖），Task 8 最后执行。
