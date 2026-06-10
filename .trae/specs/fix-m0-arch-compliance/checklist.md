# Checklist

## P1 — 分页
- [ ] DictService 列表方法有 page/pageSize
- [ ] I18nService 列表方法有 page/pageSize
- [ ] SettingService 列表方法有 page/pageSize
- [ ] 对应 Endpoints 传递分页参数
- [ ] pageSize 默认 20 最大 100

## P2 — 排序白名单
- [ ] LogService 排序白名单
- [ ] SecurityService 排序白名单

## P3 — Affected rows
- [ ] UserService 写操作检查 affected rows
- [ ] RoleService 写操作检查 affected rows
- [ ] MenuService 写操作检查 affected rows
- [ ] 其余 Service UPDATE/DELETE 检查

## 全量验证
- [ ] dotnet build -warnaserror
- [ ] dotnet test
- [ ] dotnet publish /p:PublishAot=true
