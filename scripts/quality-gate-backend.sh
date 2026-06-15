#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

LOG_DIR="${TMPDIR:-/tmp}/wecms-quality-gate"
mkdir -p "$LOG_DIR"
AOT_LOG="$LOG_DIR/aot-publish.log"

echo "=== WeCMS M0-BE Backend Quality Gate ==="

echo "==> dotnet build"
dotnet build backend/WeCms.sln -warnaserror

echo "==> dotnet test"
dotnet test backend/WeCms.sln

echo "==> dotnet publish linux-x64 Native AOT"
set +e
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  /p:PublishAot=true 2>&1 | tee "$AOT_LOG"
publish_status=${PIPESTATUS[0]}
set -e

if [[ "$publish_status" -ne 0 ]]; then
  echo "Native AOT publish failed with exit code $publish_status."
  exit "$publish_status"
fi

if grep -Eiq '(^|[[:space:]:])(warning|error)[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|:[[:space:]]*(warning|error)[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|警告[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|错误[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)' "$AOT_LOG"; then
  echo "Native AOT publish produced warnings or errors. AOT gate requires 0 error / 0 warning."
  exit 1
fi

echo "Native AOT publish output contains 0 detected warnings and 0 detected errors."

if [[ -d frontend ]] && git status --short -- frontend | grep -q .; then
  echo "Frontend changes detected during backend-only gate."
  git status --short -- frontend
  exit 1
fi

echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
