# P1-001 Logout Refresh-Token Revocation Checklist

- [ ] `/api/v1/auth/logout` is `AllowAnonymous()`.
- [ ] `/api/v1/auth/logout` still requires `LogoutRequest` request body metadata.
- [ ] OpenAPI export keeps `/api/v1/auth/logout` requestBody.
- [ ] OpenAPI export no longer marks `/api/v1/auth/logout` with `bearerAuth`.
- [ ] `/api/v1/auth/me` still requires authorization.
- [ ] Regression tests fail if logout is changed back to `RequireAuthorization()`.
- [ ] No MVC, EF Core, dynamic, SQL drift, or AI runtime code is introduced.
