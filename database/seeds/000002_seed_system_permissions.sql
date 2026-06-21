-- WeCMS system-foundation permissions and menu seed baseline.
-- Super-admin user, secrets, and role grants live in 000003_seed_super_admin.sql.

-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT 'sys:system:secure-ping', 'System secure ping', 'system', 'Allows access to the secure system ping endpoint.', 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission WHERE code = 'sys:system:secure-ping'
);

-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT v.code, v.name, CASE WHEN v.code LIKE 'sys:user:%' THEN 'identity' ELSE 'system' END, v.description, 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
FROM (
  SELECT 'sys:user:page' AS code, 'User page' AS name, 'Access user management page' AS description UNION ALL
  SELECT 'sys:user:list', 'User list', 'List users' UNION ALL
  SELECT 'sys:user:detail', 'User detail', 'View user details' UNION ALL
  SELECT 'sys:user:create', 'User create', 'Create users' UNION ALL
  SELECT 'sys:user:update', 'User update', 'Update users' UNION ALL
  SELECT 'sys:user:delete', 'User delete', 'Delete users' UNION ALL
  SELECT 'sys:user:enable', 'User enable', 'Enable users' UNION ALL
  SELECT 'sys:user:disable', 'User disable', 'Disable users' UNION ALL
  SELECT 'sys:user:reset-password', 'User reset password', 'Reset user passwords' UNION ALL
  SELECT 'sys:user:reset-2fa', 'User reset 2FA', 'Reset user two-factor authentication' UNION ALL
  SELECT 'sys:user:assign-role', 'User assign role', 'Assign roles to users' UNION ALL
  SELECT 'sys:user:assign-position', 'User assign position', 'Assign positions to users' UNION ALL
  SELECT 'sys:role:page', 'Role page', 'Access role management page' UNION ALL
  SELECT 'sys:role:list', 'Role list', 'List roles' UNION ALL
  SELECT 'sys:role:detail', 'Role detail', 'View role details' UNION ALL
  SELECT 'sys:role:create', 'Role create', 'Create roles' UNION ALL
  SELECT 'sys:role:update', 'Role update', 'Update roles' UNION ALL
  SELECT 'sys:role:delete', 'Role delete', 'Delete roles' UNION ALL
  SELECT 'sys:role:enable', 'Role enable', 'Enable roles' UNION ALL
  SELECT 'sys:role:disable', 'Role disable', 'Disable roles' UNION ALL
  SELECT 'sys:role:assign-permission', 'Role assign permission', 'Assign permissions to roles' UNION ALL
  SELECT 'sys:role:assign-menu', 'Role assign menu', 'Assign menus to roles' UNION ALL
  SELECT 'sys:menu:page', 'Menu page', 'Access menu management page' UNION ALL
  SELECT 'sys:menu:list', 'Menu list', 'List menus' UNION ALL
  SELECT 'sys:menu:tree', 'Menu tree', 'View menu tree' UNION ALL
  SELECT 'sys:menu:detail', 'Menu detail', 'View menu details' UNION ALL
  SELECT 'sys:menu:create', 'Menu create', 'Create menus' UNION ALL
  SELECT 'sys:menu:update', 'Menu update', 'Update menus' UNION ALL
  SELECT 'sys:menu:delete', 'Menu delete', 'Delete menus' UNION ALL
  SELECT 'sys:menu:enable', 'Menu enable', 'Enable menus' UNION ALL
  SELECT 'sys:menu:disable', 'Menu disable', 'Disable menus' UNION ALL
  SELECT 'sys:permission:page', 'Permission page', 'Access permission management page' UNION ALL
  SELECT 'sys:permission:list', 'Permission list', 'List permissions' UNION ALL
  SELECT 'sys:permission:tree', 'Permission tree', 'View permission tree' UNION ALL
  SELECT 'sys:permission:detail', 'Permission detail', 'View permission details' UNION ALL
  SELECT 'sys:permission:create', 'Permission create', 'Create permissions' UNION ALL
  SELECT 'sys:permission:update', 'Permission update', 'Update permissions' UNION ALL
  SELECT 'sys:permission:delete', 'Permission delete', 'Delete permissions' UNION ALL
  SELECT 'sys:permission:enable', 'Permission enable', 'Enable permissions' UNION ALL
  SELECT 'sys:permission:disable', 'Permission disable', 'Disable permissions' UNION ALL
  SELECT 'sys:dept:page', 'Department page', 'Access department management page' UNION ALL
  SELECT 'sys:dept:list', 'Department list', 'List departments' UNION ALL
  SELECT 'sys:dept:tree', 'Department tree', 'View department tree' UNION ALL
  SELECT 'sys:dept:detail', 'Department detail', 'View department details' UNION ALL
  SELECT 'sys:dept:create', 'Department create', 'Create departments' UNION ALL
  SELECT 'sys:dept:update', 'Department update', 'Update departments' UNION ALL
  SELECT 'sys:dept:delete', 'Department delete', 'Delete departments' UNION ALL
  SELECT 'sys:dept:enable', 'Department enable', 'Enable departments' UNION ALL
  SELECT 'sys:dept:disable', 'Department disable', 'Disable departments' UNION ALL
  SELECT 'sys:position:page', 'Position page', 'Access position management page' UNION ALL
  SELECT 'sys:position:list', 'Position list', 'List positions' UNION ALL
  SELECT 'sys:position:detail', 'Position detail', 'View position details' UNION ALL
  SELECT 'sys:position:create', 'Position create', 'Create positions' UNION ALL
  SELECT 'sys:position:update', 'Position update', 'Update positions' UNION ALL
  SELECT 'sys:position:delete', 'Position delete', 'Delete positions' UNION ALL
  SELECT 'sys:position:enable', 'Position enable', 'Enable positions' UNION ALL
  SELECT 'sys:position:disable', 'Position disable', 'Disable positions' UNION ALL
  SELECT 'sys:dict:page', 'Dictionary page', 'Access dictionary management page' UNION ALL
  SELECT 'sys:dict:type:list', 'Dictionary type list', 'List dictionary types' UNION ALL
  SELECT 'sys:dict:type:create', 'Dictionary type create', 'Create dictionary types' UNION ALL
  SELECT 'sys:dict:type:update', 'Dictionary type update', 'Update dictionary types' UNION ALL
  SELECT 'sys:dict:type:delete', 'Dictionary type delete', 'Delete dictionary types' UNION ALL
  SELECT 'sys:dict:value:list', 'Dictionary value list', 'List dictionary values' UNION ALL
  SELECT 'sys:dict:value:create', 'Dictionary value create', 'Create dictionary values' UNION ALL
  SELECT 'sys:dict:value:update', 'Dictionary value update', 'Update dictionary values' UNION ALL
  SELECT 'sys:dict:value:delete', 'Dictionary value delete', 'Delete dictionary values' UNION ALL
  SELECT 'sys:setting:page', 'Setting page', 'Access setting management page' UNION ALL
  SELECT 'sys:setting:list', 'Setting list', 'List settings' UNION ALL
  SELECT 'sys:setting:detail', 'Setting detail', 'View setting details' UNION ALL
  SELECT 'sys:setting:update', 'Setting update', 'Update settings' UNION ALL
  SELECT 'sys:login-log:page', 'Login log page', 'Access login log page' UNION ALL
  SELECT 'sys:login-log:list', 'Login log list', 'List login logs' UNION ALL
  SELECT 'sys:login-log:detail', 'Login log detail', 'View login log details' UNION ALL
  SELECT 'sys:audit-log:page', 'Audit log page', 'Access audit log page' UNION ALL
  SELECT 'sys:audit-log:list', 'Audit log list', 'List audit logs' UNION ALL
  SELECT 'sys:audit-log:detail', 'Audit log detail', 'View audit log details' UNION ALL
  SELECT 'sys:security:page', 'Security center page', 'Access security center page' UNION ALL
  SELECT 'sys:security:status', 'Security status', 'View security status' UNION ALL
  SELECT 'sys:security:ban:list', 'Security ban list', 'List security bans' UNION ALL
  SELECT 'sys:security:ban:detail', 'Security ban detail', 'View security ban details' UNION ALL
  SELECT 'sys:security:ban:unban', 'Security ban unban', 'Unban security bans' UNION ALL
  SELECT 'sys:security:ban:batch-unban', 'Security ban batch unban', 'Batch unban security bans' UNION ALL
  SELECT 'sys:security-event:page', 'Security event page', 'Access security event page' UNION ALL
  SELECT 'sys:security-event:list', 'Security event list', 'List security events' UNION ALL
  SELECT 'sys:security-event:detail', 'Security event detail', 'View security event details' UNION ALL
  SELECT 'sys:file:page', 'File page', 'Access file management page' UNION ALL
  SELECT 'sys:file:list', 'File list', 'List files' UNION ALL
  SELECT 'sys:file:detail', 'File detail', 'View file details' UNION ALL
  SELECT 'sys:file:upload', 'File upload', 'Upload file metadata' UNION ALL
  SELECT 'sys:file:download', 'File download', 'Download and preview files' UNION ALL
  SELECT 'sys:file:delete', 'File delete', 'Delete files'
) v
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p.code = v.code
);

