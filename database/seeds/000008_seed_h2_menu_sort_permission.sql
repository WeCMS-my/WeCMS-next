INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT 'sys:menu:sort', '菜单批量排序', 'system', '批量调整系统菜单父级与排序', 'enabled', TRUE, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE code = 'sys:menu:sort');

INSERT INTO sys_role_permission (role_id, permission_id, created_at)
SELECT r.id, p.id, UTC_TIMESTAMP()
FROM sys_role r
JOIN sys_permission p ON p.code = 'sys:menu:sort'
WHERE r.code = 'super_admin'
  AND NOT EXISTS (
      SELECT 1
      FROM sys_role_permission rp
      WHERE rp.role_id = r.id
        AND rp.permission_id = p.id
  );
