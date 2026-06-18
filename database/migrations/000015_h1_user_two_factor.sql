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
