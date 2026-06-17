# Checklist

- [x] `LoginResponse` has no `refreshToken` field.
- [x] `/api/v1/auth/refresh` has no request body.
- [x] `/api/v1/auth/logout` has no request body.
- [x] Login and refresh append `__Host-wecms_refresh`.
- [x] Logout deletes `__Host-wecms_refresh`.
- [x] Frontend does not store tokens in `localStorage` or `sessionStorage`.
- [x] Frontend refresh/logout sends credentials and no refresh-token JSON.
- [x] OpenAPI artifact and generated frontend types match backend contracts.
- [x] No MVC Controller, EF Core, dynamic query, SQL boundary, or AI runtime change is introduced.
