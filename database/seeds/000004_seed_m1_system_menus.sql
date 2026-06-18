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
  SELECT 'menu', 'sys.posts', '/system/posts', 'system/posts/index', 'Posts', 'route.system.posts', 'material-symbols:badge', 160, 'sys:post:page' UNION ALL
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