-- Historical seed segment
INSERT INTO sys_menu (parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, is_builtin, created_at, updated_at, deleted_at)
SELECT NULL, 'catalog', 'sys.system', '/system', 'layout.base', 'System Management', 'route.system', 'material-symbols:settings', 100, FALSE, FALSE, NULL, NULL, 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
WHERE NOT EXISTS (
  SELECT 1 FROM sys_menu WHERE name = 'sys.system'
);

INSERT INTO sys_menu (parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, is_builtin, created_at, updated_at, deleted_at)
SELECT root.id, v.type, v.name, v.path, v.component, v.title, v.i18n_key, v.icon, v.sort, FALSE, FALSE, NULL, v.permission_code, 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
FROM sys_menu root
JOIN (
  SELECT 'menu' AS type, 'sys.users' AS name, '/system/users' AS path, 'system/users/index' AS component, 'Users' AS title, 'route.system.users' AS i18n_key, 'material-symbols:group' AS icon, 110 AS sort, 'sys:user:page' AS permission_code UNION ALL
  SELECT 'menu', 'sys.roles', '/system/roles', 'system/roles/index', 'Roles', 'route.system.roles', 'material-symbols:admin-panel-settings', 120, 'sys:role:page' UNION ALL
  SELECT 'menu', 'sys.menus', '/system/menus', 'system/menus/index', 'Menus', 'route.system.menus', 'material-symbols:menu', 130, 'sys:menu:page' UNION ALL
  SELECT 'menu', 'sys.permissions', '/system/permissions', 'system/permissions/index', 'Permissions', 'route.system.permissions', 'material-symbols:key', 140, 'sys:permission:page' UNION ALL
  SELECT 'menu', 'sys.departments', '/system/departments', 'system/departments/index', 'Departments', 'route.system.departments', 'material-symbols:account-tree', 150, 'sys:dept:page' UNION ALL
  SELECT 'menu', 'sys.positions', '/system/positions', 'system/positions/index', 'Positions', 'route.system.positions', 'material-symbols:badge', 160, 'sys:position:page' UNION ALL
  SELECT 'menu', 'sys.dicts', '/system/dicts', 'system/dicts/index', 'Dictionaries', 'route.system.dicts', 'material-symbols:format-list-bulleted', 170, 'sys:dict:page' UNION ALL
  SELECT 'menu', 'sys.settings', '/system/settings', 'system/settings/index', 'Settings', 'route.system.settings', 'material-symbols:tune', 180, 'sys:setting:page' UNION ALL
  SELECT 'menu', 'sys.loginLogs', '/system/login-logs', 'system/login-logs/index', 'Login Logs', 'route.system.loginLogs', 'material-symbols:login', 190, 'sys:login-log:page' UNION ALL
  SELECT 'menu', 'sys.auditLogs', '/system/audit-logs', 'system/audit-logs/index', 'Audit Logs', 'route.system.auditLogs', 'material-symbols:fact-check', 200, 'sys:audit-log:page' UNION ALL
  SELECT 'menu', 'sys.security', '/system/security', 'system/security/index', 'Security Center', 'route.system.security', 'material-symbols:security', 210, 'sys:security:page' UNION ALL
  SELECT 'menu', 'sys.securityEvents', '/system/security-events', 'system/security-events/index', 'Security Events', 'route.system.securityEvents', 'material-symbols:shield', 220, 'sys:security-event:page' UNION ALL
  SELECT 'menu', 'sys.files', '/system/files', 'system/files/index', 'Files', 'route.system.files', 'material-symbols:folder', 230, 'sys:file:page'
) v ON root.name = 'sys.system'
WHERE NOT EXISTS (
  SELECT 1 FROM sys_menu m WHERE m.name = v.name
);

