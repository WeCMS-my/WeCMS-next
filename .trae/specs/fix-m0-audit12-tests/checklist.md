# Checklist

## T1 — TokenService tests
- [x] GenerateTokenPair_ShouldReturnAccessAndRefreshTokens
- [x] ValidateAccessToken_ShouldReturnPrincipal_WhenTokenIsValid
- [x] ValidateAccessToken_ShouldReturnNull_WhenTokenIsTampered
- [x] GenerateTokenPair_ShouldIncludeClaims (incl IsSuperAdmin+PermissionVersion)
- [x] RefreshTokens_ShouldBeDifferentEachTime
- [x] ValidateAccessToken_ShouldReturnNull_WhenTokenIsInvalid

## T1 — AuthService tests
- [x] LoginAsync constructor wiring smoke test
- [x] LoginAsync_ShouldReturnNull_WhenUserNotFound
- [x] LogoutAsync_ShouldNotThrow
- [x] GetCurrentUserAsync_ShouldReturnNull_WhenUserNotFound
- [x] RefreshTokenAsync_ShouldReturnNull_WhenTokenNotFound

## T1 — PermissionEndpointFilter tests
- [x] ShouldAllowRequest_WhenNoPermissionMetadata
- [x] ShouldReturn401_WhenUserNotAuthenticated
- [x] ShouldAllowRequest_WhenSuperAdmin
- [x] ShouldReturn401_WhenInvalidTokenSub

## T2 — Integration test
- [x] Ping endpoint returns 200 + pong
- [x] Health live returns 200

## T3 — quality-gate.sh
- [x] 文件存在 `scripts/quality-gate.sh`
- [x] 包含 build/test/AOT publish/typecheck/build 5 步

## Bug fixes found during testing
- [x] TokenService: `DateTimeOffset.DateTime` → `.UtcDateTime` (DateTimeKind fix)
- [x] TokenService: `handler.MapInboundClaims = false` (claim mapping fix)

## 全量验证
- [x] dotnet build -warnaserror ✅
- [x] dotnet test ✅ (all tests pass including new ones)
- [x] dotnet publish /p:PublishAot=true ✅
