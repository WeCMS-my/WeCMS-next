-- M0-BE-006 Seed 001: Base permission codes for M0-BE endpoints

INSERT IGNORE INTO `sys_permission` (`code`, `name`, `module`, `resource`, `action`, `http_method`, `route_pattern`, `is_system`)
VALUES
('sys:system:secure-ping', '安全探针', 'sys', 'system', 'secure-ping', 'GET', '/api/v1/system/secure-ping', 1);
