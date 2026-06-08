 # ADR-0004: AI as Independent Service, Phase 2 Only
 
 > Status: accepted  
 > Date: 2026-06-08
 
 ## Context
 WeCMS Next 未来可能需要 AI 能力，但一期重点在 CMS Core 的稳定性。
 
 ## Decision
 AI 功能作为二期独立项目（wecms-ai），一期严禁实现任何 AI runtime。
 
 ## Rationale
 - 避免分散一期核心 CMS 功能的工程资源
 - AI 服务需要独立扩展（GPU、向量库），不适合作为 CMS 模块
 - 数据安全：AI 服务通过 API 访问 CMS 数据，不直连数据库
 
 ## Consequences
 - 一期严禁创建 WeCms.Modules.Ai
 - 一期严禁实现 AI Provider、Prompt、RAG、Vector Store、Agent Tool
 - 一期严禁在后端调用任何模型 API
 - 一期严禁在前端增加 AI 页面
 - 二期 AI 服务必须作为独立项目
 - 二期 AI 只能通过 CMS Core API 获取数据
 - 二期 AI 严禁直连 CMS 数据库或文件存储
 - CMS Core 保留 AI Bridge / AI-facing API 边界扩展点
