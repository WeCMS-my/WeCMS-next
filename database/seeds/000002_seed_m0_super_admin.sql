-- M0-BE-006 Seed 002: Super admin user and role
-- 密码: Admin@123 (PBKDF2-SHA256, 600000 iterations)
-- 此文件仅作参考，实际密码hash由应用在运行时计算

-- Super admin role
INSERT IGNORE INTO `sys_role` (`code`, `name`, `description`, `is_system`, `is_builtin`, `sort_order`)
VALUES ('super_admin', '超级管理员', '系统内置超级管理员角色，拥有所有权限', 1, 1, 0);

-- Super admin user (password hash placeholder — 实际运行时由 DbMigrationRunner 计算)
INSERT IGNORE INTO `sys_user` (`username`, `display_name`, `password_hash`, `password_hash_algorithm`, `status`, `security_stamp`)
VALUES ('admin', '超级管理员', 'PLACEHOLDER_RUNTIME_HASH', 'pbkdf2-sha256', 1, UUID());

-- Assign super_admin role to admin user
INSERT IGNORE INTO `sys_user_role` (`user_id`, `role_id`)
SELECT u.id, r.id
FROM `sys_user` u, `sys_role` r
WHERE u.username = 'admin' AND r.code = 'super_admin';

-- Assign all permissions to super_admin role
INSERT IGNORE INTO `sys_role_permission` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `sys_role` r, `sys_permission` p
WHERE r.code = 'super_admin' AND p.status = 1;
