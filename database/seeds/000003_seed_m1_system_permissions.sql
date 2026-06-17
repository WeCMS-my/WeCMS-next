INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT v.code, v.name, 'system', v.description, 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
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
  SELECT 'sys:user:assign-role', 'User assign role', 'Assign roles to users' UNION ALL
  SELECT 'sys:user:assign-post', 'User assign post', 'Assign posts to users' UNION ALL
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
  SELECT 'sys:post:page', 'Post page', 'Access post management page' UNION ALL
  SELECT 'sys:post:list', 'Post list', 'List posts' UNION ALL
  SELECT 'sys:post:detail', 'Post detail', 'View post details' UNION ALL
  SELECT 'sys:post:create', 'Post create', 'Create posts' UNION ALL
  SELECT 'sys:post:update', 'Post update', 'Update posts' UNION ALL
  SELECT 'sys:post:delete', 'Post delete', 'Delete posts' UNION ALL
  SELECT 'sys:post:enable', 'Post enable', 'Enable posts' UNION ALL
  SELECT 'sys:post:disable', 'Post disable', 'Disable posts' UNION ALL
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
