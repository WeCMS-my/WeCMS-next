#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKEND_DIR="$PROJECT_ROOT/backend"
FRONTEND_DIR="$PROJECT_ROOT/frontend/soybean-admin"

echo "=== WeCMS Quality Gate ==="
echo ""

# 1. Build with warnings as errors
echo "[1/5] dotnet build -warnaserror"
dotnet build "$BACKEND_DIR/WeCms.sln" -warnaserror

# 2. Run tests
echo "[2/5] dotnet test"
dotnet test "$BACKEND_DIR/WeCms.sln"

# 3. AOT publish
echo "[3/5] dotnet publish AOT"
dotnet publish "$BACKEND_DIR/src/WeCms.Api/WeCms.Api.csproj" -c Release -r linux-x64 /p:PublishAot=true

# 4. Frontend typecheck
echo "[4/5] pnpm typecheck"
pnpm --dir "$FRONTEND_DIR" typecheck

# 5. Frontend build
echo "[5/5] pnpm build"
pnpm --dir "$FRONTEND_DIR" build

echo ""
echo "=== Quality Gate PASSED ==="
