#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])
violations: list[str] = []

required_files = [
    "docs/specs/ph4-file-storage/spec.md",
    "docs/specs/ph4-file-storage/tasks.md",
    "docs/specs/ph4-file-storage/checklist.md",
    "docs/ops/file-storage-production.md",
    "docs/adr/production-file-storage-provider.md",
    "backend/src/WeCms.Shared/FileStorage.cs",
    "backend/src/WeCms.Infrastructure/Files/LocalFileStorage.cs",
    "backend/src/WeCms.Infrastructure/Files/ClamAvFileScanService.cs",
    "backend/tests/WeCms.Tests.Unit/Files/LocalFileStorageTests.cs",
    "backend/tests/WeCms.Tests.Unit/Files/ClamAvFileScanServiceTests.cs",
]

for relative in required_files:
    if not (repo / relative).is_file():
        violations.append(f"missing {relative}")

def read(relative: str) -> str:
    path = repo / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""

shared = read("backend/src/WeCms.Shared/FileStorage.cs")
for token in ["ExistsAsync", "GetMetadataAsync", "FileStorageMetadata", "IFileScanService", "FileScanRequest", "FileScanResult"]:
    if token not in shared:
        violations.append(f"FileStorage shared contract missing {token}")

local_storage = read("backend/src/WeCms.Infrastructure/Files/LocalFileStorage.cs")
for token in ["LocalFileStorage(string basePath)", "Path.GetFullPath(basePath)", "IsUnderBasePath", "NoopFileScanService", "FileScanResult.CleanResult"]:
    if token not in local_storage:
        violations.append(f"LocalFileStorage missing {token}")
if "Microsoft.Extensions" in local_storage:
    violations.append("LocalFileStorage must not add Microsoft.Extensions dependencies to Infrastructure")

clamav = read("backend/src/WeCms.Infrastructure/Files/ClamAvFileScanService.cs")
for token in ["ClamAvFileScanService", "ClamAvFileScanOptions", "zINSTREAM", "WriteStreamChunksAsync", " FOUND", "FileScanResult.CleanResult"]:
    if token not in clamav:
        violations.append(f"ClamAV scanner missing {token}")
if "Microsoft.Extensions" in clamav:
    violations.append("ClamAvFileScanService must not add Microsoft.Extensions dependencies to Infrastructure")

program = read("backend/src/WeCms.Api/Program.cs")
file_storage_ext = read("backend/src/WeCms.Api/Extensions/FileStorageExtensions.cs")
for token in [
    "AddWeCmsFileStorage",
    "AddWeCmsSystemFiles",
    "CreateFileScanService",
    "IFileScanService",
]:
    if token not in program:
        violations.append(f"Program.cs missing FileStorage registration token {token}")

for token in ["FileStorage:Local:BasePath"]:
    if token not in file_storage_ext:
        violations.append(f"FileStorageExtensions.cs missing FileStorage runtime token {token}")

validator = read("backend/src/WeCms.Api/Configuration/ProductionConfigurationValidator.cs")
for token in [
    "RequireFileStorage",
    "FileStorage:Provider",
    "FileStorage:Local:BasePath",
    "Path.IsPathFullyQualified",
    "Directory.Exists",
    "EnsureWritableDirectory",
    "wwwroot",
    "FileStorage:VirusScanEnabled",
    "FileStorage:VirusScan:Provider",
    "FileStorage:VirusScan:Host",
    "clamav-tcp",
]:
    if token not in validator:
        violations.append(f"ProductionConfigurationValidator missing {token}")

file_service = read("backend/src/WeCms.Modules.System/Files/FileService.cs")
for token in ["IFileScanService", "ScanAsync(file", "FileScanRequest", "file scan rejected uploaded content", "file_upload_rejected"]:
    if token not in file_service:
        violations.append(f"FileService missing scanner/security token {token}")

account_profile = read("backend/src/WeCms.Modules.System/Auth/AccountProfileService.cs")
for token in ["IFileScanService", "ScanAvatarAsync", "FileScanRequest", "Avatar scan rejected uploaded content", "file_upload_rejected"]:
    if token not in account_profile:
        violations.append(f"AccountProfileService missing scanner token {token}")

tests = (
    read("backend/tests/WeCms.Tests.Unit/Files/LocalFileStorageTests.cs")
    + read("backend/tests/WeCms.Tests.Unit/Files/ClamAvFileScanServiceTests.cs")
    + read("backend/tests/WeCms.Tests.Unit/Files/FileServiceTests.cs")
    + read("backend/tests/WeCms.Tests.Unit/Auth/AccountProfileServiceTests.cs")
    + read("backend/tests/WeCms.Tests.Unit/Configuration/ProductionConfigurationValidatorTests.cs")
)
for token in [
    "StoreAsync_UsesConfiguredBasePathAndExposesMetadata",
    "StoreAsync_RejectsPathTraversal",
    "CreateAsync_RejectsWhenFileScannerRejectsContentAndWritesSecurityEvent",
    "UploadAvatarAsync_RejectsWhenFileScannerRejectsContent",
    "Validate_ProductionRejectsMissingFileStorageBasePath",
    "Validate_ProductionRejectsVirusScanEnabledWithNoopScanner",
    "Validate_ProductionAllowsVirusScanEnabledWithClamAvProvider",
    "ScanAsync_ReturnsClean_WhenClamAvReportsOk",
    "ScanAsync_ReturnsRejected_WhenClamAvReportsFound",
]:
    if token not in tests:
        violations.append(f"PH-4 tests missing {token}")

production_template = read("backend/src/WeCms.Api/appsettings.Production.example.json")
for token in ['"FileStorage"', '"Provider": "local"', '"BasePath": "__SET_BY_ENV__"', '"VirusScanEnabled": false', '"Provider": "clamav-tcp"', '"Host": "scanner.internal"']:
    if token not in production_template:
        violations.append(f"production template missing {token}")

docs = (
    read("docs/ops/file-storage-production.md")
    + read("docs/adr/production-file-storage-provider.md")
    + read("docs/ops/production-configuration.md")
)
for token in ["local", "s3-compatible", "NoopFileScanService", "ClamAvFileScanService", "clamav-tcp", "VirusScanEnabled", "path traversal", "web root"]:
    if token not in docs:
        violations.append(f"PH-4 docs missing {token}")

for forbidden in ["AWSSDK", "AmazonS3", "Aliyun.OSS", "Cloudflare", "Minio"]:
    if forbidden in read("backend/src/WeCms.Api/WeCms.Api.csproj") + read("backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj"):
        violations.append(f"PH-4 must not introduce cloud storage SDK {forbidden}")

if violations:
    print("check-file-storage-production: failed", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    raise SystemExit(1)

print("check-file-storage-production: ok")
PY
