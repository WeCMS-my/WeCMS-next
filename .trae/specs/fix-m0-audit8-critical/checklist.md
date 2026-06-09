# Checklist — fix-m0-audit8-critical

## C6 — ExceptionMiddleware 安全化
- [x] ExceptionMiddleware 构造函数注入 ILogger<ExceptionMiddleware>
- [x] catch (InvalidOperationException) 日志记录完整 ex.ToString()，客户端返回 "Business error"
- [x] catch (Exception) 日志记录完整 ex.ToString()，客户端返回 "Internal server error"
- [x] 不再暴露 ex.Message 或内部细节给客户端

## C1 + C7 — 种子数据修复
- [x] 种子 SQL 中 admin 用户的 password_hash 为有效 PBKDF2-SHA256 hash（admin@123）
- [x] 种子 SQL 中 admin 用户的 security_stamp 使用 REPLACE(UUID(), '-', '') 生成随机值
- [x] 种子 SQL 中补充 sys:role:assign-menu, sys:role:assign-permission, sys:menu:sort, sys:permission:sync 权限码
- [x] 不再存在 "REPLACE_ME" 占位符

## C3 — sys_file 与 sys_i18n_message migration
- [x] database/migrations/ 中存在 000004_add_file_i18n_tables.sql
- [x] sys_file 表包含所有必需字段（original_name, storage_name, storage_path, size, mime_type, extension, deleted_at 等）
- [x] sys_i18n_message 表包含所有必需字段（locale, message_key, message_value, remark, deleted_at 等）

## C4 — 文件存储相对路径
- [x] FileService.UploadAsync 中 storage_path 存储相对路径而非绝对路径
- [x] FileService.GetDownloadInfoAsync 返回相对路径
- [x] FileEndpoints.DownloadAsync 拼接 BasePath 后使用 Path.GetFullPath，校验结果以 BasePath 开头
- [x] Path.GetFullPath 不在 BasePath 内的请求返回 404

## C2 — 审计日志系统
- [x] WeCms.Shared.Contracts 中有 IAuditWriter 接口
- [x] WeCms.Infrastructure 中有 AuditWriter 实现
- [x] ServiceCollectionExtensions 注册 IAuditWriter 为 Scoped
- [x] UserService 所有写方法调用 IAuditWriter.LogAsync
- [x] RoleService 所有写方法调用 IAuditWriter.LogAsync
- [x] MenuService 所有写方法调用 IAuditWriter.LogAsync
- [x] SettingService.UpdateAsync 调用 IAuditWriter.LogAsync
- [x] DictService 写方法调用 IAuditWriter.LogAsync
- [x] FileService 写方法调用 IAuditWriter.LogAsync
- [x] I18nService 写方法调用 IAuditWriter.LogAsync
- [x] AuthManagementEndpoints 写操作调用 IAuditWriter.LogAsync
- [x] WeCmsJsonContext 注册 AuditLogItem 类型

## C5 — 权限内存缓存
- [x] PermissionEndpointFilter 注入 IMemoryCache
- [x] 缓存键包含 userId、permissionCode、permissionVersion
- [x] 缓存未命中时查 DB，命中时直接返回
- [x] 缓存有滑动过期时间（5分钟）

## C8 — 2FA ticket 数据库化
- [x] migration 000005 创建 sys_two_factor_ticket 表
- [x] AuthService 移除 static ConcurrentDictionary _tickets
- [x] AuthService 移除 static Timer _ticketCleanupTimer
- [x] AuthService 移除 static IClock _s_clock
- [x] LoginAsync 中 ticket 写入 sys_two_factor_ticket 表
- [x] VerifyTwoFactorAndLoginAsync 中从 sys_two_factor_ticket 表查询并删除 ticket
- [x] 过期 ticket 由 expires_at 判定

## 全量验证
- [x] dotnet build -warnaserror 通过
- [x] dotnet test 通过
- [x] dotnet publish -c Release -r win-x64 /p:PublishAot=true 通过
- [x] pnpm typecheck 通过（如涉及前端变更）
- [x] pnpm build 通过（如涉及前端变更）— ⚠️ 预置 pnpm 环境（esbuild scripts 需审批），非代码变更引起
