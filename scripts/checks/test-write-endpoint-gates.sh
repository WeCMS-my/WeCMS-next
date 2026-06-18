#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}

trap cleanup EXIT

assert_fails_with() {
  local expected="$1"
  shift
  local output_file="$tmp_dir/output.txt"

  set +e
  "$@" >"$output_file" 2>&1
  local status=$?
  set -e

  if [[ $status -eq 0 ]]; then
    printf 'expected command to fail: %s\n' "$*" >&2
    cat "$output_file" >&2
    exit 1
  fi

  if ! rg -q --fixed-strings -- "$expected" "$output_file"; then
    printf 'expected failure output to contain: %s\n' "$expected" >&2
    cat "$output_file" >&2
    exit 1
  fi
}

python3 - "$tmp_dir/get-write.json" "$tmp_dir/missing-permission.json" "$tmp_dir/missing-audit.json" <<'PY'
import json
import sys
from pathlib import Path

base = {
    "openapi": "3.0.1",
    "paths": {
        "/api/v1/system/users": {
            "post": {
                "security": [{"bearerAuth": []}],
                "x-wecms-permission": "sys:user:create",
            }
        }
    },
}

get_write = {
    "openapi": "3.0.1",
    "paths": {
        "/api/v1/system/users/{id:long}/delete": {
            "get": {
                "security": [{"bearerAuth": []}],
                "x-wecms-permission": "sys:user:delete",
            }
        }
    },
}

missing_permission = {
    "openapi": "3.0.1",
    "paths": {
        "/api/v1/system/users": {
            "post": {
                "security": [{"bearerAuth": []}],
            }
        }
    },
}

missing_audit = {
    "openapi": "3.0.1",
    "paths": {
        "/api/v1/system/not-audited": {
            "post": {
                "security": [{"bearerAuth": []}],
                "x-wecms-permission": "sys:not-audited:create",
            }
        }
    },
}

Path(sys.argv[1]).write_text(json.dumps(get_write), encoding="utf-8")
Path(sys.argv[2]).write_text(json.dumps(missing_permission), encoding="utf-8")
Path(sys.argv[3]).write_text(json.dumps(missing_audit), encoding="utf-8")
PY

bash "$repo_root/scripts/checks/check-write-endpoint-methods.sh" "$repo_root/artifacts/openapi/wecms-api-v1.json"
bash "$repo_root/scripts/checks/check-write-endpoint-permission-coverage.sh" "$repo_root/artifacts/openapi/wecms-api-v1.json"
bash "$repo_root/scripts/checks/check-write-endpoint-audit-coverage.sh" "$repo_root/artifacts/openapi/wecms-api-v1.json"

assert_fails_with "GET /api/v1/system/users/{id:long}/delete looks like a write operation" \
  bash "$repo_root/scripts/checks/check-write-endpoint-methods.sh" "$tmp_dir/get-write.json"

assert_fails_with "POST /api/v1/system/users missing x-wecms-permission" \
  bash "$repo_root/scripts/checks/check-write-endpoint-permission-coverage.sh" "$tmp_dir/missing-permission.json"

assert_fails_with "POST /api/v1/system/not-audited missing audit coverage entry" \
  bash "$repo_root/scripts/checks/check-write-endpoint-audit-coverage.sh" "$tmp_dir/missing-audit.json"

printf 'test-write-endpoint-gates: ok\n'
