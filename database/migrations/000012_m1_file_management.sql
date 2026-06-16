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
