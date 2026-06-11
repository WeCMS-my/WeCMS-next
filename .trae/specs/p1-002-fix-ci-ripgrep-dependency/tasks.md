# Tasks

- [x] Task 1: 在 CI workflow 中安装 ripgrep
  - 文件：`.github/workflows/backend-quality-gate.yml`
  - 变更：在 Setup .NET 10 步骤之后、"Restore dependencies" 步骤之前，添加 "Install ripgrep" 步骤
  - 实现：`sudo apt-get update && sudo apt-get install -y ripgrep`（第 52-53 行）
  - 验证：静态检查通过，步骤位置正确

# Task Dependencies

- Task 1 无依赖，可独立执行
