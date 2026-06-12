# P1-4 JWT Token Version Validation Checklist

- [x] `securityStamp` mismatch rejects authenticated requests with HTTP 401.
- [x] `permissionVersion` mismatch rejects authenticated requests with HTTP 401.
- [x] disabled or deleted users reject authenticated requests with HTTP 401.
- [x] `/auth/me` and `/auth/logout` are protected by the same bearer validation path.
- [x] no runtime AI capability is introduced.
- [x] no module SQL or DB connector dependency is introduced.
- [x] no Service Locator is used in endpoint/business classes.
