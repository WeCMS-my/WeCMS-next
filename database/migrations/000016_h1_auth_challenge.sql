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
