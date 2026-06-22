# Checklist

- [x] Minimal API / JIT / SqlSugar 基线未改变。
- [x] 未引入 MVC Controller / Razor / EF Core。
- [x] 未新增公共 API、权限码、菜单、数据库表或 migration。
- [x] 未修改 frontend 或 generated 类型。
- [x] 未实现 AI runtime。
- [x] AOP 未使用 `.Wait()` / `.Result`。
- [x] Endpoint 审计默认不再由组合根注册 Noop writer。
- [x] Outbox dispatcher 通过 hosted service 启动并使用 scope 解析 scoped 依赖。
- [x] raw SQL/Ado API 限制在数据边界。
- [x] `git diff --check` 通过。
- [ ] `dotnet build backend/WeCms.slnx -warnaserror`（当前容器缺少 dotnet，未能运行）。
- [ ] `dotnet test backend/WeCms.slnx`（当前容器缺少 dotnet，未能运行）。
- [ ] `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false`（当前容器缺少 dotnet，未能运行）。
