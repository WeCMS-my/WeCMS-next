# Checklist — fix-m0-audit10-medium

## M1 — Permissions.cs 常量
- [x] dict 权限码常量已添加
- [x] file 权限码常量已添加
- [x] setting 权限码常量已添加
- [x] log 权限码常量已添加
- [x] security 权限码常量已添加
- [x] i18n 权限码常量已添加

## M2 — SensitiveKeys
- [x] jwt_signing_key 已加入
- [x] db_password 已加入
- [x] smtp_user 已加入
- [x] sms_secret_key 已加入

## M3 — 排序白名单
- [x] RoleService.ListAsync 排序字段白名单
- [x] LogService 安全注释
- [x] SecurityService 安全注释

## M4 — 合并查询
- [x] GetCurrentUser 重构为可读格式 + MySQL MARS 注释

## M7 — 连接池
- [x] MaxPoolSize=100
- [x] MinPoolSize=0
- [x] ConnectionLifeTime=300
- [x] Pooling=true

## M9 — 安全事件
- [x] ForgotPasswordAsync 记录 security event

## M10 — ConfirmPassword
- [x] ChangePasswordRequest 含 ConfirmPassword 字段
- [x] ChangePasswordAsync 校验一致性

## 全量验证
- [x] dotnet build -warnaserror 通过
- [x] dotnet test 通过
- [x] dotnet publish -c Release -r win-x64 /p:PublishAot=true 通过
- [x] code_review.md 审查通过
