# Checklist: P0-3 LogoutRequest 语义一致性

- [x] `LogoutRequest.RefreshToken` 类型为 `string`（非 `string?`），确认不可空
  → `AuthDtos.cs:13` `public sealed record LogoutRequest(string RefreshToken);`
- [x] `/logout` handler 中有 `string.IsNullOrWhiteSpace(request.RefreshToken)` 空值校验，确认 fail-fast
  → `AuthEndpoints.cs:60-63` `if (string.IsNullOrWhiteSpace(request.RefreshToken)) throw new DomainException(...);`
- [x] OpenAPI `requestBody.required: true` 与 DTO 一致
  → `artifacts/openapi/wecms-api-v1.json:223` `"required": true`
- [x] OpenAPI schema 中 `required: ["refreshToken"]` 与 DTO 字段不可空一致
  → `artifacts/openapi/wecms-api-v1.json:233-235` `"required": ["refreshToken"]`
- [x] `quality-gate-backend.sh` 关键步骤已全部通过，无 regression
  → Step 1 (build): PASSED, Step 2 (AOT baseline): PASSED, Step 3 (AOT suppression): PASSED, Step 5 (Unit Tests): 44/44, Step 6 (OpenAPI auth check): PASSED, Step 7 (Architecture Tests): 15/15
