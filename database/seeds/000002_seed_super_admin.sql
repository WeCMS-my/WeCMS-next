INSERT INTO sys_role (code, name, status, created_at, updated_at)
SELECT 'super_admin', 'Super Administrator', 'enabled', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role WHERE code = 'super_admin'
);

INSERT INTO sys_user (username, display_name, password_hash, status, is_super_admin, must_change_password, created_at, updated_at)
SELECT 'admin', 'Administrator', '{{ADMIN_PASSWORD_HASH}}', 'enabled', TRUE, {{ADMIN_MUST_CHANGE_PASSWORD}}, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
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

INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP(6)
FROM sys_role r
JOIN sys_permission p ON p.code = 'sys:system:secure-ping'
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
    SELECT 1
    FROM sys_role_permission rp
    WHERE rp.role_id = r.id
      AND rp.permission_id = p.id
  );
