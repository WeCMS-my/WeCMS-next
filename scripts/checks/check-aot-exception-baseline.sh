#!/usr/bin/env bash

# WeCMS M0-BE check-aot-exception-baseline
# Ensures Dapper / Dapper.AOT versions match ADR-0006 baseline before AOT publish.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--repo-root <path>]" >&2
      exit 1
      ;;
  esac
done

BASELINE_FILE="$SCRIPT_DIR/aot-exception-baseline.env"

if [ ! -f "$BASELINE_FILE" ]; then
  echo "Baseline file missing: $BASELINE_FILE" >&2
  exit 1
fi

source "$BASELINE_FILE"

infrastructure_csproj="$REPO_ROOT/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj"
system_csproj="$REPO_ROOT/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj"

extract_version() {
  local csproj="$1"
  local package="$2"
  rg -o "PackageReference Include=\"${package}\" Version=\"[^\"]+\"" "$csproj" \
    | head -n 1 \
    | sed -E 's/.*Version=\"([^\"]+)\".*/\1/' \
    || true
}

infra_dapper_version="$(extract_version "$infrastructure_csproj" "Dapper")"
infra_dapper_aot_version="$(extract_version "$infrastructure_csproj" "Dapper.AOT")"
system_dapper_version="$(extract_version "$system_csproj" "Dapper")"

if [ -z "$infra_dapper_version" ] || [ -z "$infra_dapper_aot_version" ] || [ -z "$system_dapper_version" ]; then
  echo "Failed to read Dapper versions from project files." >&2
  exit 1
fi

if [ "$infra_dapper_version" != "$WECMS_DAPPER_VERSION" ] || [ "$system_dapper_version" != "$WECMS_DAPPER_VERSION" ]; then
  echo "Dapper version mismatch detected." >&2
  echo "  baseline:  $WECMS_DAPPER_VERSION"
  echo "  infrastructure: $infra_dapper_version"
  echo "  modules-system: $system_dapper_version"
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

if [ "$infra_dapper_aot_version" != "$WECMS_DAPPER_AOT_VERSION" ]; then
  echo "Dapper.AOT version mismatch detected." >&2
  echo "  baseline: $WECMS_DAPPER_AOT_VERSION"
  echo "  infrastructure: $infra_dapper_aot_version"
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

violations=()
while IFS= read -r csproj; do
  local_dapper_version="$(extract_version "$csproj" "Dapper")"
  local_dapper_aot_version="$(extract_version "$csproj" "Dapper.AOT")"

  if [ -n "$local_dapper_version" ] && [ "$local_dapper_version" != "$WECMS_DAPPER_VERSION" ]; then
    violations+=("${csproj}: Dapper=$local_dapper_version")
  fi

  if [ -n "$local_dapper_aot_version" ] && [ "$local_dapper_aot_version" != "$WECMS_DAPPER_AOT_VERSION" ]; then
    violations+=("${csproj}: Dapper.AOT=$local_dapper_aot_version")
  fi
done < <(find "$REPO_ROOT/backend/src" -name "*.csproj" -print)

if [ "${#violations[@]}" -ne 0 ]; then
  echo "Additional Dapper version mismatch detected." >&2
  for item in "${violations[@]}"; do
    echo "  $item" >&2
  done
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

echo "  AOT warning baseline check passed (Dapper versions are aligned with ADR-0006)."
