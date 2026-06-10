# Checklist

## P1 — 分页
- [x] DictService 列表方法有 page/pageSize
- [x] I18nService 列表方法有 page/pageSize
- [x] SettingService 列表方法有 page/pageSize
- [x] 对应 Endpoints 传递分页参数
- [x] pageSize 默认 20 最大 100

## P2 — 排序白名单
- [x] LogService 排序白名单
- [x] SecurityService 排序白名单

## P3 — Affected rows
- [x] UserService 写操作检查 affected rows
- [x] RoleService 写操作检查 affected rows
- [x] MenuService 写操作检查 affected rows
- [x] DictService 写操作检查 affected rows
- [x] I18nService 写操作检查 affected rows
- [x] AuthManagementEndpoints 写操作检查 affected rows
- [x] SettingService 跳过（ON DUPLICATE KEY UPDATE）

## 全量验证
- [x] dotnet build -warnaserror
- [x] dotnet test (全部通过)
- [x] dotnet publish /p:PublishAot=true