-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT v.code, v.name, 'system', v.description, 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
FROM (
  SELECT 'sys:i18n:page' AS code, 'i18n page' AS name, 'Access i18n message management page' AS description UNION ALL
  SELECT 'sys:i18n:list', 'i18n list', 'List i18n messages' UNION ALL
  SELECT 'sys:i18n:detail', 'i18n detail', 'View i18n message details' UNION ALL
  SELECT 'sys:i18n:create', 'i18n create', 'Create i18n messages' UNION ALL
  SELECT 'sys:i18n:update', 'i18n update', 'Update i18n messages' UNION ALL
  SELECT 'sys:i18n:delete', 'i18n delete', 'Delete i18n messages' UNION ALL
  SELECT 'account:i18n:switch', 'Account i18n switch', 'Switch own account locale'
) v
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p.code = v.code
);


-- Historical seed segment
INSERT INTO sys_menu (parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, is_builtin, created_at, updated_at, deleted_at)
SELECT root.id, 'menu', 'sys.i18n', '/system/i18n', 'system/i18n/index', 'i18n Messages', 'route.system.i18n', 'material-symbols:translate', 240, FALSE, FALSE, NULL, 'sys:i18n:page', 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
FROM sys_menu root
WHERE root.name = 'sys.system'
  AND NOT EXISTS (
    SELECT 1 FROM sys_menu m WHERE m.name = 'sys.i18n'
  );


