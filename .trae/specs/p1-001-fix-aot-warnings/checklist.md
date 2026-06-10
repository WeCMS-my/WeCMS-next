# Checklist

- [ ] `WeCms.Modules.System.csproj` 不再包含 `NoWarn` IL2026/IL3050
- [ ] `AuthEndpoints.cs` 不再包含 `#pragma warning disable IL2026, IL3050`
- [ ] `SystemEndpoints.cs` 不再包含 `#pragma warning disable IL2026, IL3050`
- [ ] 全仓 `grep` 确认无残留的 IL2026/IL3050 屏蔽
- [ ] `dotnet build -warnaserror` 通过（如因误报导致失败，ADR 已记录）
- [ ] `dotnet publish -c Release -r linux-x64 /p:PublishAot=true` 已运行
- [ ] AOT publish 输出中的 IL2026/IL3050 警告已分析并分类（真实问题 vs 误报）
- [ ] 如有误报：`docs/adr/0006-aot-trim-warnings-exception.md` 已创建，包含临时例外、移除时间和验证依据
- [ ] 如无误报：AOT publish 零 IL2026/IL3050 警告通过
