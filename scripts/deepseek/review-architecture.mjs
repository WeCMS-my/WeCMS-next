import fs from 'node:fs/promises';

const apiKey = process.env.DEEPSEEK_API_KEY;

if (!apiKey) {
  console.error('Missing DEEPSEEK_API_KEY');
  process.exit(1);
}

const files = process.argv.slice(2);

if (files.length === 0) {
  console.error('Usage: node scripts/deepseek/review-architecture.mjs <files...>');
  process.exit(1);
}

const contents = [];

for (const file of files) {
  const text = await fs.readFile(file, 'utf8');
  contents.push(`\n\n# File: ${file}\n\n${text}`);
}

const prompt = `
你是 WeCMS 架构审查员。

请审查以下文件是否违反项目约束：

1. ASP.NET Core Minimal APIs
2. .NET 10 Native AOT Only
3. Dapper / Dapper.AOT
4. 禁止 MVC Controller
5. 禁止 EF Core
6. 禁止 dynamic
7. 禁止 SELECT *
8. 所有 DTO 必须加入 JsonSerializerContext
9. 除 AllowAnonymous 外，业务 Endpoint 必须绑定权限码
10. AI 接入是二期独立项目，当前不得实现运行时 AI 功能

请输出：
- 必须修复
- 建议修复
- AOT 风险
- 安全风险
- 是否允许进入 PR Review
`;

const response = await fetch('https://api.deepseek.com/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${apiKey}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    model: 'deepseek-v4-pro',
    messages: [
      {
        role: 'system',
        content: '你是严谨的软件架构和安全审查专家。'
      },
      {
        role: 'user',
        content: `${prompt}\n\n${contents.join('\n')}`
      }
    ],
    thinking: { type: 'enabled' },
    reasoning_effort: 'high',
    stream: false
  })
});

if (!response.ok) {
  console.error(await response.text());
  process.exit(1);
}

const json = await response.json();
console.log(json.choices?.[0]?.message?.content ?? JSON.stringify(json, null, 2));