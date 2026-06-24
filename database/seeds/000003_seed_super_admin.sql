-- WeCMS system-foundation super-admin seed baseline.
-- Runs after 000002_seed_system_permissions.sql so super_admin receives every current permission.

-- Historical seed segment
INSERT INTO sys_role (code, name, status, is_builtin, is_locked, created_at, updated_at, deleted_at)
SELECT 'super_admin', 'Super Administrator', 'enabled', TRUE, TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role WHERE code = 'super_admin'
);

UPDATE sys_role
SET status = 'enabled',
    is_builtin = TRUE,
    is_locked = TRUE,
    updated_at = UTC_TIMESTAMP(6),
    deleted_at = NULL
WHERE code = 'super_admin';

INSERT INTO sys_user (username, display_name, password_hash, status, must_change_password, created_at, updated_at)
SELECT 'admin', 'Administrator', '{{ADMIN_PASSWORD_HASH}}', 'enabled', {{ADMIN_MUST_CHANGE_PASSWORD}}, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_user WHERE username = 'admin'
);

INSERT INTO sys_user_role (user_id, role_id, created_at)
SELECT u.id, r.id, UTC_TIMESTAMP(6)
FROM sys_user u
JOIN sys_role r ON r.code = 'super_admin'
WHERE u.username = 'admin'
  AND NOT EXISTS (
    SELECT 1
    FROM sys_user_role ur
    WHERE ur.user_id = u.id
      AND ur.role_id = r.id
  );


-- Historical seed segment
INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP(6)
FROM sys_role r
JOIN sys_permission p
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
    SELECT 1
    FROM sys_role_permission rp
    WHERE rp.role_id = r.id
      AND rp.permission_id = p.id
  );

INSERT INTO sys_role_menu (role_id, menu_id, created_at)
SELECT r.id, m.id, UTC_TIMESTAMP(6)
FROM sys_role r
CROSS JOIN sys_menu m
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
    SELECT 1
    FROM sys_role_menu rm
    WHERE rm.role_id = r.id
      AND rm.menu_id = m.id
  );
