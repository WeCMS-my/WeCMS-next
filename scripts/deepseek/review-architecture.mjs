import fs from 'node:fs/promises';

// ── 输入预算（支持环境变量覆盖） ──
const MAX_FILES = Number(process.env.REVIEW_MAX_FILES ?? 20);
const MAX_FILE_CHARS = Number(process.env.REVIEW_MAX_FILE_CHARS ?? 8000);
const MAX_TOTAL_CHARS = Number(process.env.REVIEW_MAX_TOTAL_CHARS ?? 40000);

// ── API Key ──
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

// ── 文件优先级评分 ──
function scoreFile(file) {
  const name = file.replace(/\\/g, '/').toLowerCase();
  if (/(program\.cs|\.csproj|appsettings|endpoint|middleware|extensions)/.test(name)) return 100;
  if (/backend\/src/.test(name)) return 60;
  return 10;
}

// ── 单文件裁剪（保留头尾） ──
function clip(text, max) {
  if (text.length <= max) return { text, truncated: false };
  const half = Math.floor(max / 2);
  return {
    text: `${text.slice(0, half)}\n\n... [已截断 ${text.length - max} 字符] ...\n\n${text.slice(-half)}`,
    truncated: true
  };
}

// ── 按预算构建审查上下文 ──
async function buildReviewContext(rankedFiles) {
  const included = [];
  const truncated = [];
  const omitted = [];
  const errors = [];

  let totalChars = 0;
  let fileCount = 0;

  for (const file of rankedFiles) {
    if (fileCount >= MAX_FILES) {
      omitted.push(`${file} (超出文件数上限 ${MAX_FILES})`);
      continue;
    }

    let text;
    try {
      text = await fs.readFile(file, 'utf8');
    } catch (err) {
      errors.push(`${file}: ${err.message}`);
      continue;
    }

    const { text: clippedText, truncated: wasTruncated } = clip(text, MAX_FILE_CHARS);

    const header = `\n\n# File: ${file}${wasTruncated ? ' [已截断]' : ''}\n\n`;
    const entry = header + clippedText;

    if (totalChars + entry.length > MAX_TOTAL_CHARS) {
      omitted.push(`${file} (超出总字符数上限 ${MAX_TOTAL_CHARS})`);
      continue;
    }

    included.push(entry);
    totalChars += entry.length;
    fileCount++;

    if (wasTruncated) {
      truncated.push(file);
    }
  }

  return { included, truncated, omitted, errors };
}

// ── 主流程 ──
const rankedFiles = [...files].sort((a, b) => scoreFile(b) - scoreFile(a));
const { included, truncated, omitted, errors } = await buildReviewContext(rankedFiles);

// 输出诊断信息到 stderr
if (truncated.length > 0) {
  console.error(`[review-architecture] 已截断 ${truncated.length} 个文件: ${truncated.join(', ')}`);
}
if (omitted.length > 0) {
  console.error(`[review-architecture] 已跳过 ${omitted.length} 个文件:\n  - ${omitted.join('\n  - ')}`);
}
if (errors.length > 0) {
  console.error(`[review-architecture] 读取失败 ${errors.length} 个文件:\n  - ${errors.join('\n  - ')}`);
}

if (included.length === 0) {
  console.error('[review-architecture] 没有可审查的文件，请检查输入或预算配置');
  process.exit(1);
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

const omissionNote = omitted.length > 0
  ? `\n\n注意：以下文件因超出预算未附全文，请人工复查：\n${omitted.map(o => `- ${o}`).join('\n')}`
  : '';

const truncationNote = truncated.length > 0
  ? `\n\n注意：以下文件因超出单文件长度上限（${MAX_FILE_CHARS} 字符）已被截断，仅保留头尾：\n${truncated.map(t => `- ${t}`).join('\n')}`
  : '';

const reviewInput = [
  prompt,
  omissionNote,
  truncationNote,
  ...included
].filter(Boolean).join('\n');

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
        content: reviewInput
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
