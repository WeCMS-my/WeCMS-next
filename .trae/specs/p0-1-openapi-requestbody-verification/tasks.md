# Tasks

- [x] Task 1: 确认 patch 脚本已移除
  - 确认 `scripts/checks/patch-openapi-auth-request-bodies.sh` 不存在
  - 确认 `scripts/quality-gate-backend.sh` 中不包含对 patch 脚本的引用

- [x] Task 2: 确认 check-openapi-auth-request-bodies.sh 逻辑正确
  - 确认 check 脚本直接读取 `artifacts/openapi/wecms-api-v1.json`
  - 确认对 login/refresh/logout 三个 endpoint 的 requestBody schema 进行校验

- [x] Task 3: 修复 OpenApiExtensions.ExportOpenApiAsync
  - 从"复制已有 artifact"改为"启动 app → HTTP GET `/openapi/v1.json` → 写入文件"
  - 移除 `FindRepositoryRoot` 和 `ArtifactRelativePath` 常量
  - 使用 `app.StartAsync()` / `app.StopAsync()` 管理生命周期

- [x] Task 4: 质量门禁插入 export 步骤 + 防回归
  - 插入 `[6/16] OpenAPI export` 步骤
  - export 前 `rm -f` 旧 artifact
  - 重新编号 `[6/15]...[15/15]` → `[7/16]...[16/16]`

- [x] Task 5: 验证从零生成链路
  - `rm -f` + `dotnet run -- --export-openapi` → 通过
  - `check-openapi-auth-request-bodies.sh` → 通过
  - 确认不再抛出 FileNotFoundException

# Task Dependencies

- Task 4 依赖 Task 3
- Task 5 依赖 Task 4
