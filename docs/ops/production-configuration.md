# WeCMS Next Production Configuration

This document is the PH-0 production configuration inventory. It defines which keys are required, how they differ by environment, and which values must never be committed.

Template: `backend/src/WeCms.Api/appsettings.Production.example.json`.

## Rules

- Real production secrets must come from environment variables or a secret manager.
- Do not commit production connection strings, JWT secrets, 2FA keys, seed passwords, storage keys, SMTP passwords, webhook secrets, or tokens.
- Production startup fails when required keys are missing or still use placeholders.
- Development may use local `user-secrets` or local environment variables.
- Staging must follow production secret rules, with staging-only hosts and credentials.

## Backend Configuration

| Key | Required | Environment | Example | Secret level | Default allowed | Fail-fast behavior | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ConnectionStrings:Default` | Yes | Dev/Staging/Production | `__SET_BY_ENV__` | High | No in Production | Production rejects empty or placeholder | Ops |
| `ConnectionStrings:Migration` | Recommended | Staging/Production | `__SET_BY_SECRET_MANAGER__` | High | Fallback to Default | Used by `--migrate` when configured | Ops |
| `Auth:AccessTokenSecret` | Yes | Dev/Staging/Production | `__SET_BY_ENV__` | Critical | No | Production rejects empty, placeholder, or length < 32 | Backend |
| `Auth:Issuer` | Yes | Dev/Staging/Production | `wecms-production` | Public | Yes | Falls back in lower env; production template must declare | Backend |
| `Auth:AccessTokenMinutes` | Yes | Dev/Staging/Production | `15` | Public | Yes | Existing Auth registration validates integer | Backend |
| `Auth:RefreshTokenDays` | Yes | Dev/Staging/Production | `7` | Public | Yes | Existing Auth registration validates integer | Backend |
| `Security:TwoFactor:SecretProtectionKey` | Yes | Dev/Staging/Production | `__SET_BY_ENV__` | Critical | No | Production rejects empty, placeholder, or length < 32 | Backend |
| `Security:TwoFactor:Issuer` | Yes | Dev/Staging/Production | `WeCMS` | Public | Yes | Existing 2FA registration applies default | Backend |
| `Security:TwoFactor:PeriodSeconds` | Yes | Dev/Staging/Production | `30` | Public | Yes | Existing 2FA registration validates integer | Backend |
| `Security:TwoFactor:CodeDigits` | Yes | Dev/Staging/Production | `6` | Public | Yes | Existing 2FA registration validates integer | Backend |
| `Security:TwoFactor:AllowedWindowSteps` | Yes | Dev/Staging/Production | `1` | Public | Yes | Existing 2FA registration validates integer | Backend |
| `Security:TwoFactor:RecoveryCodeCount` | Yes | Dev/Staging/Production | `10` | Public | Yes | Existing 2FA registration validates integer | Backend |
| `Security:TwoFactor:ChallengeMinutes` | Yes | Dev/Staging/Production | `5` | Public | Yes | Existing Auth registration validates integer | Backend |
| `Security:TwoFactor:ChallengeMaxFailedAttempts` | Yes | Dev/Staging/Production | `5` | Public | Yes | Existing Auth registration validates integer | Backend |
| `Security:AllowedOrigins` | Yes | Dev/Staging/Production | `https://admin.example.com` | Public deploy setting | No in Production | Production rejects empty, wildcard, localhost, or HTTP origins | Ops |
| `Security:RequireOriginForCookieAuth` | Yes | Dev/Staging/Production | `true` | Public | No in Production | Existing Cookie auth validator rejects false outside Development | Backend |
| `Security:AllowRefererFallbackForCookieAuth` | Yes | Dev/Staging/Production | `false` | Public | Yes | Recommended false in Production | Security |
| `Security:ForwardedHeaders:Enabled` | Yes | Dev/Staging/Production | `true` | Public | No | Production trusts forwarded headers only when enabled | Ops |
| `Security:ForwardedHeaders:KnownProxies` | Required when enabled | Staging/Production | `10.0.0.10` | Sensitive topology | No | Production rejects enabled forwarded headers without proxies or networks | Ops |
| `Security:ForwardedHeaders:KnownNetworks` | Required when enabled | Staging/Production | `10.0.0.0/24` | Sensitive topology | No | Production rejects invalid CIDR networks | Ops |
| `Security:SecureHeaders:CspEnabled` | Yes | Dev/Staging/Production | `false` | Public | Yes | Production requires CSP value when enabled | Security |
| `Security:SecureHeaders:CspReportOnlyEnabled` | Yes | Dev/Staging/Production | `true` | Public | Yes | Production requires CSP report-only value when enabled | Security |
| `Security:SecureHeaders:PermissionsPolicy` | Yes | Dev/Staging/Production | `geolocation=(), microphone=(), camera=()` | Public | Yes | Middleware applies configured value | Security |
| `Security:SecureHeaders:Csp` | Required when enforce enabled | Staging/Production | `default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'` | Public | No | Production requires `object-src 'none'` and `frame-ancestors` | Security |
| `Security:SecureHeaders:CspReportOnly` | Yes | Dev/Staging/Production | `default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'` | Public | Yes | PH-1 tightens CSP checks | Security |
| `Security:LoginFailure:Enabled` | Yes | Dev/Staging/Production | `true` | Public | Yes | Existing Auth registration validates boolean | Security |
| `Security:LoginFailure:WindowMinutes` | Yes | Dev/Staging/Production | `10` | Public | Yes | Existing Auth registration validates integer | Security |
| `Security:LoginFailure:UsernameThreshold` | Yes | Dev/Staging/Production | `5` | Public | Yes | Existing Auth registration validates integer | Security |
| `Security:LoginFailure:IpThreshold` | Yes | Dev/Staging/Production | `20` | Public | Yes | Existing Auth registration validates integer | Security |
| `Security:LoginFailure:BanThreshold` | Yes | Dev/Staging/Production | `10` | Public | Yes | Existing Auth registration validates integer | Security |
| `Security:LoginFailure:BanMinutes` | Yes | Dev/Staging/Production | `15` | Public | Yes | Existing Auth registration validates integer | Security |
| `Security:RateLimiting:AuthLogin` | Yes | Dev/Staging/Production | `PermitLimit=5, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `Security:RateLimiting:AuthRefresh` | Yes | Dev/Staging/Production | `PermitLimit=20, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `Security:RateLimiting:AuthTwoFactor` | Yes | Dev/Staging/Production | `PermitLimit=5, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `Security:RateLimiting:AdminWrite` | Yes | Dev/Staging/Production | `PermitLimit=60, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `Security:RateLimiting:FileUpload` | Yes | Dev/Staging/Production | `PermitLimit=10, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `Security:RateLimiting:SecurityUnban` | Yes | Dev/Staging/Production | `PermitLimit=5, WindowMinutes=1` | Public | Yes | Existing rate limit registration validates positive integers | Security |
| `FileStorage:Provider` | Yes | Dev/Staging/Production | `local` | Public | Yes | Production rejects non-`local` until object storage adapter exists | Backend/Ops |
| `FileStorage:Local:BasePath` | Yes in Production | Dev/Staging/Production | `/var/lib/wecms/files` | Sensitive path | Development default `storage/files` | Production rejects missing, relative, nonexistent, unwritable, or web-root paths | Ops |
| `FileStorage:PublicBaseUrl` | Optional | Staging/Production | `https://files.example.com` | Public deploy setting | Empty allowed for local API-served downloads | Documented for future object storage providers | Ops |
| `FileStorage:MaxUploadBytes` | Yes | Dev/Staging/Production | `10485760` | Public | Yes | Documents deploy cap; per-policy upload caps remain enforced | Backend |
| `FileStorage:AllowedMimeTypes` | Yes | Dev/Staging/Production | `image/png,image/jpeg` | Public | Yes | Documents deploy MIME families; per-policy allowlists remain enforced | Backend |
| `FileStorage:VirusScanEnabled` | Yes | Staging/Production | `false` | Public | Yes | Production rejects `true` while only `NoopFileScanService` exists | Security |
| `Logging:LogLevel:Default` | Yes | Dev/Staging/Production | `Information` | Public | Yes | ASP.NET configuration applies default | Ops |
| `Logging:LogLevel:Microsoft.AspNetCore` | Yes | Dev/Staging/Production | `Warning` | Public | Yes | ASP.NET configuration applies default | Ops |
| `Database:SeedAdminPassword` | Yes in Production | Staging/Production | `__SET_BY_SECRET_MANAGER__` | Critical | Development only | Production rejects empty, placeholder, `Admin@123`, or weak value | Ops |
| `Database:RunMigrationsOnStartup` | Yes | Dev/Staging/Production | `false` | Public | Yes | Production default is false; `--migrate` is the production entry | Backend/Ops |
| `Database:CommandTimeoutSeconds` | Yes | Dev/Staging/Production | `30` | Public | Yes | Persistence rejects values outside 1-300 | Backend/Ops |
| `Database:LatestRequiredMigration` | Yes | Dev/Staging/Production | `000019_h2_security_event_classifier` | Public | Yes | Readiness reports migration unavailable when this version is absent from `sys_schema_migration` | Backend/Ops |

