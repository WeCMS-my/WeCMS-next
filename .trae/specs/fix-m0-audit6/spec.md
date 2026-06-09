# Fix Audit Round 6 — 13 Issues

## What Changes
- 种子 admin hash 改为 Pbkdf2PasswordHasher 兼容格式
- 角色权限查询 JOIN sys_role 过滤 status/deleted_at
- 分页 page 参数最小 1
- HashToken 提取到 Shared
- WeCms.Modules.System.csproj 加 TreatWarningsAsErrors
