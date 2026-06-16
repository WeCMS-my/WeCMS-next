CREATE TABLE sys_setting (
  id BIGINT NOT NULL AUTO_INCREMENT,
  `key` VARCHAR(120) NOT NULL,
  `value` TEXT NULL,
  value_type VARCHAR(32) NOT NULL,
  group_code VARCHAR(80) NOT NULL,
  name VARCHAR(120) NOT NULL,
  description VARCHAR(500) NULL,
  is_sensitive BOOLEAN NOT NULL DEFAULT FALSE,
  is_system BOOLEAN NOT NULL DEFAULT FALSE,
  updated_at DATETIME(6) NOT NULL,
  updated_by BIGINT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_setting_key (`key`),
  KEY ix_sys_setting_group_code (group_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO sys_setting (`key`, `value`, value_type, group_code, name, description, is_sensitive, is_system, updated_at, updated_by)
VALUES
  ('site.name', 'WeCMS Next', 'string', 'site', 'Site Name', 'Public site name', FALSE, TRUE, UTC_TIMESTAMP(6), NULL),
  ('security.passwordPepper', '', 'string', 'security', 'Password Pepper', 'Sensitive password pepper placeholder', TRUE, TRUE, UTC_TIMESTAMP(6), NULL);
