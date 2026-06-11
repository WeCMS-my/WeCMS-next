# P0-1: OpenAPI requestBody Patch → Check 验证与修复

## Why

P0-1 原是：Auth 端点（login/refresh/logout）的 OpenAPI requestBody 通过 `patch-openapi-auth-request-bodies.sh` 脚本补丁注入，DTO 与 OpenAPI 合同存在双源头，容易漂移。经 `openapi-auth-requestbody-source-generation` spec 修复后，需验证已从"导出后 patch"修正为"导出后直接检查"模式。

初次验证发现 patch 依赖已修复，但暴露了两个缺口：
1. 质量门禁缺少 OpenAPI export 步骤
2. `ExportOpenApiAsync` 依赖已有的 `artifacts/openapi/wecms-api-v1.json`（复制模式），而非真正生成

## What Changes

- **验证**：确认 `patch-openapi-auth-request-bodies.sh` 已移除 ✅
- **修复**：`OpenApiExtensions.ExportOpenApiAsync` 改为启动 app → HTTP GET `/openapi/v1.json` → 写入 outputPath
- **修复**：`quality-gate-backend.sh` 插入 `[6/16] OpenAPI export` + `rm -f` 防回归
- **验证**：export → check 从零生成链路通过

## Impact

- Affected specs: `openapi-auth-requestbody-source-generation` (原始修复 spec)
- Affected code: `backend/src/WeCms.Api/Extensions/OpenApiExtensions.cs`、`scripts/quality-gate-backend.sh`

## ADDED Requirements

### Requirement: OpenAPI requestBody 由源码自然生成

Auth 端点（login/refresh/logout）的 OpenAPI requestBody 必须由 typed Minimal API delegate + DTO 自然生成，不依赖任何外部 patch 脚本注入。

#### Scenario: 质量门禁通过

- **WHEN** 运行 `bash scripts/quality-gate-backend.sh`
- **THEN** `[6/16] OpenAPI export` 通过（从零生成 artifact）
- **AND** `[7/16] OpenAPI auth request body check` 通过
- **AND** 流程中不存在任何 `patch-openapi-auth-request-bodies.sh` 的调用

#### Scenario: DTO 变更可检测

- **WHEN** 修改 `LoginRequest`、`RefreshRequest` 或 `LogoutRequest` 的字段
- **THEN** `check-openapi-auth-request-bodies.sh` 应检测到 schema 不匹配并报错

### Requirement: OpenAPI export 从零生成

`ExportOpenApiAsync` 必须启动 app、请求 `/openapi/v1.json`、写入文件，不得依赖已有的 artifact 文件。

#### Scenario: 从干净状态运行

- **WHEN** `artifacts/openapi/wecms-api-v1.json` 不存在
- **THEN** `dotnet run -- --export-openapi <path>` 启动 app → 生成 OpenAPI → 写入文件
- **AND** 不抛出 FileNotFoundException

### Requirement: 质量门禁防回归

export 步骤前必须 `rm -f` 旧 artifact，确保每次从零生成。

## REMOVED Requirements

### Requirement: patch-openapi-auth-request-bodies.sh

**Reason**：改为 typed delegate 后，requestBody 由源码自然生成，不再需要事后补丁。

**Migration**：已由 `openapi-auth-requestbody-source-generation` spec 完成。
