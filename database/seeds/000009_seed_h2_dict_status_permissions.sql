INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT code, name, 'system', description, 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
FROM (
    SELECT 'sys:dict:type:enable' AS code, '字典类型启用' AS name, '启用字典类型' AS description UNION ALL
    SELECT 'sys:dict:type:disable', '字典类型禁用', '禁用字典类型' UNION ALL
    SELECT 'sys:dict:value:enable', '字典值启用', '启用字典值' UNION ALL
    SELECT 'sys:dict:value:disable', '字典值禁用', '禁用字典值'
) p
WHERE NOT EXISTS (SELECT 1 FROM sys_permission existing WHERE existing.code = p.code);

INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP()
FROM sys_role r
JOIN sys_permission p ON p.code IN (
    'sys:dict:type:enable',
    'sys:dict:type:disable',
    'sys:dict:value:enable',
    'sys:dict:value:disable'
)
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
