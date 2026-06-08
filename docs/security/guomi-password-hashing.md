 # 国密密码哈希需求评估
 
 > 状态：待评估  
 > 优先级：非 M0 阻塞，列为 M1 安全阶段独立需求  
 > 关联：M0-BE-012（PasswordHasher），AGENTS.md 安全红线
 
 ## 1. 需求背景
 
 项目方提出：超级管理员及其他用户密码是否可以采用国密（SM 系列）算法进行哈希存储。
 
 国内密码标准中，与密码哈希场景相关的主要是 **SM3**（密码杂凑算法，GB/T 32905-2016），等效于替代 SHA-256 在 PBKDF2 中的角色。
 
 ## 2. 当前方案
 
 M0 阶段默认采用：
 
 ```
 PBKDF2 + SHA-256
 密钥长度：256 bits
 迭代次数：≥ 600,000 (OWASP 2025 推荐)
 盐：128 bits 随机值
 格式：wecms.pbkdf2.v1.<iterations>.<salt-base64>.<hash-base64>
 ```
 
 实现方式：.NET 内置 `System.Security.Cryptography.Rfc2898DeriveBytes`，Native AOT 零依赖，零风险。
 
 ## 3. 国密落地方案分析
 
 ### 3.1 方案一：PBKDF2 + SM3
 
 | 维度 | 评估 |
 |---|---|
 | 原理 | 用 SM3 替换 SHA-256 作为 PBKDF2 的底层伪随机函数 |
 | .NET 10 内置 | 否 |
 | 需要第三方库 | 是（BouncyCastle / GM.SmCrypto / 自实现） |
 | AOT 兼容性 | 未知，需在 M0 阶段实际验证 `dotnet publish /p:PublishAot=true` |
 | License | BouncyCastle: MIT；GM.SmCrypto: 需确认 |
 | 维护状态 | BouncyCastle 活跃；国产库需评估 |
 
 ### 3.2 方案二：纯 SM3 哈希（无 PBKDF2）
 
 不推荐。SM3 是快速哈希，不加盐 + 不迭代等同于裸 SHA-256 哈希密码，无法抵御彩虹表和暴力破解。
 
 ### 3.3 推荐路线
 
 通过 `IPasswordHasher` 接口抽象，支持多算法共存：
 
 ```
 IPasswordHasher
   ├── Pbkdf2PasswordHasher          (PBKDF2-SHA256，M0 默认实现)
   └── Pbkdf2Sm3PasswordHasher       (PBKDF2-SM3，M1 可选实现)
 ```
 
 密码格式扩展为可识别算法版本：
 
 ```
 wecms.pbkdf2-sha256.v1.<iter>.<salt>.<hash>     ← M0 格式
 wecms.pbkdf2-sm3.v1.<iter>.<salt>.<hash>        ← 国密格式（待实现）
 ```
 
 ## 4. 前置验证项
 
 在决定引入国密之前，必须先完成以下验证：
 
 1. **AOT publish 实际通过** — 候选第三方库在 `PublishAot=true` + `linux-x64` 下无阻断警告
 2. **性能基准** — SM3 实现与 SHA-256 的哈希速度对比，确保不影响登录体验
 3. **合规确认** — 确认是否为硬性合规要求，还是技术偏好
 4. **License 审查** — 第三方国密库的 License 是否允许商业使用
 
 ## 5. 决策时间点
 
 | 阶段 | 动作 |
 |---|---|
 | M0 | 仅实现 PBKDF2-SHA256，预留 `IPasswordHasher` 接口 |
 | M1 启动前 | 完成国密第三方库的 AOT 兼容性验证 |
 | M1 | 若验证通过且合规要求明确，实现 `Pbkdf2Sm3PasswordHasher` 作为可插拔实现 |
 
 ## 6. 风险评估
 
 | 风险 | 等级 | 说明 |
 |---|---|---|
 | AOT 不兼容 | 中 | 国产密码库大多未针对 Native AOT 优化 |
 | 维护依赖 | 中 | 第三方国密库的长期维护不确定 |
 | 合规必要性 | 待确认 | 非金融/政务场景可能不强制要求 |
 | 新旧格式共存 | 低 | 通过算法版本前缀天然隔离 |
 
 ## 7. 结论
 
 - M0 **不阻塞**：PBKDF2-SHA256 直接可用，AOT 零风险
 - `IPasswordHasher` 接口设计预留算法扩展能力
 - 国密作为 **独立安全需求** 跟踪，在 M1 启动前完成技术验证
 - 如果合规不是硬性要求，建议保持 PBKDF2-SHA256 不变
