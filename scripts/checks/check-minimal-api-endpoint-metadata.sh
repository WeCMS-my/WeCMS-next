#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

command -v rg >/dev/null 2>&1 || {
  printf 'check-minimal-api-endpoint-metadata: rg is required.\n' >&2
  exit 1
}

controller_violations="$(
  rg -n --fixed-strings \
    -e ': ControllerBase' \
    -e ': Controller' \
    -e 'AddControllers(' \
    -e 'MapControllers(' \
    -e '[ApiController]' \
    "$src_root" \
    --glob '*.cs' \
    --glob '!**/bin/**' \
    --glob '!**/obj/**' || true
)"

if [[ -n "$controller_violations" ]]; then
  printf 'check-minimal-api-endpoint-metadata: Controller API surface is forbidden.\n%s\n' "$controller_violations" >&2
  exit 1
fi

endpoint_files="$(
  rg -l '\bMap(Get|Post|Put|Delete|Patch)\s*\(' \
    "$src_root" \
    --glob '*.cs' \
    --glob '!**/bin/**' \
    --glob '!**/obj/**' || true
)"

metadata_violations=()
while IFS= read -r file; do
  [[ -n "$file" ]] || continue

  if ! rg -q '\.(RequirePermission|RequireAuthorization|AllowAnonymous)\s*\(' "$file"; then
    metadata_violations+=("${file#$repo_root/}")
  fi
done <<<"$endpoint_files"

if [[ ${#metadata_violations[@]} -ne 0 ]]; then
  printf 'check-minimal-api-endpoint-metadata: endpoint mapping files must declare RequirePermission, RequireAuthorization, or AllowAnonymous metadata.\n' >&2
  printf '%s\n' "${metadata_violations[@]}" >&2
  exit 1
fi

printf 'check-minimal-api-endpoint-metadata: ok\n'
