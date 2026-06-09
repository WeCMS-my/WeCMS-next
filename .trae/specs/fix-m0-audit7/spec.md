# Fix Audit Round 7 — 14 Issues

## What Changes
- C1: Logout 400 状态码使用 Results.Json
- C2: 重新生成 OpenAPI spec
- H3: 10 个 Service 注入 IClock
- H4: SetStatusAsync 禁用时吊销 RT
- H5: UpdateAsync status 白名单校验
- H6: 分页 page/pageSize Math.Max
- H7-8: CreateMenuRequest + INSERT 补全字段
- M9: 双扩展名检测限制危险扩展
- M10: X-Forwarded-For 逗号分隔
- M11+M12: Permissions.cs 命名空间修正 + 端点引用
- M13: Menu null parent_id 支持
- L14: ExceptionMiddleware traceId
