INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT code, name, 'system', description, 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
FROM (
    SELECT 'sys:setting:validate-ip-rules' AS code, '设置 IP 规则校验' AS name, '校验系统设置 IP 规则' AS description UNION ALL
    SELECT 'sys:setting:reload-cache', '设置缓存刷新', '刷新系统设置缓存'
) p
WHERE NOT EXISTS (SELECT 1 FROM sys_permission existing WHERE existing.code = p.code);

INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP()
FROM sys_role r
JOIN sys_permission p ON p.code IN (
    'sys:setting:validate-ip-rules',
    'sys:setting:reload-cache'
)
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
