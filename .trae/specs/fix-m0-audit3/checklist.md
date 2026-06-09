# Checklist

- [x] 登录失败 HTTP 200（无 401 刷新循环）
- [x] 2FA 端点无遗留流程
- [x] 前端登出发送 X-Refresh-Token
- [x] email/phone 部分更新 COALESCE 保护
- [x] 4 段权限码解析正确
- [x] LogService COUNT 带过滤
- [x] status 白名单验证
- [x] 修改密码吊销 RT
- [x] row_version 假乐观锁已移除
- [x] 2FA 登录写入 log
- [x] Endpoint 注入 I* 接口
- [x] BumpPermissionVersion 过滤已删除用户
- [x] SecurityEventLogger 注入 IClock
- [x] Menu 批量删除
- [x] 缩进统一
- [x] `dotnet build -warnaserror` 通过
- [x] `dotnet test` 通过
