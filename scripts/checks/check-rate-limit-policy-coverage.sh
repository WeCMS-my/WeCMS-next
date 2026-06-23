#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

require_contains() {
  local file="$1"
  local pattern="$2"
  if ! rg -F "$pattern" "$repo_root/$file" >/dev/null; then
    printf 'check-rate-limit-policy-coverage: %s missing required pattern: %s\n' "$file" "$pattern" >&2
    exit 1
  fi
}

require_not_line_contains() {
  local file="$1"
  local route="$2"
  local forbidden="$3"
  if rg -F "$route" "$repo_root/$file" | rg -F "$forbidden" >/dev/null; then
    printf 'check-rate-limit-policy-coverage: %s route %s must not contain %s\n' "$file" "$route" "$forbidden" >&2
    exit 1
  fi
}

require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'AuthLogin = "auth_login_policy"'
require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'AuthRefresh = "auth_refresh_policy"'
require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'AuthTwoFactor = "auth_2fa_policy"'
require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'AdminWrite = "admin_write_policy"'
require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'FileUpload = "file_upload_policy"'
require_contains "backend/src/WeCms.Shared/Security/RateLimitPolicyNames.cs" 'SecurityUnban = "security_unban_policy"'

require_contains "backend/src/WeCms.Api/Program.cs" 'builder.Services.AddWeCmsRateLimiting(builder.Configuration);'
require_contains "backend/src/WeCms.Api/Program.cs" 'app.UseRateLimiter();'
require_contains "backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs" 'options.OnRejected = OnRejectedAsync;'
require_contains "backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs" 'IRateLimitHitBuffer'
require_contains "backend/src/WeCms.Api/RateLimiting/RateLimitSecurityEventFlushHostedService.cs" 'RateLimitSecurityEventFlushHostedService'
require_contains "backend/src/WeCms.Modules.Security.SqlSugar/SecuritySqlSugarServiceCollectionExtensions.cs" 'IRateLimitSecurityEventRepository'

require_contains "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs" 'RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthLogin)'
require_contains "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs" 'RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthRefresh)'
require_contains "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs" 'RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthTwoFactor)'
require_contains "backend/src/WeCms.Modules.FileCenter/Files/FileEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.FileUpload)'
require_contains "backend/src/WeCms.Modules.Security/SecurityEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.SecurityUnban)'
require_contains "backend/src/WeCms.Modules.Identity/Endpoints/UserEndpointDefinition.cs" 'RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite)'
require_contains "backend/src/WeCms.Modules.AccessControl/Menus/MenuEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AdminWrite)'
require_contains "backend/src/WeCms.Modules.Configuration/Settings/SettingEndpoints.cs" 'RequireRateLimiting(AdminWriteRateLimitPolicy)'
require_contains "backend/src/WeCms.Modules.Configuration/Dicts/DictEndpoints.cs" 'RequireRateLimiting(AdminWriteRateLimitPolicy)'
require_contains "backend/src/WeCms.Modules.Configuration/I18n/I18nEndpoints.cs" 'RequireRateLimiting(AdminWriteRateLimitPolicy)'

require_not_line_contains "backend/src/WeCms.Modules.FileCenter/Files/FileEndpoints.cs" 'MapGet("/files"' 'RequireRateLimiting'
require_not_line_contains "backend/src/WeCms.Modules.Security/SecurityEndpoints.cs" 'MapGet("/bans"' 'RequireRateLimiting'
require_not_line_contains "backend/src/WeCms.Modules.Identity/Endpoints/AuthEndpointDefinition.cs" 'MapGet("/me"' 'RequireRateLimiting'

printf 'check-rate-limit-policy-coverage: ok\n'
