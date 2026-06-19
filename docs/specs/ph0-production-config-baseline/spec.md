# PH-0 Production Configuration Baseline Spec

## Objective

Establish the first production hardening baseline for WeCMS Next configuration.

PH-0 must make production configuration auditable and fail-fast without adding CMS phase-two features, AI runtime code, legacy ThinkPHP compatibility, database migrations, or frontend contract changes.

## Scope

In scope:

- Add the Production Hardening plan as a repository plan document.
- Add a production configuration inventory.
- Add a safe production appsettings example template.
- Add Production-only fail-fast validation for required runtime configuration.
- Replace misleading Development password placeholders.
- Reference the production configuration document from README.
- Add a script check so the backend gate verifies the PH-0 docs and template.

Out of scope:

- No CMS Articles / Channels / Pages / Media / Tags / Links implementation.
- No AI runtime, model provider, prompt, RAG, vector store, or agent tool code.
- No old ThinkPHP runtime compatibility or data migration.
- No migration execution strategy change. PH-2 owns migration production governance.
- No file storage provider implementation change. PH-4 owns file storage productionization.
- No frontend generated type change.

## Required Production Keys

Production startup must fail when these keys are absent or unsafe:

- `ConnectionStrings:Default`
- `Auth:AccessTokenSecret`
- `Security:TwoFactor:SecretProtectionKey`
- `Security:AllowedOrigins`
- `Database:SeedAdminPassword`

Production startup must also reject obvious non-production values:

- `__SET_BY_ENV__`
- `__SET_BY_SECRET_MANAGER__`
- `Admin@123` for `Database:SeedAdminPassword`
- localhost or wildcard origins in `Security:AllowedOrigins`
- non-HTTPS origins in `Security:AllowedOrigins`

Development behavior remains local-dev friendly and is not tightened by the PH-0 validator.

## Acceptance

- `docs/ops/production-configuration.md` exists and lists backend and frontend production configuration keys.
- `backend/src/WeCms.Api/appsettings.Production.example.json` exists and contains only safe placeholders.
- Production fail-fast unit tests cover missing Auth secret, missing 2FA key, default seed password, and Development allowance.
- README links the production configuration document and clarifies Development placeholder behavior.
- Backend gate runs a PH-0 production configuration check.
