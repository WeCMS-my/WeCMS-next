# P0-1: OpenAPI requestBody Patch → Check 验证与修复

## Why

P0-1 原是：Auth 端点（login/refresh/logout）的 OpenAPI requestBody 通过 `patch-openapi-auth-request-bodies.sh` 脚本补丁注入，DTO 与 OpenAPI 合同存在双源头，容易漂移。经 `openapi-auth-requestbody-source-generation` spec 修复后，需验证已从"导出后 patch"修正为"导出后直接检查"模式。

初次验证发现 patch 依赖已修复，但暴露了一个新缺口：质量门禁缺少 OpenAPI export 步骤，直接跳到 check，而 `artifacts/` 目录已被 `.gitignore` 忽略，check 依赖的文件未必存在。

## What Changes

- **验证**：确认 `patch-openapi-auth-request-bodies.sh` 已移除 ✅
- **修复**：`quality-gate-backend.sh` 插入 `[6/16] OpenAPI export` 步骤（`dotnet run -- --export-openapi`），使流程变为明确的 `export → check` 链路
- **验证**：确认 `check-openapi-auth-request-bodies.sh` 能直接通过（说明 requestBody 由源码/framework 自然生成）
- **验证**：export → check 两步联调通过

## Impact

- Affected specs: `openapi-auth-requestbody-source-generation` (原始修复 spec)
- Affected code: `scripts/quality-gate-backend.sh`、`scripts/checks/check-openapi-auth-request-bodies.sh`

## ADDED Requirements

### Requirement: OpenAPI requestBody 由源码自然生成

Auth 端点（login/refresh/logout）的 OpenAPI requestBody 必须由 typed Minimal API delegate + DTO 自然生成，不依赖任何外部 patch 脚本注入。

#### Scenario: 质量门禁通过

- **WHEN** 运行 `bash scripts/quality-gate-backend.sh`
- **THEN** 第 6 步（`OpenAPI export`）通过
- **AND** 第 7 步（`OpenAPI auth request body check`）通过
- **AND** 流程中不存在任何 `patch-openapi-auth-request-bodies.sh` 的调用

#### Scenario: DTO 变更可检测

- **WHEN** 修改 `LoginRequest`、`RefreshRequest` 或 `LogoutRequest` 的字段
- **THEN** `check-openapi-auth-request-bodies.sh` 应检测到 schema 不匹配并报错

### Requirement: 质量门禁包含 OpenAPI export 步骤

质量门禁必须在 check 之前先执行 OpenAPI export，确保 `artifacts/openapi/wecms-api-v1.json` 由最新源码重新生成。

#### Scenario: 从干净状态运行

- **WHEN** `artifacts/openapi/wecms-api-v1.json` 不存在或过期
- **THEN** `[6/16] OpenAPI export` 通过 `dotnet run -- --export-openapi` 重新生成
- **AND** `[7/16] check-openapi-auth-request-bodies.sh` 基于最新导出文件校验

## REMOVED Requirements

### Requirement: patch-openapi-auth-request-bodies.sh

**Reason**：改为 typed delegate 后，requestBody 由源码自然生成，不再需要事后补丁。

**Migration**：已由 `openapi-auth-requestbody-source-generation` spec 完成。
