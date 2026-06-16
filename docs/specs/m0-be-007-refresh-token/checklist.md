# M0-BE-007 Refresh Token Rotation Checklist

- [x] Refresh token is stored only as SHA-256 hash.
- [x] Successful refresh creates a new refresh token.
- [x] Successful refresh revokes the old refresh token.
- [x] Old revoke and new insert run in one transaction.
- [x] Reusing a revoked token revokes the whole family.
- [x] Expired token returns 401.
- [x] Disabled user returns 401.
- [x] Concurrent refresh has exactly one success.
- [x] Security events are recorded for denied/reuse cases.
