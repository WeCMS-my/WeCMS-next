# P1-001：修复 AOT 警告屏蔽，恢复 Native AOT 可信

## Why

`WeCms.Modules.System.csproj` 通过 `<NoWarn>IL2026;IL3050</NoWarn>` 屏蔽了关键 Native AOT / trim 警告，同时 `AuthEndpoints.cs` 和 `SystemEndpoints.cs` 也使用 `#pragma warning disable IL2026, IL3050` 屏蔽了文件级警告。这导致即使 AOT publish 通过，也不能证明代码真正 AOT-safe，违反 M0-BE 的核心目标：**Native AOT 可信，而不是通过屏蔽警告勉强发布**。

## What Changes

- 删除 `WeCms.Modules.System.csproj` 中的 `<NoWarn>$(NoWarn);IL2026;IL3050</NoWarn>`
- 删除 `AuthEndpoints.cs` 中的 `#pragma warning disable IL2026, IL3050`
- 删除 `SystemEndpoints.cs` 中的 `#pragma warning disable IL2026, IL3050`
- 重新运行 `dotnet publish -c Release -r linux-x64 /p:PublishAot=true`，分析实际警告
- 如确实存在 ASP.NET Minimal API delegate reflection 的误报（已知 .NET 10 preview 问题），写 ADR 明确临时例外、移除时间和验证依据

## Impact

- Affected specs: M0-BE（工程骨架验证）
- Affected code:
  - `backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj` — 删除 NoWarn 行
  - `backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs` — 删除 pragma
  - `backend/src/WeCms.Modules.System/System/SystemEndpoints.cs` — 删除 pragma
  - 如需要：`docs/adr/0006-aot-trim-warnings-exception.md` — 新增 ADR

## REMOVED Requirements

### Requirement: 屏蔽 AOT Trim 警告

**Reason**: 屏蔽 IL2026/IL3050 使 AOT publish 失去可信度，违反 M0-BE 目标。

**Migration**: 直接删除屏蔽，让真实警告暴露出来。对无法消除的误报写 ADR 记录。

## ADDED Requirements

### Requirement: AOT 警告必须可见

系统 SHALL 不在项目文件或源代码中屏蔽 IL2026（trim analysis）和 IL3050（Native AOT）警告。所有 AOT 相关警告必须在构建时可见。

#### Scenario: AOT publish 暴露真实警告

- **WHEN** 运行 `dotnet publish -c Release -r linux-x64 /p:PublishAot=true`
- **THEN** IL2026/IL3050 警告不被屏蔽，在构建输出中可见

#### Scenario: 误报通过 ADR 记录

- **WHEN** 存在已知的 .NET 平台误报（如 Minimal API delegate reflection 在 source generator 覆盖范围内）
- **THEN** 通过 ADR 文档记录临时例外、移除条件和验证依据，而非在代码中屏蔽