-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT 'sys:menu:sort', '菜单批量排序', 'system', '批量调整系统菜单父级与排序', 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE code = 'sys:menu:sort');


-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT code, name, 'system', description, 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
FROM (
    SELECT 'sys:dict:type:enable' AS code, '字典类型启用' AS name, '启用字典类型' AS description UNION ALL
    SELECT 'sys:dict:type:disable', '字典类型禁用', '禁用字典类型' UNION ALL
    SELECT 'sys:dict:value:enable', '字典值启用', '启用字典值' UNION ALL
    SELECT 'sys:dict:value:disable', '字典值禁用', '禁用字典值'
) p
WHERE NOT EXISTS (SELECT 1 FROM sys_permission existing WHERE existing.code = p.code);


-- Historical seed segment
INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT code, name, 'system', description, 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
FROM (
    SELECT 'sys:setting:validate-ip-rules' AS code, '设置 IP 规则校验' AS name, '校验系统设置 IP 规则' AS description UNION ALL
    SELECT 'sys:setting:reload-cache', '设置缓存刷新', '刷新系统设置缓存'
) p
WHERE NOT EXISTS (SELECT 1 FROM sys_permission existing WHERE existing.code = p.code);


-- Historical seed segment
UPDATE sys_permission
SET module = 'identity',
    updated_at = UTC_TIMESTAMP(6)
WHERE code LIKE 'sys:user:%'
  AND module <> 'identity';
