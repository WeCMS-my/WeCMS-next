# Tasks

- [x] Task 1: T1 — TokenService 单元测试（6 tests）✅ + 发现并修复 2 个 bug
- [x] Task 2: T1 — AuthService 单元测试（5 tests, Moq） ✅
- [x] Task 3: T1 — PermissionEndpointFilter 单元测试（4 tests）✅
- [x] Task 4: T2 — Integration tests 骨架（2 endpoint tests）✅
- [x] Task 5: T3 — `scripts/quality-gate.sh` ✅
- [x] Task 6: 全量验证 ✅ build/test/AOT 全通过

## Bug fixes discovered
- TokenService used `DateTimeOffset.DateTime` causing `DateTimeKind.Unspecified` → fixed to `.UtcDateTime`
- TokenService missing `handler.MapInboundClaims = false` → claims invisible on read-back

# Dependencies
全部完成。
