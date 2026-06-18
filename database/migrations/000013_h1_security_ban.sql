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
