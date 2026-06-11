# P1-002：修复 CI 缺少 ripgrep 导致 quality gate [2/15] 失败

## Why

多个 `scripts/checks/` 脚本依赖 `rg`（ripgrep），但 GitHub Actions `ubuntu-latest` runner 默认未安装 ripgrep。导致 Backend Quality Gate 的第 2 步 `check-aot-exception-baseline.sh` 立即失败：

```
/home/runner/work/WeCMS-next/WeCMS-next/scripts/checks/check-aot-exception-baseline.sh: line 39: rg: command not found
Failed to read required package versions from persistence project file.
=== WeCMS M0-BE Backend Quality Gate FAILED ===
```

同一次运行中后续依赖 `rg` 的检查（[3/15]、[8/15]、[9/15]、[13/15]、[15/15]）虽然未暴露（因为 gate 在 [2/15] 就中止了），但它们同样会失败。

## What Changes

- 在 `.github/workflows/backend-quality-gate.yml` 的 CI workflow 中安装 ripgrep
- 方案对比：
  - **方案 A（推荐）**：CI 中安装 ripgrep（`sudo apt-get install ripgrep`），脚本无需任何修改。ripgrep 是 `ubuntu-latest` 官方仓库可用包，安装简单且后续所有脚本均受益。
  - 方案 B：将脚本中的 `rg` 调用逐一替换为 `grep`/`sed`。但部分脚本使用了 ripgrep 独有特性（`--pcre2`、`--glob`、`-g` 多文件过滤），替换工程量大且容易引入 bug，不如方案 A 稳健。

选择方案 A。

## Impact

- Affected specs: M0-BE（工程骨架验证）
- Affected code:
  - `.github/workflows/backend-quality-gate.yml` — 安装 ripgrep 步骤
- Not affected:
  - `scripts/checks/*.sh` — 不修改，ripgrep 依赖保持

## ADDED Requirements

### Requirement: CI runner 必须安装 ripgrep

Backend Quality Gate CI workflow SHALL 在运行质量门禁脚本之前安装 ripgrep。

#### Scenario: CI quality gate 成功运行到终点

- **GIVEN** GitHub Actions `ubuntu-latest` runner
- **WHEN** 触发 Backend Quality Gate workflow
- **THEN** ripgrep 在步骤中安装，`rg` 命令可被所有 check 脚本使用
- **AND** quality gate 15 个步骤均能正常执行（不再因 `rg: command not found` 失败）

#### Scenario: 本地开发环境不受影响

- **GIVEN** 开发者本地已安装 ripgrep
- **WHEN** 本地运行 `bash scripts/quality-gate-backend.sh`
- **THEN** 行为不变，无需修改任何本地配置

## Impacted Scripts（依赖 `rg` 的完整列表）

| 脚本 | rg 用法 | 受影响步骤 |
|------|---------|-----------|
| `check-aot-exception-baseline.sh` | `rg -o` 提取版本号 | [2/15] |
| `check-no-self-aot-suppression.sh` | `rg -n -g` 搜索 `.cs`/`.csproj` | [3/15] |
| `check-no-select-star.sh` | `rg -i -n --glob` 搜索 SQL | [8/15] |
| `check-no-dynamic-query.sh` | `rg --pcre2 -n --glob` 搜索 C# | [9/15] |
| `check-json-context-coverage.sh` | `rg --pcre2 -o` + `rg -o` 分析 JSON context | [13/15] |
| `check-code-review.sh` | `rg -n -g --glob` 搜索 `.cs` | [15/15] |
| `review-di.sh` | `rg -n` 搜索 DI 违规 | DI scan |
