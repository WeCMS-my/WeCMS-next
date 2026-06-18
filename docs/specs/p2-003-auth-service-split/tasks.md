1. Add spec-owned internal auth split targets and register them in DI.
2. Extract auth audit writing into `AuthAuditWriter`.
3. Extract auth security-event writing into `AuthSecurityEventWriter`.
4. Extract refresh rotation flow into `RefreshTokenRotationService`.
5. Extract logout revocation flow into `LogoutTokenRevoker`.
6. Refactor `AuthService` to orchestrate and delegate without changing public behavior.
7. Update unit tests/fakes to exercise the delegated services through `AuthService`.
8. Run task-scoped auth unit/integration tests and backend quality gate.
9. Audit for unchanged endpoint contracts and replay/logout semantics.

