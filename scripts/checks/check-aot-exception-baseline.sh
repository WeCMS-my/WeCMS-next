#!/usr/bin/env bash

# WeCMS M0-BE check-aot-exception-baseline
# Ensures Dapper, Dapper.AOT, and MySqlConnector versions match ADR-0006 baseline before AOT publish.

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

persistence_csproj="$REPO_ROOT/backend/src/WeCms.Persistence/WeCms.Persistence.csproj"

extract_version() {
  local csproj="$1"
  local package="$2"
  rg -o "PackageReference Include=\"${package}\" Version=\"[^\"]+\"" "$csproj" \
    | head -n 1 \
    | sed -E 's/.*Version=\"([^\"]+)\".*/\1/' \
    || true
}

persistence_dapper_version="$(extract_version "$persistence_csproj" "Dapper")"
persistence_dapper_aot_version="$(extract_version "$persistence_csproj" "Dapper.AOT")"
persistence_mysql_connector_version="$(extract_version "$persistence_csproj" "MySqlConnector")"

if [ -z "$persistence_dapper_version" ] || [ -z "$persistence_dapper_aot_version" ] || [ -z "$persistence_mysql_connector_version" ]; then
  echo "Failed to read required package versions from persistence project file." >&2
  exit 1
fi

if [ "$persistence_dapper_version" != "$WECMS_DAPPER_VERSION" ]; then
  echo "Dapper version mismatch detected." >&2
  echo "  baseline:  $WECMS_DAPPER_VERSION"
  echo "  persistence: $persistence_dapper_version"
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

if [ "$persistence_dapper_aot_version" != "$WECMS_DAPPER_AOT_VERSION" ]; then
  echo "Dapper.AOT version mismatch detected." >&2
  echo "  baseline: $WECMS_DAPPER_AOT_VERSION"
  echo "  persistence: $persistence_dapper_aot_version"
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

if [ "$persistence_mysql_connector_version" != "$WECMS_MYSQLCONNECTOR_VERSION" ]; then
  echo "MySqlConnector version mismatch detected." >&2
  echo "  baseline: $WECMS_MYSQLCONNECTOR_VERSION"
  echo "  persistence: $persistence_mysql_connector_version"
  echo "Please re-evaluate docs/adr/0006-aot-trim-warnings-exception.md before merge." >&2
  exit 1
fi

echo "  AOT warning baseline check passed (Dapper, Dapper.AOT, and MySqlConnector are aligned with ADR-0006)."
