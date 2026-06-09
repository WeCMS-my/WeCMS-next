# Tasks

- [x] Task 1: CRITICAL #1 登录 HTTP 200
  - [x] AuthEndpoints.cs:18 改回 `Results.Ok(...)` + `ApiCodes.BusinessError`
  - [x] 验证：build 通过

- [x] Task 2: CRITICAL #2 2FA 遗留流程 + 前端登出
  - [x] TwoFactorEndpoints.cs 删除遗留 flow
  - [x] auth.ts apiLogout 添加 X-Refresh-Token header
  - [x] 验证：build 通过

- [x] Task 3: HIGH #3 email/phone COALESCE
  - [x] UserService.cs:36 UPDATE COALESCE 保护
  - [x] 验证：build 通过

- [x] Task 4: HIGH #4 PermissionSync + #6 LogService COUNT
  - [x] PermissionSyncService LastIndexOf 拆分
  - [x] LogService COUNT 带 @S/@M
  - [x] 验证：build 通过

- [x] Task 5: HIGH #5 + MEDIUM #7-#12
  - [x] status 白名单验证
  - [x] 修改密码 RT 吊销
  - [x] row_version 假乐观锁移除
  - [x] 2FA 登录审计日志
  - [x] Endpoint I* 接口注入
  - [x] BumpPermissionVersion 过滤
  - [x] SecurityEventLogger IClock
  - [x] 验证：build + test 通过

- [x] Task 6: LOW #13-#16
  - [x] Menu 批量删除 IN @Ids
  - [x] 缩进统一
  - [x] 验证：build 通过
