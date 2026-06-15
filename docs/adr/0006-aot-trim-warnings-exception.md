# ADR-0006：Native AOT / Trim 警告例外管理

## 状态

Superseded by `ADR-0009`.

## 说明

本文件保留为历史记录。

它描述的是 WeCMS 仍以 Native AOT 作为强制运行时基线时的告警例外治理方式。自 `ADR-0009：运行时基线从 Native AOT 切换为 JIT` 生效后，本文件不再作为现行规则、合并门禁或发布基线。

## 当前结论

1. WeCMS 当前运行时基线为 `.NET 10 JIT publish/runtime`。
2. 本文件中的 `PublishAot`、`IsAotCompatible`、AOT/Trim 告警例外、宿主 RID AOT 验证路径均已退出现行治理。
3. 如需查看历史 AOT 决策背景，可保留阅读本文件版本历史；但不得再将其作为当前开发任务的验收标准。

