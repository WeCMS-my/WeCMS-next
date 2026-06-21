wecms-next/
  AGENTS.md
  README.md

  docs/
    context/
      01-thinkphp-system.md
      02-next-migration-plan.md
      03-engineering-delivery.md
      04-m0-skeleton-validation.md
    adr/
      0001-use-dotnet10-native-aot.md
      0002-use-sqlsugar-orm.md
      0003-backend-contract-first.md
      0004-ai-independent-service-phase2.md

  backend/
    src/
      WeCms.Api/
      WeCms.Shared/
      WeCms.Infrastructure/
      WeCms.Data.SqlSugar/
      WeCms.Caching/
      WeCms.EventBus/
      WeCms.Aop/
      WeCms.Modules.Identity/
      WeCms.Modules.AccessControl/
      WeCms.Modules.Organization/
      WeCms.Modules.Configuration/
      WeCms.Modules.Audit/
      WeCms.Modules.Security/
      WeCms.Modules.FileCenter/
      WeCms.Modules.Platform/
      WeCms.Modules.*.SqlSugar/
      WeCms.Modules.Cms/        # 二期内容模块占位，系统基础升级期间不启用
    tests/
      WeCms.Tests.Unit/
      WeCms.Tests.Integration/

  frontend/
    soybean-admin/

  database/
    migrations/
    seeds/
    legacy-reference/

  scripts/
    codex/
      prompts/
    deepseek/
      review-architecture.mjs
      review-security.mjs
      review-sql.mjs
      review-migration.mjs

  artifacts/
    openapi/
    reports/
      deepseek/
      migration/

