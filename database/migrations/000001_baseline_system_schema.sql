-- WeCMS system-foundation destructive-upgrade reset baseline.
-- Generated from the reviewed 000001..000019 migration chain for empty reset databases.

-- Historical migration segment
CREATE TABLE IF NOT EXISTS sys_schema_migration (
  version VARCHAR(64) NOT NULL,
  checksum CHAR(64) NOT NULL,
  applied_at DATETIME(6) NOT NULL,
  PRIMARY KEY (version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_user (
  id BIGINT NOT NULL AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL,
  display_name VARCHAR(120) NOT NULL,
  password_hash VARCHAR(512) NOT NULL,
  status VARCHAR(32) NOT NULL,
  is_super_admin BOOLEAN NOT NULL DEFAULT FALSE,
  last_login_at DATETIME(6) NULL,
  last_login_ip VARCHAR(64) NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_user_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_role (
  id BIGINT NOT NULL AUTO_INCREMENT,
  code VARCHAR(64) NOT NULL,
  name VARCHAR(120) NOT NULL,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_role_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_user_role (
  user_id BIGINT NOT NULL,
  role_id BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (user_id, role_id),
  CONSTRAINT fk_sys_user_role_user FOREIGN KEY (user_id) REFERENCES sys_user (id),
  CONSTRAINT fk_sys_user_role_role FOREIGN KEY (role_id) REFERENCES sys_role (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
CREATE TABLE sys_menu (
  id BIGINT NOT NULL AUTO_INCREMENT,
  parent_id BIGINT NULL,
  type VARCHAR(32) NOT NULL,
  name VARCHAR(120) NOT NULL,
  path VARCHAR(240) NOT NULL,
  component VARCHAR(240) NULL,
  title VARCHAR(120) NOT NULL,
  i18n_key VARCHAR(160) NULL,
  icon VARCHAR(120) NULL,
  sort INT NOT NULL DEFAULT 0,
  hidden BOOLEAN NOT NULL DEFAULT FALSE,
  keep_alive BOOLEAN NOT NULL DEFAULT FALSE,
  external_url VARCHAR(500) NULL,
  permission_code VARCHAR(160) NULL,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_menu_name (name),
  KEY ix_sys_menu_parent_id (parent_id),
  CONSTRAINT fk_sys_menu_parent FOREIGN KEY (parent_id) REFERENCES sys_menu (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_permission (
  id BIGINT NOT NULL AUTO_INCREMENT,
  code VARCHAR(160) NOT NULL,
  name VARCHAR(160) NOT NULL,
  module VARCHAR(64) NOT NULL,
  description VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_permission_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_role_permission (
  role_id BIGINT NOT NULL,
  permission_id BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (role_id, permission_id),
  CONSTRAINT fk_sys_role_permission_role FOREIGN KEY (role_id) REFERENCES sys_role (id),
  CONSTRAINT fk_sys_role_permission_permission FOREIGN KEY (permission_id) REFERENCES sys_permission (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
CREATE TABLE sys_refresh_token (
  id BIGINT NOT NULL AUTO_INCREMENT,
  user_id BIGINT NOT NULL,
  token_hash CHAR(64) NOT NULL,
  family_id CHAR(36) NOT NULL,
  expires_at DATETIME(6) NOT NULL,
  revoked_at DATETIME(6) NULL,
  replaced_by_token_hash CHAR(64) NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_refresh_token_hash (token_hash),
  KEY ix_sys_refresh_token_user_id (user_id),
  KEY ix_sys_refresh_token_family_id (family_id),
  CONSTRAINT fk_sys_refresh_token_user FOREIGN KEY (user_id) REFERENCES sys_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_login_log (
  id BIGINT NOT NULL AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL,
  user_id BIGINT NULL,
  ip VARCHAR(64) NULL,
  user_agent VARCHAR(500) NULL,
  result VARCHAR(32) NOT NULL,
  reason VARCHAR(160) NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_sys_login_log_username (username),
  KEY ix_sys_login_log_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_security_event (
  id BIGINT NOT NULL AUTO_INCREMENT,
  event_type VARCHAR(80) NOT NULL,
  user_id BIGINT NULL,
  username VARCHAR(64) NULL,
  ip VARCHAR(64) NULL,
  severity VARCHAR(32) NOT NULL,
  message VARCHAR(500) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_sys_security_event_type (event_type),
  KEY ix_sys_security_event_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
ALTER TABLE sys_user
  ADD COLUMN must_change_password BOOLEAN NOT NULL DEFAULT FALSE;


-- Historical migration segment
CREATE TABLE sys_audit_log (
  id BIGINT NOT NULL AUTO_INCREMENT,
  user_id BIGINT NULL,
  username VARCHAR(64) NULL,
  module VARCHAR(80) NOT NULL,
  resource VARCHAR(80) NOT NULL,
  action VARCHAR(80) NOT NULL,
  target_id VARCHAR(128) NULL,
  request_method VARCHAR(16) NOT NULL,
  request_path VARCHAR(160) NOT NULL,
  ip_address VARCHAR(64) NULL,
  user_agent VARCHAR(500) NULL,
  trace_id VARCHAR(64) NULL,
  result VARCHAR(32) NOT NULL,
  detail VARCHAR(500) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_sys_audit_log_user_id (user_id),
  KEY ix_sys_audit_log_created_at (created_at),
  KEY ix_sys_audit_log_module_resource_action (module, resource, action)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
ALTER TABLE sys_user
  ADD COLUMN dept_id BIGINT NULL AFTER is_super_admin,
  ADD COLUMN email VARCHAR(160) NULL AFTER display_name,
  ADD COLUMN phone VARCHAR(40) NULL AFTER email,
  ADD COLUMN security_stamp VARCHAR(64) NOT NULL DEFAULT '' AFTER must_change_password,
  ADD COLUMN permission_version BIGINT NOT NULL DEFAULT 0 AFTER security_stamp,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD UNIQUE KEY ux_sys_user_email (email),
  ADD UNIQUE KEY ux_sys_user_phone (phone),
  ADD KEY ix_sys_user_dept_id (dept_id),
  ADD KEY ix_sys_user_deleted_at (deleted_at);

CREATE TABLE sys_dept (
  id BIGINT NOT NULL AUTO_INCREMENT,
  parent_id BIGINT NULL,
  code VARCHAR(80) NOT NULL,
  name VARCHAR(120) NOT NULL,
  sort_order INT NOT NULL DEFAULT 0,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_dept_code (code),
  KEY ix_sys_dept_parent_id (parent_id),
  CONSTRAINT fk_sys_dept_parent FOREIGN KEY (parent_id) REFERENCES sys_dept (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_position (
  id BIGINT NOT NULL AUTO_INCREMENT,
  code VARCHAR(80) NOT NULL,
  name VARCHAR(120) NOT NULL,
  sort_order INT NOT NULL DEFAULT 0,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_position_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_user_position (
  user_id BIGINT NOT NULL,
  position_id BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (user_id, position_id),
  CONSTRAINT fk_sys_user_position_user FOREIGN KEY (user_id) REFERENCES sys_user (id),
  CONSTRAINT fk_sys_user_position_position FOREIGN KEY (position_id) REFERENCES sys_position (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE sys_user
  ADD CONSTRAINT fk_sys_user_dept FOREIGN KEY (dept_id) REFERENCES sys_dept (id);

-- Historical migration segment
ALTER TABLE sys_role
  ADD COLUMN is_builtin BOOLEAN NOT NULL DEFAULT FALSE AFTER status,
  ADD COLUMN is_locked BOOLEAN NOT NULL DEFAULT FALSE AFTER is_builtin,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD KEY ix_sys_role_deleted_at (deleted_at);

CREATE TABLE sys_role_menu (
  role_id BIGINT NOT NULL,
  menu_id BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (role_id, menu_id),
  CONSTRAINT fk_sys_role_menu_role FOREIGN KEY (role_id) REFERENCES sys_role (id),
  CONSTRAINT fk_sys_role_menu_menu FOREIGN KEY (menu_id) REFERENCES sys_menu (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
ALTER TABLE sys_menu
  ADD COLUMN is_builtin BOOLEAN NOT NULL DEFAULT FALSE AFTER status,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD KEY ix_sys_menu_deleted_at (deleted_at);

-- Historical migration segment
ALTER TABLE sys_permission
  ADD COLUMN status VARCHAR(32) NOT NULL DEFAULT 'enabled' AFTER description,
  ADD COLUMN is_builtin BOOLEAN NOT NULL DEFAULT FALSE AFTER status,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD KEY ix_sys_permission_deleted_at (deleted_at);

-- Historical migration segment
CREATE TABLE sys_dict_type (
  id BIGINT NOT NULL AUTO_INCREMENT,
  code VARCHAR(80) NOT NULL,
  name VARCHAR(120) NOT NULL,
  description VARCHAR(500) NULL,
  is_system BOOLEAN NOT NULL DEFAULT FALSE,
  status VARCHAR(32) NOT NULL,
  sort_order INT NOT NULL DEFAULT 0,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_dict_type_code (code),
  KEY ix_sys_dict_type_deleted_at (deleted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_dict_value (
  id BIGINT NOT NULL AUTO_INCREMENT,
  type_id BIGINT NOT NULL,
  label VARCHAR(120) NOT NULL,
  value VARCHAR(160) NOT NULL,
  description VARCHAR(500) NULL,
  sort_order INT NOT NULL DEFAULT 0,
  status VARCHAR(32) NOT NULL,
  is_default BOOLEAN NOT NULL DEFAULT FALSE,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_dict_value_type_value (type_id, value),
  KEY ix_sys_dict_value_type_id (type_id),
  KEY ix_sys_dict_value_deleted_at (deleted_at),
  CONSTRAINT fk_sys_dict_value_type FOREIGN KEY (type_id) REFERENCES sys_dict_type (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
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

-- Historical migration segment
CREATE TABLE sys_file (
  id BIGINT NOT NULL AUTO_INCREMENT,
  storage_provider VARCHAR(32) NOT NULL,
  bucket VARCHAR(80) NOT NULL,
  object_key VARCHAR(160) NOT NULL,
  original_name VARCHAR(255) NOT NULL,
  file_ext VARCHAR(16) NOT NULL,
  mime_type VARCHAR(120) NOT NULL,
  size_bytes BIGINT NOT NULL,
  sha256 CHAR(64) NOT NULL,
  status VARCHAR(32) NOT NULL,
  created_by BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_file_object_key (object_key),
  KEY ix_sys_file_created_by (created_by),
  KEY ix_sys_file_deleted_at (deleted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
CREATE TABLE sys_security_ban (
  id BIGINT NOT NULL AUTO_INCREMENT,
  ban_type VARCHAR(32) NOT NULL,
  target VARCHAR(128) NOT NULL,
  reason VARCHAR(500) NOT NULL,
  severity VARCHAR(32) NOT NULL,
  source VARCHAR(80) NOT NULL,
  expires_at DATETIME(6) NULL,
  revoked_at DATETIME(6) NULL,
  revoked_by BIGINT NULL,
  revoke_reason VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_sys_security_ban_lookup (ban_type, target, revoked_at, expires_at),
  KEY ix_sys_security_ban_revoked_by (revoked_by),
  CONSTRAINT fk_sys_security_ban_revoked_by FOREIGN KEY (revoked_by) REFERENCES sys_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
CREATE TABLE sys_login_failure_counter (
  id BIGINT NOT NULL AUTO_INCREMENT,
  scope VARCHAR(32) NOT NULL,
  target VARCHAR(128) NOT NULL,
  failure_count INT NOT NULL,
  window_started_at DATETIME(6) NOT NULL,
  last_failed_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_login_failure_counter_scope_target (scope, target),
  KEY ix_sys_login_failure_counter_updated_at (updated_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Historical migration segment
CREATE TABLE sys_user_two_factor (
  id BIGINT NOT NULL AUTO_INCREMENT,
  user_id BIGINT NOT NULL,
  enabled BOOLEAN NOT NULL DEFAULT FALSE,
  secret_cipher TEXT NOT NULL,
  confirmed_at DATETIME(6) NULL,
  last_totp_step BIGINT NULL,
  recovery_codes_hash_json JSON NOT NULL,
  recovery_codes_used_count INT NOT NULL DEFAULT 0,
  reset_required BOOLEAN NOT NULL DEFAULT FALSE,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_user_two_factor_user_id (user_id),
  CONSTRAINT fk_sys_user_two_factor_user FOREIGN KEY (user_id) REFERENCES sys_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
CREATE TABLE sys_auth_challenge (
  id BIGINT NOT NULL AUTO_INCREMENT,
  challenge_id CHAR(43) NOT NULL,
  user_id BIGINT NOT NULL,
  challenge_type VARCHAR(32) NOT NULL,
  status VARCHAR(32) NOT NULL,
  failed_attempts INT NOT NULL DEFAULT 0,
  expires_at DATETIME(6) NOT NULL,
  consumed_at DATETIME(6) NULL,
  ip VARCHAR(45) NULL,
  user_agent VARCHAR(500) NULL,
  trace_id VARCHAR(64) NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_auth_challenge_challenge_id (challenge_id),
  KEY ix_sys_auth_challenge_user_status (user_id, status, expires_at),
  CONSTRAINT fk_sys_auth_challenge_user FOREIGN KEY (user_id) REFERENCES sys_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Historical migration segment
ALTER TABLE sys_user
  ADD COLUMN avatar_object_key VARCHAR(255) NULL AFTER last_login_ip,
  ADD COLUMN avatar_mime_type VARCHAR(120) NULL AFTER avatar_object_key,
  ADD COLUMN avatar_file_ext VARCHAR(16) NULL AFTER avatar_mime_type,
  ADD COLUMN avatar_updated_at DATETIME(6) NULL AFTER avatar_file_ext;

-- Historical migration segment
CREATE TABLE sys_i18n_message (
  id BIGINT NOT NULL AUTO_INCREMENT,
  locale VARCHAR(16) NOT NULL,
  module VARCHAR(80) NOT NULL,
  message_key VARCHAR(160) NOT NULL,
  message_value TEXT NOT NULL,
  remark VARCHAR(500) NULL,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_sys_i18n_message_locale_key (locale, message_key),
  KEY ix_sys_i18n_message_locale_status (locale, status),
  KEY ix_sys_i18n_message_module (module),
  KEY ix_sys_i18n_message_deleted_at (deleted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- Historical migration segment
ALTER TABLE sys_security_event
  ADD COLUMN source VARCHAR(80) NOT NULL DEFAULT 'system' AFTER severity,
  ADD COLUMN trace_id VARCHAR(64) NOT NULL DEFAULT 'unknown' AFTER message,
  ADD KEY ix_sys_security_event_source (source),
  ADD KEY ix_sys_security_event_trace_id (trace_id);

-- Historical migration segment
CREATE TABLE sys_outbox_message (
  id BIGINT NOT NULL AUTO_INCREMENT,
  event_id CHAR(36) NOT NULL,
  event_type VARCHAR(160) NOT NULL,
  aggregate_type VARCHAR(120) NULL,
  aggregate_id VARCHAR(128) NULL,
  payload_json JSON NOT NULL,
  status VARCHAR(32) NOT NULL,
  retry_count INT NOT NULL DEFAULT 0,
  available_at DATETIME(6) NOT NULL,
  locked_at DATETIME(6) NULL,
  lock_token CHAR(36) NULL,
  processed_at DATETIME(6) NULL,
  error TEXT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_outbox_message_event_id (event_id),
  KEY ix_sys_outbox_message_status_available (status, available_at, locked_at),
  KEY ix_sys_outbox_message_event_type (event_type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
