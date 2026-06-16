# M0-BE-007 Refresh Token Rotation Tasks

1. Extend auth service and DTOs with refresh behavior.
2. Extend auth repository with refresh-token lookup, revoke, family revoke, insert, and security-event methods.
3. Wire `/api/v1/auth/refresh` endpoint to the service.
4. Add unit tests for expired, revoked reuse, disabled user, and plaintext-hash behavior.
5. Add MySQL integration tests for successful rotation, token reuse family revocation, expired token, disabled user, and concurrent refresh.
6. Run focused tests, full backend gate, and task audit.
