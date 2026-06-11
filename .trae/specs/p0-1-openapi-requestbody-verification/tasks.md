# Tasks

- [x] Task 1: 确认 patch 脚本已移除
  - 确认 `scripts/checks/patch-openapi-auth-request-bodies.sh` 不存在
  - 确认 `scripts/quality-gate-backend.sh` 中不包含对 patch 脚本的引用

- [x] Task 2: 确认 check-openapi-auth-request-bodies.sh 逻辑正确
  - 确认 check 脚本直接读取 `artifacts/openapi/wecms-api-v1.json`
  - 确认对 login/refresh/logout 三个 endpoint 的 requestBody schema 进行校验
  - 确认 schema 与 DTO（LoginRequest/RefreshRequest/LogoutRequest）字段一致

- [x] Task 3: 发现并修复质量门禁缺口 — 缺少 OpenAPI export 步骤
  - 质量门禁第 6 步原本直接 check，但 `artifacts/` 被 `.gitignore`，文件可能不存在
  - 插入 `[6/16] OpenAPI export` 步骤：`dotnet run --project backend/src/WeCms.Api -- --export-openapi "$REPO_ROOT/artifacts/openapi/wecms-api-v1.json" --nologo`
  - 重新编号 `[6/15]...[15/15]` → `[7/16]...[16/16]`
  - 验证 export → check 两步联调通过

- [x] Task 4: 运行质量门禁验证 (export + check 联调)
  - 单独运行 `dotnet run -- --export-openapi` → 通过
  - 单独运行 `check-openapi-auth-request-bodies.sh` → 通过
  - 确认两步可串联执行

# Task Dependencies

- Task 3 依赖 Task 1, Task 2
- Task 4 依赖 Task 3
