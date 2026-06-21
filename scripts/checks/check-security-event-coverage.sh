#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
from pathlib import Path

repo = Path(sys.argv[1])

checks = {
    "cookie Origin rejection": [
        ("backend/src/WeCms.Modules.Identity/Services/CookieAuthOriginValidation.cs", "auth.cookie_origin_rejected"),
        ("backend/tests/WeCms.Tests.Unit/Auth/CookieAuthOriginValidatorTests.cs", "auth.cookie_origin_rejected"),
    ],
    "IP access rejection": [
        ("backend/src/WeCms.Api/Middleware/IpAccessControlMiddleware.cs", "security.ip_rejected"),
        ("backend/tests/WeCms.Tests.Unit/Api/IpAccessControlMiddlewareTests.cs", "security.ip_rejected"),
    ],
    "security ban hit": [
        ("backend/src/WeCms.Modules.Security/SecurityBanService.cs", "security.ban_hit"),
        ("backend/tests/WeCms.Tests.Unit/Security/SecurityBanServiceTests.cs", "security.ban_hit"),
    ],
    "login failure and rate limit": [
        ("backend/src/WeCms.Modules.Identity/Services/AuthService.cs", "auth.login_failed"),
        ("backend/src/WeCms.Modules.Identity/Services/LoginFailureLimiter.cs", "auth.login_rate_limited"),
        ("backend/tests/WeCms.Tests.Integration/Auth/AuthIntegrationTests.cs", "auth.login_rate_limited"),
    ],
    "2FA failure and replay": [
        ("backend/src/WeCms.Modules.Identity/Services/AuthTwoFactorChallengeService.cs", "auth.two_factor_failed"),
        ("backend/src/WeCms.Modules.Identity/Services/AuthTwoFactorChallengeService.cs", "auth.2fa_replay"),
        ("backend/tests/WeCms.Tests.Integration/Auth/AuthIntegrationTests.cs", "two_factor_replay"),
    ],
    "rate limit hit": [
        ("backend/src/WeCms.Api/RateLimiting/WeCmsRateLimitingExtensions.cs", "IRateLimitSecurityEventService"),
        ("backend/src/WeCms.Modules.Security/RateLimitRecords.cs", "rate_limit_hit"),
        ("backend/tests/WeCms.Tests.Integration/Security/RateLimitSecurityEventRepositoryTests.cs", "rate_limit_hit"),
    ],
    "file upload rejection": [
        ("backend/src/WeCms.Modules.FileCenter/Files/FileService.cs", "file_upload_rejected"),
        ("backend/tests/WeCms.Tests.Unit/Files/FileServiceTests.cs", "file_upload_rejected"),
        ("backend/tests/WeCms.Tests.Integration/Files/FileIntegrationTests.cs", "file_upload_rejected"),
    ],
    "permission denied": [
        ("backend/src/WeCms.Modules.AccessControl/Permissions/PermissionEndpointFilter.cs", "permission_denied"),
        ("backend/src/WeCms.Modules.AccessControl.SqlSugar/Repositories/PermissionSecurityEventRepository.cs", "Classify(record.EventType"),
        ("backend/tests/WeCms.Tests.Unit/Permissions/PermissionEndpointFilterTests.cs", "permission_denied"),
    ],
    "sensitive settings change": [
        ("backend/src/WeCms.Modules.Configuration/Settings/SettingService.cs", "security.setting_changed"),
        ("backend/tests/WeCms.Tests.Unit/Settings/SettingServiceTests.cs", "SecurityEventCount"),
    ],
    "high-risk 2FA reset": [
        ("backend/src/WeCms.Modules.Identity/Services/UserService.cs", "auth.user_2fa_reset"),
        ("backend/tests/WeCms.Tests.Unit/Users/UserServiceTests.cs", "auth.user_2fa_reset"),
    ],
    "security event classifier": [
        ("backend/src/WeCms.Shared/Security/SecurityEventClassifier.cs", "permission_denied"),
        ("backend/src/WeCms.Shared/Security/SecurityEventClassifier.cs", "file_upload_rejected"),
        ("backend/tests/WeCms.Tests.Unit/Security/SecurityEventClassifierTests.cs", "file_upload_rejected"),
    ],
}

violations: list[str] = []
for name, evidence in checks.items():
    for relative_path, token in evidence:
        path = repo / relative_path
        if not path.is_file():
            violations.append(f"{name}: missing evidence file {relative_path}")
            continue

        text = path.read_text(encoding="utf-8")
        if token not in text:
            violations.append(f"{name}: missing token {token!r} in {relative_path}")

if violations:
    raise SystemExit("check-security-event-coverage: " + "; ".join(violations))

print(f"check-security-event-coverage: ok ({len(checks)} security event areas checked)")
PY
