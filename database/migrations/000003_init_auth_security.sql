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