## Frontend Configuration

| Key | Required | Environment | Example | Secret level | Default allowed | Fail-fast behavior | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `VITE_API_BASE_URL` | Depends on deploy mode | Dev/Staging/Production | `https://api.example.com` | Public deploy setting | Empty allowed for same-origin | Frontend gate rejects HTTP or localhost production examples | Frontend/Ops |
| Build mode | Yes | Staging/Production | `production` | Public | No | Frontend gate runs production build and config checks | Frontend |
| Route permission source | Yes | All | Backend `/api/v1/auth/me` menus and permissions | Public | No static override | Existing frontend gate checks route permission coverage | Frontend |

## Environment Behavior

Development:

- Use `dotnet user-secrets` or local environment variables for DB and secrets.
- `Database:SeedAdminPassword` may be absent, and Development seed may use `Admin@123`.
- `Security:AllowedOrigins` may include localhost.

Staging:

- Use staging-specific secret manager entries.
- Do not reuse Development passwords or keys.
- Use HTTPS origins matching staging admin URLs.

Production:

- Required keys must be present before startup.
- Secrets must be injected outside git.
- `Security:AllowedOrigins` must be HTTPS, explicit, and non-localhost.
- `Security:ForwardedHeaders` must include known proxies or networks when enabled.
- CSP report-only or enforce mode must be enabled and must include `object-src 'none'` plus `frame-ancestors`.
- `Database:SeedAdminPassword` must be strong and must not equal `Admin@123`.
- Runtime must not auto-run migrations in Production. Use the `--migrate` command with a migration account.

## Current Deviations Recorded In PH-0

- Migration / seed runners are registered, but the current API host does not automatically execute them at startup. PH-0 corrects README wording; PH-2 owns the production migration execution strategy.
- FileStorage uses the local provider in PH-4. Object storage adapters are explicitly deferred by `docs/adr/production-file-storage-provider.md`.
- PH-1 adds full CORS production policy, reverse proxy strategy, and CSP enforce rollout controls.
