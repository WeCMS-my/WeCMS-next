# Fix M0 Audit Round 11 — 6 LOW Issues Spec

## Why
审计发现 6 个 LOW 问题：Tuple 返回值、.gitignore 遗漏、SystemClock 可用 TimeProvider、前端 403 路由复用、前端 refresh 竞态通知。L2 已在 CRITICAL 轮修复。本 spec 修复剩余 5 个。

## What Changes
- L1: `FileService.GetDownloadInfoAsync` Tuple 改 record
- L3: `.gitignore` 补 `**/obj/`
- L4: `SystemClock` 改用 `TimeProvider`（.NET 内置抽象）
- L5: 前端 `/403` 路由组件独立
- L6: 前端 `tryRefreshToken` 失败时通知排队 subscribers

## ADDED Requirements
### L1 — Tuple 改 record
系统 SHALL 在 `FileService` 中用命名 record 替代 `(string Path, string MimeType, string FileName)?` Tuple。

### L3 — .gitignore 忽略 obj
系统 SHALL 在 `.gitignore` 中添加 `**/obj/` 排除编译产物。

### L4 — SystemClock 用 TimeProvider
系统 SHALL 将 `SystemClock` 实现改为封装 `TimeProvider.System`。

### L5 — 403 页面独立
前端 `/403` 路由 SHALL 使用独立组件而非复用 dashboard。

### L6 — refresh 竞态通知
前端 `tryRefreshToken` SHALL 在失败时通知所有排队 subscribers。
