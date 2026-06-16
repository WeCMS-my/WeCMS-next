INSERT INTO sys_permission (code, name, module, description, created_at, updated_at)
SELECT 'sys:system:secure-ping', 'System secure ping', 'system', 'Allows access to the secure system ping endpoint.', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission WHERE code = 'sys:system:secure-ping'
);
