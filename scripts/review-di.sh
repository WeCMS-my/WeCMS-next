#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="${1:-.}"
MODULE_DIRS=(
  "${ROOT_DIR}/backend/src/WeCms.Modules.System"
  "${ROOT_DIR}/backend/src/WeCms.Modules.Cms"
)

run_scan() {
  local title="$1"
  local pattern="$2"
  shift 2

  printf "\n[%s]\n" "$title"
  if rg -n "$pattern" "$@"; then
    return 1
  else
    printf "  ✅ none\n"
    return 0
  fi
}

scan_modules() {
  local title="$1"
  local pattern="$2"
  shift 2

  local had_matches=0
  local dir
  printf "\n[%s]\n" "$title"
  for dir in "${MODULE_DIRS[@]}"; do
    if rg -n "$pattern" "$dir" "$@"; then
      had_matches=1
    fi
  done

  if ((had_matches == 0)); then
    printf "  ✅ none\n"
    return 0
  fi

  return 1
}

printf "== DI Review Quick Scan ==\n"
printf "Root: %s\n" "$ROOT_DIR"
printf "Modules: %s %s\n" "${MODULE_DIRS[0]}" "${MODULE_DIRS[1]}"

P0=0
P1=0

if run_scan "1) 禁止的构造/实例化模式（P0）" \
  "new\\s+(HttpClient|MySqlConnection|SqlSugarClient|DbConnectionFactory|JwtTokenService|Pbkdf2PasswordHasher|SmtpClient|FileStorage)\\s*\\(" \
  "$ROOT_DIR/backend/src" -g "*.cs" -g "*.ts" -g "*.tsx" \
  --glob '!**/backend/tests/**' \
  --glob '!**/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs' \
  --glob '!**/backend/src/WeCms.Infrastructure/Id/**'; then
  :;
else
  P0=$((P0+1))
fi

if run_scan "2) 时间/随机/ID 未 DI 抽象（P0）" \
  "DateTime\\.UtcNow|Guid\\.NewGuid\\(\\)|Random\\.Shared" \
  "$ROOT_DIR" -g "*.cs" -g "*.ts" -g "*.tsx" \
  --glob '!**/backend/tests/**' \
  --glob '!**/backend/src/WeCms.Infrastructure/Id/**'; then
  :;
else
  P0=$((P0+1))
fi

if run_scan "3) Service Locator 使用（P1）" \
  "\\.GetRequiredService<|\\.GetService<" \
  "$ROOT_DIR" -g "*.cs" \
  --glob '!**/backend/tests/**' \
  --glob '!**/backend/src/WeCms.Api/Program.cs' \
  --glob '!**/backend/src/WeCms.Api/Extensions/OpenApiExtensions.cs' \
  --glob '!**/*Endpoints.cs'; then
  :;
else
  P1=$((P1+1))
fi

if scan_modules "4) 模块层数据库边界（P0）" \
  "Dapper\\.AOT|Dapper\\.|MySqlConnector|\\bDbConnection\\b|\\bDbTransaction\\b|ExecuteAsync|QueryAsync|CommandDefinition|\\bSELECT\\s+\\*\\b" \
  -g "*.cs"; then
  :;
else
  P0=$((P0+1))
fi

if scan_modules "5) 模块层 SQL 文本粗筛（P0）" \
  "\\\"\\s*(SELECT|INSERT|UPDATE|DELETE|INSERT INTO)\\b" \
  -g "*.cs"; then
  :;
else
  P0=$((P0+1))
fi

if run_scan "6) 构造参数具体实现风险（P1）" \
  "class\\s+\\w+\\([^\\)]*\\b(AuthRepository|UserRepository|RoleRepository|PermissionRepository|FileRepository|EmailRepository|.*RepositoryImpl|.*ServiceImpl)" \
  "${MODULE_DIRS[0]}" "${MODULE_DIRS[1]}" -g "*.cs"; then
  :;
else
  P1=$((P1+1))
fi

printf "\n=== DI 扫描总结 ===\n"
printf "P0: %s  P1: %s\n" "$P0" "$P1"
printf "建议：优先修复 P0，再修 P1。\n"
printf "说明：本扫描为快速静态扫描，部分命中需人工复核语境。\n"

if (( P0 + P1 > 0 )); then
  exit 1
fi

printf "\nDone.\n"
