INSERT INTO sys_menu (parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, is_builtin, created_at, updated_at, deleted_at)
SELECT root.id, 'menu', 'sys.i18n', '/system/i18n', 'system/i18n/index', 'i18n Messages', 'route.system.i18n', 'material-symbols:translate', 240, FALSE, FALSE, NULL, 'sys:i18n:page', 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
FROM sys_menu root
WHERE root.name = 'sys.system'
  AND NOT EXISTS (
    SELECT 1 FROM sys_menu m WHERE m.name = 'sys.i18n'
  );

