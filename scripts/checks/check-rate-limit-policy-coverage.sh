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

require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'AuthLogin = "auth_login_policy"'
require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'AuthRefresh = "auth_refresh_policy"'
require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'AuthTwoFactor = "auth_2fa_policy"'
require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'AdminWrite = "admin_write_policy"'
require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'FileUpload = "file_upload_policy"'
require_contains "backend/src/WeCms.Modules.System/Security/RateLimitRecords.cs" 'SecurityUnban = "security_unban_policy"'

require_contains "backend/src/WeCms.Api/Program.cs" 'builder.Services.AddWeCmsRateLimiting(builder.Configuration);'
require_contains "backend/src/WeCms.Api/Program.cs" 'app.UseRateLimiter();'
require_contains "backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs" 'options.OnRejected = OnRejectedAsync;'
require_contains "backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs" 'IRateLimitSecurityEventService'
require_contains "backend/src/WeCms.Persistence/Data/PersistenceServiceCollectionExtensions.cs" 'IRateLimitSecurityEventRepository'

require_contains "backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AuthLogin)'
require_contains "backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AuthRefresh)'
require_contains "backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AuthTwoFactor)'
require_contains "backend/src/WeCms.Modules.System/Files/FileEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.FileUpload)'
require_contains "backend/src/WeCms.Modules.System/Security/SecurityEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.SecurityUnban)'
require_contains "backend/src/WeCms.Modules.System/Users/UserEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AdminWrite)'
require_contains "backend/src/WeCms.Modules.System/Menus/MenuEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AdminWrite)'
require_contains "backend/src/WeCms.Modules.System/Settings/SettingEndpoints.cs" 'RequireRateLimiting(RateLimitPolicyNames.AdminWrite)'

require_not_line_contains "backend/src/WeCms.Modules.System/Files/FileEndpoints.cs" 'MapGet("/files"' 'RequireRateLimiting'
require_not_line_contains "backend/src/WeCms.Modules.System/Security/SecurityEndpoints.cs" 'MapGet("/bans"' 'RequireRateLimiting'
require_not_line_contains "backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs" 'MapGet("/me"' 'RequireRateLimiting'

printf 'check-rate-limit-policy-coverage: ok\n'
