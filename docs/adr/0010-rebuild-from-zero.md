# ADR-0010：M0-BE 从 0 重建后端工程

## 状态

Accepted

## 背景

WeCMS Next 当前阶段是 `M0-BE backend-only`。上级计划已经确认旧 ThinkPHP 系统只作为业务、Schema 和权限模型参考；当前仓库中历史后端实现不作为必须保留的基础。

继续在旧实现上修补会把旧 Dapper、Native AOT、临时 Persistence、旧质量门禁和不一致测试带入新的 JIT + SqlSugar 后端底座。

## 决策

1. M0-BE 后端工程按从 0 重建执行。
2. 保留文档、ADR、规则、业务分析和旧系统 reference 材料。
3. 不保留旧后端工程结构、旧 Repository 实现、旧 Dapper / Dapper.AOT 路径、旧 Native AOT 配置、旧 OpenAPI 产物和旧不稳定质量门禁。
4. 后端重新建立 `backend/WeCms.slnx`、API Host、Shared、Infrastructure、Persistence、System/Cms 模块和测试项目。
5. `frontend/**` 不参与 M0-BE。
6. 旧系统只作为 reference；不迁移旧数据，不兼容旧密码、token、session、2FA secret、backup code、SMTP 密码或 auth_key。该决策延续 [ADR-0005](./0005-no-legacy-data-migration-and-frontend-deferred.md)。

## 影响

- 后续任务从干净后端工程骨架开始推进。
- 历史实现只能作为审计参考，不能成为兼容约束。
- 若发现旧文件残留，必须按当前任务范围删除、归档或明确废弃，不能通过 legacy fallback 保留运行时兼容。

## 验收

- M0-BE 计划书和 README 指向本 ADR。
- 仓库不存在需要保留的旧 backend 实现作为当前运行时基础。
- 后续 Codex 任务按计划书逐项重新交付后端底座。
