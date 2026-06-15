#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

LOG_DIR="${TMPDIR:-/tmp}/wecms-quality-gate"
mkdir -p "$LOG_DIR"
PUBLISH_LOG="$LOG_DIR/publish.log"

echo "=== WeCMS M0-BE Backend Quality Gate ==="

echo "==> dotnet build"
dotnet build backend/WeCms.sln -warnaserror

echo "==> dotnet test"
dotnet test backend/WeCms.sln

echo "==> dotnet publish linux-x64 JIT"
set +e
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false 2>&1 | tee "$PUBLISH_LOG"
publish_status=${PIPESTATUS[0]}
set -e

if [[ "$publish_status" -ne 0 ]]; then
  echo "Publish failed with exit code $publish_status."
  exit "$publish_status"
fi

if grep -Eiq '(^|[[:space:]:])(error)[[:space:]]+(CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|:[[:space:]]*(error)[[:space:]]+(CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|错误[[:space:]]+(CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)' "$PUBLISH_LOG"; then
  echo "Publish output contains detected errors."
  exit 1
fi

echo "Publish completed without detected build errors."

if [[ -d frontend ]] && git status --short -- frontend | grep -q .; then
  echo "Frontend changes detected during backend-only gate."
  git status --short -- frontend
  exit 1
fi

echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
