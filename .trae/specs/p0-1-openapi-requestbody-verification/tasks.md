# Tasks

- [x] Task 1: 确认 patch 脚本已移除
  - 确认 `scripts/checks/patch-openapi-auth-request-bodies.sh` 不存在
  - 确认 `scripts/quality-gate-backend.sh` 中不包含对 patch 脚本的引用

- [x] Task 2: 运行质量门禁验证
  - 运行 `bash scripts/quality-gate-backend.sh`
  - 确认第 6 步 `[6/15] OpenAPI auth request body check` 通过
  - 确认完整流程中无 patch 步骤

- [x] Task 3: 确认 check-openapi-auth-request-bodies.sh 逻辑正确
  - 确认 check 脚本直接读取 `artifacts/openapi/wecms-api-v1.json`
  - 确认对 login/refresh/logout 三个 endpoint 的 requestBody schema 进行校验
  - 确认 schema 与 DTO（LoginRequest/RefreshRequest/LogoutRequest）字段一致

# Task Dependencies

- Task 2 依赖 Task 1
- Task 3 可与 Task 1 并行
