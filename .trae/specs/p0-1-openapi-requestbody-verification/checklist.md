# P0-1 Verification Checklist

- [x] `patch-openapi-auth-request-bodies.sh` 在 `scripts/checks/` 目录中不存在
- [x] `quality-gate-backend.sh` 不含对 `patch-openapi-auth-request-bodies.sh` 的调用
- [x] `check-openapi-auth-request-bodies.sh` 存在且只做校验（不做 patch）
- [x] `OpenApiExtensions.ExportOpenApiAsync` 改为启动 app 后请求 `/openapi/v1.json` 写入文件（不再依赖已有 artifact）
- [x] 质量门禁 export 前加 `rm -f` 防回归
- [x] `[6/16] OpenAPI export` 从零生成 artifact 通过
- [x] `[7/16] OpenAPI auth request body check` 通过
- [x] Login、Refresh、Logout 三个端点的 requestBody schema 由 typed delegate + DTO 自然生成
- [x] 不存在 OpenAPI artifact 的事后补丁注入流程
- [x] 质量门禁链路为 `rm old → export → check`（从零生成，无依赖风险）
