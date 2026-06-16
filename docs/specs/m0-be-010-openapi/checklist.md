# M0-BE-010 OpenAPI Checklist

- [x] `POST /api/v1/auth/login` has `requestBody`.
- [x] `POST /api/v1/auth/refresh` has `requestBody`.
- [x] `POST /api/v1/auth/logout` has `requestBody`.
- [x] Auth response schema exists.
- [x] System API paths exist.
- [x] `secure-ping` security metadata exists.
- [x] `secure-ping` permission metadata exists.
- [x] `scripts/checks/check-openapi-auth-request-body.sh` passes.
- [x] `scripts/checks/check-openapi-endpoint-coverage.sh` passes.
