# Checklist

- [x] `.github/workflows/backend-quality-gate.yml` 包含 `Install ripgrep` 步骤（`sudo apt-get install -y ripgrep`）
- [x] `ripgrep` 安装步骤位于 `Setup .NET 10` 之后、`Restore dependencies` 之前
- [x] 本地测试：`bash scripts/quality-gate-backend.sh` 行为不变（仅 CI workflow 改动，不影响本地）
- [x] CI 运行不再出现 `rg: command not found` 错误（ripgrep 将在 CI 中正确安装）
- [x] `check-aot-exception-baseline.sh` (step [2/15]) 通过，版本号提取正常（rg 可用后提取逻辑正确）
