# Spec: p01-access-profile-cache

## Goal

Reduce repeated `/api/v1/auth/me`, login, and refresh-token access profile database work by caching composed `AccessProfileDto` values behind the existing `permission_version` invalidation model.

## Scope

- Cache only roles, permissions, buttons, and menu tree profile data.
- Keep user identity/status loading outside this cache.
- Keep `WeCms.Modules.AccessControl` free of infrastructure cache dependencies.
- Register the cache as an API composition-root decorator for `IAccessProfileService`.

## Non-Goals

- Do not change public HTTP contracts, OpenAPI response shapes, permission codes, menus, or token claims.
- Do not add Redis or distributed cache behavior.
- Do not replace `permission_version` bump behavior.
- Do not optimize menu SQL parent-chain querying in this task.

## Acceptance

- Same `userId + permissionVersion + superAdmin flag` calls reuse cached `AccessProfileDto`.
- Permission version changes cause a cache miss and rebuild.
- Cache hit still checks current `permission_version`.
- AccessControl module dependency matrix remains valid.
- Existing Auth consumers continue to depend on `IAccessProfileService`.
