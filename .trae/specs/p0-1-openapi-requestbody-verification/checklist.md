# P0-1 Verification Checklist

- [x] `patch-openapi-auth-request-bodies.sh` 在 `scripts/checks/` 目录中不存在
- [x] `quality-gate-backend.sh` 不含对 `patch-openapi-auth-request-bodies.sh` 的调用
- [x] `check-openapi-auth-request-bodies.sh` 存在且只做校验（不做 patch）
- [x] `[6/16] OpenAPI export` 通过 (`dotnet run -- --export-openapi`)
- [x] `[7/16] OpenAPI auth request body check` 通过
- [x] Login、Refresh、Logout 三个端点的 requestBody schema 由 typed delegate + DTO 自然生成
- [x] 不存在 OpenAPI artifact 的事后补丁注入流程
- [x] 质量门禁链路为 `export → check`，非 `直接 check`（无依赖缺失风险）
