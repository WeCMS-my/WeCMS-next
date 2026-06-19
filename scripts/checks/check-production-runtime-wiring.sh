#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import json
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []


def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""


def add_violation(message: str) -> None:
    violations.append(message)


program = read("backend/src/WeCms.Api/Program.cs")
file_storage_ext = read("backend/src/WeCms.Api/Extensions/FileStorageExtensions.cs")
system_files_ext = read("backend/src/WeCms.Modules.System/Files/SystemFilesServiceCollectionExtensions.cs")
fwd_ext = read("backend/src/WeCms.Api/Security/WeCmsForwardedHeadersExtensions.cs")
cors_ext = read("backend/src/WeCms.Api/Security/WeCmsCorsExtensions.cs")
config_validator = read("backend/src/WeCms.Api/Configuration/ProductionConfigurationValidator.cs")
request_ts = read("frontend/soybean-admin/src/api/request.ts")
frontend_prod_doc = read("docs/ops/frontend-production.md")
production_config_doc = read("docs/ops/production-configuration.md")
deployment_proxy_doc = read("docs/ops/deployment-reverse-proxy.md")
file_storage_doc = read("docs/ops/file-storage-production.md")

for relative in [
    "backend/src/WeCms.Api/Program.cs",
    "backend/src/WeCms.Api/Extensions/FileStorageExtensions.cs",
    "backend/src/WeCms.Modules.System/Files/SystemFilesServiceCollectionExtensions.cs",
    "backend/src/WeCms.Api/Security/WeCmsForwardedHeadersExtensions.cs",
    "backend/src/WeCms.Api/Security/WeCmsCorsExtensions.cs",
    "backend/src/WeCms.Api/Configuration/ProductionConfigurationValidator.cs",
    "frontend/soybean-admin/.env.production.example",
    "frontend/soybean-admin/src/api/request.ts",
    "docs/ops/frontend-production.md",
    "docs/ops/production-configuration.md",
    "docs/ops/deployment-reverse-proxy.md",
    "docs/ops/file-storage-production.md",
    "backend/src/WeCms.Api/appsettings.Production.example.json",
]:
    if not (repo / relative).is_file():
        add_violation(f"missing required evidence file: {relative}")


template_path = repo / "backend/src/WeCms.Api/appsettings.Production.example.json"
try:
    template = json.loads(template_path.read_text(encoding="utf-8"))
except (FileNotFoundError, json.JSONDecodeError):
    template = {}

if not template:
    add_violation("appsettings.Production.example.json is missing or invalid JSON")


# 1. FileStorage registration and LocalBasePath binding in runtime wiring
if "builder.Services.AddWeCmsFileStorage" not in program:
    add_violation("Program.cs must register AddWeCmsFileStorage")
if "new LocalFileStorage" not in file_storage_ext:
    add_violation("FileStorageExtensions.cs should bind LocalFileStorage runtime implementation")
if "FileStorage:Local:BasePath" not in file_storage_ext and "FileStorage:Local:BasePath" not in config_validator:
    add_violation("runtime must bind FileStorage:Local:BasePath from configuration")


# 2. IFileScanService registration and runtime wiring
if "AddSingleton<IFileScanService" not in system_files_ext:
    add_violation("SystemFilesServiceCollectionExtensions.cs must register IFileScanService")
if "CreateFileScanService(" not in program:
    add_violation("Program.cs should call CreateFileScanService to wire IFileScanService runtime implementation")
if "FileStorage:VirusScan" not in config_validator and "NoopFileScanService" not in system_files_ext:
    add_violation("runtime/file validator should recognize FileStorage virus scan configuration")


# 3. Forwarded headers wiring and documented fallback mode
if "AddWeCmsForwardedHeaders" not in program or "UseWeCmsForwardedHeaders" not in program:
    add_violation("Program.cs must register and use forwarded-headers extension")
if "KnownProxies" not in fwd_ext or "KnownNetworks" not in fwd_ext:
    add_violation("WeCmsForwardedHeadersExtensions should support known proxy/network topology validation")

forwarded_enabled = False
if template:
    security = template.get("Security", {}) if isinstance(template, dict) else {}
    forwarded = security.get("ForwardedHeaders", {}) if isinstance(security, dict) else {}
    if isinstance(forwarded, dict):
        forwarded_enabled = bool(forwarded.get("Enabled"))

if forwarded_enabled:
    if "Security:ForwardedHeaders" not in config_validator:
        add_violation("ProductionConfigurationValidator should validate forwarded-headers config")
else:
    docs_text = (deployment_proxy_doc + frontend_prod_doc + production_config_doc).lower()
    if "same-origin" not in docs_text and "no-proxy" not in docs_text:
        add_violation(
            "production docs do not document same-origin/no-proxy mode when forwarded headers are not enabled in template"
        )


# 4. CORS wiring and CORS/docs mode alignment
if "AddWeCmsCors" not in program or "UseCors(WeCmsCorsPolicyNames.AdminApi)" not in program:
    add_violation("Program.cs must register AddWeCmsCors and use matching CORS policy")
if "WithOrigins(origins)" not in cors_ext or "Security:AllowedOrigins" not in cors_ext:
    add_violation("WeCmsCorsExtensions should bind CORS policy to Security:AllowedOrigins")
if "AllowCredentials" not in cors_ext:
    add_violation("WeCmsCorsExtensions must allow credentials for API cookie flows")

allowed_origins = []
if template:
    security = template.get("Security", {}) if isinstance(template, dict) else {}
    allowed_origins = [item for item in security.get("AllowedOrigins", []) or [] if isinstance(item, str)]
    if not allowed_origins and "same-origin" not in frontend_prod_doc.lower():
        add_violation("Production template has empty Security:AllowedOrigins and no same-origin docs fallback")

if allowed_origins and request_ts and "VITE_API_BASE_URL" not in request_ts:
    add_violation("Frontend runtime should reference VITE_API_BASE_URL for split-domain deployment")


# 5. Production template keys must be consumed by code or explicitly documented
required_consumption = {
    "FileStorage:Provider": [file_storage_ext, config_validator],
    "FileStorage:Local:BasePath": [file_storage_ext, config_validator],
    "FileStorage:VirusScanEnabled": [program, config_validator, file_storage_doc],
    "FileStorage:VirusScan:Provider": [program, config_validator, file_storage_doc],
    "Security:ForwardedHeaders:Enabled": [fwd_ext, config_validator],
    "Security:ForwardedHeaders:KnownProxies": [fwd_ext, config_validator],
    "Security:ForwardedHeaders:KnownNetworks": [fwd_ext, config_validator],
    "Security:AllowedOrigins": [cors_ext, config_validator, production_config_doc],
    "FileStorage:PublicBaseUrl": [file_storage_doc, production_config_doc],
    "FileStorage:MaxUploadBytes": [file_storage_doc, production_config_doc],
    "FileStorage:AllowedMimeTypes": [file_storage_doc],
    "VITE_API_BASE_URL": [frontend_prod_doc, request_ts],
}

for key, evidences in required_consumption.items():
    if not any(key in text for text in evidences):
        add_violation(
            f"production template/runtime contract key {key} is neither wired in code nor documented for explicit docs-only usage"
        )

if violations:
    print("check-production-runtime-wiring: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-production-runtime-wiring: ok")
PY
