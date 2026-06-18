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

INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP(6)
FROM sys_role r
JOIN sys_permission p ON p.code IN (
  'sys:i18n:page',
  'sys:i18n:list',
  'sys:i18n:detail',
  'sys:i18n:create',
  'sys:i18n:update',
  'sys:i18n:delete',
  'account:i18n:switch'
)
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

