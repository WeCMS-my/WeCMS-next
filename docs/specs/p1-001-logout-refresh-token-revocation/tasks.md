# P1-001 Logout Refresh-Token Revocation Tasks

1. Add a change-specific spec trio for the logout authorization decision.
2. Update auth endpoint regression tests so logout is expected to be anonymous while `me` remains authorized.
3. Update OpenAPI export tests so logout keeps a required request body but no longer advertises bearer-auth security.
4. Update endpoint and OpenAPI metadata implementations to match the selected logout model.
5. Run focused auth/OpenAPI tests, then run the backend quality gate and code-review audit before closing the task.
