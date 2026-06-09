# Checklist

## CRITICAL

- [x] 2FA 遗留流程已删除（只支持 ticket flow）
- [x] 所有 Endpoint 匿名类型已替换为命名 record，注册到 WeCmsJsonContext
- [x] 密码重置使用 `sys_password_reset_token` 表 + token hash
- [x] 超管计数检查仅在目标为超管时触发
- [x] GetCurrentUserAsync 顺序执行（无 Task.WhenAll）
- [x] 成功登录写入 sys_login_log
- [x] 种子数据含可登录超管用户
- [x] 文件存储路径从配置读取
- [x] MIME 验证 `_ => false`，所有扩展名明确匹配
- [x] 2FA Ticket 有过期清理

## HIGH

- [x] 10 个 Service 暴露 I* 接口
- [x] Role/File/Log/Security Endpoints 使用 PagedResult<T>
- [x] Setting PUT 拒绝 `***` 值
- [x] Forwarded Headers Middleware 已配置

## Verification

- [x] `dotnet build backend/WeCms.sln -warnaserror` 通过
- [x] `dotnet test backend/WeCms.sln` 通过
