 # ADR-0003: Backend Contract First
 
 > Status: accepted  
 > Date: 2026-06-08
 
 ## Context
 前后端分离项目中，常见问题是前端根据 UI 模板（如 SoybeanAdmin mock 数据）自行定义数据结构，导致与后端实际 API 不一致。
 
 ## Decision
 采用后端契约优先：后端 DTO → OpenAPI → TypeScript 类型 → 前端消费。
 
 ## Rationale
 - 后端 DTO 是唯一的字段事实源
 - OpenAPI 是自动化可验证的契约
 - TypeScript 类型由工具生成，消除手写错误
 - SoybeanAdmin 仅作为 UI 模板，不作为 API 契约来源
 
 ## Consequences
 - 前端 service/generated 目录禁止手写
 - 前端不得为了适配模板修改后端字段
 - request interceptor 只处理 token/401/403，不重塑业务 data
 - 前端不得定义自己的业务错误码
 - 动态路由事实源是后端菜单 DTO
 - OpenAPI diff 发现破坏性变更必须阻断合并
