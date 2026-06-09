# Fix M0 Audit Round 4 — 15 Issues Spec

## Why
第四轮审计 15 个问题，集中在边界鲁棒性，代码库已近稳定。

## What Changes
- ExceptionMiddleware 恢复 UnauthorizedAccessException → 403
- UserEndpoints 3 方法改用 IUserService
- AuthService 提取 TwoFactorTicketStore 单例
- 其余 MEDIUM/LOW 修复
