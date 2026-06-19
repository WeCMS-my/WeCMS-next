#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import json
import re
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []
template_path = repo / "backend/src/WeCms.Api/appsettings.Production.example.json"
frontend_env_path = repo / "frontend/soybean-admin/.env.production.example"

if not template_path.is_file():
    violations.append("missing backend production template")
else:
    text = template_path.read_text(encoding="utf-8")
    try:
        data = json.loads(text)
    except json.JSONDecodeError as exc:
        violations.append(f"production template invalid json: {exc}")
        data = {}

    forbidden_patterns = [
        r"pwd=(?!__SET_BY_ENV__|__SET_BY_SECRET_MANAGER__)[^;]+",
        r"sk-[A-Za-z0-9]{20,}",
        r"sk-proj-[A-Za-z0-9_-]+",
        r"AKIA[0-9A-Z]{16}",
        r"BEGIN (RSA|OPENSSH|PRIVATE) KEY",
        r"localhost",
        r"127\.0\.0\.1",
    ]
    for pattern in forbidden_patterns:
        if re.search(pattern, text):
            violations.append(f"production template contains forbidden pattern {pattern}")

    if data:
        if data.get("ConnectionStrings", {}).get("Default") != "__SET_BY_ENV__":
            violations.append("ConnectionStrings:Default must be __SET_BY_ENV__")
        if data.get("Auth", {}).get("AccessTokenSecret") != "__SET_BY_ENV__":
            violations.append("Auth:AccessTokenSecret must be __SET_BY_ENV__")
        if data.get("Security", {}).get("TwoFactor", {}).get("SecretProtectionKey") != "__SET_BY_ENV__":
            violations.append("2FA SecretProtectionKey must be __SET_BY_ENV__")
        if data.get("Database", {}).get("SeedAdminPassword") != "__SET_BY_SECRET_MANAGER__":
            violations.append("Database:SeedAdminPassword must be __SET_BY_SECRET_MANAGER__")
        if data.get("FileStorage", {}).get("Local", {}).get("BasePath") != "__SET_BY_ENV__":
            violations.append("FileStorage:Local:BasePath must be __SET_BY_ENV__")

if not frontend_env_path.is_file():
    violations.append("missing frontend production env example")
else:
    env_text = frontend_env_path.read_text(encoding="utf-8")
    if "VITE_API_BASE_URL=https://api.example.com" not in env_text:
        violations.append("frontend production env example must use safe HTTPS placeholder")
    if "localhost" in env_text or "127.0.0.1" in env_text or "http://" in env_text:
        violations.append("frontend production env example must not use localhost or HTTP")

if violations:
    print("check-production-template-no-secrets: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-production-template-no-secrets: ok")
PY
