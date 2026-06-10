# Tasks

- [ ] Task 1: 删除 `WeCms.Modules.System.csproj` 中的 `<NoWarn>$(NoWarn);IL2026;IL3050</NoWarn>`
  - 文件：`backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj` 第 9 行
  - 验证：`grep -r "NoWarn.*IL2026\|NoWarn.*IL3050" backend/` 无结果

- [ ] Task 2: 删除 `AuthEndpoints.cs` 中的 `#pragma warning disable IL2026, IL3050`
  - 文件：`backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs` 第 8-9 行
  - 验证：`grep -r "pragma warning disable IL2026\|pragma warning disable IL3050" backend/` 无结果

- [ ] Task 3: 删除 `SystemEndpoints.cs` 中的 `#pragma warning disable IL2026, IL3050`
  - 文件：`backend/src/WeCms.Modules.System/System/SystemEndpoints.cs` 第 10-11 行
  - 验证：`grep -r "pragma warning disable IL2026\|pragma warning disable IL3050" backend/` 无结果

- [ ] Task 4: 运行 AOT publish 并分析警告
  - 命令：`dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true`
  - 分析输出中的 IL2026/IL3050 警告
  - 判断哪些是真实问题、哪些是已知 .NET 平台误报

- [ ] Task 5: 如存在无法消除的误报，写 ADR 记录临时例外
  - 文件：`docs/adr/0006-aot-trim-warnings-exception.md`
  - 内容：明确哪些警告是误报、为什么安全、移除条件、验证依据
  - 如 AOT publish 零警告通过，则跳过此任务

# Task Dependencies

- Task 2 和 Task 3 可并行执行
- Task 4 依赖 Task 1、Task 2、Task 3 全部完成
- Task 5 依赖 Task 4 的分析结果
