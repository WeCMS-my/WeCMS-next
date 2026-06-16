INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
SELECT 'sys:system:secure-ping', 'System secure ping', 'system', 'Allows access to the secure system ping endpoint.', 'enabled', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission WHERE code = 'sys:system:secure-ping'
);
