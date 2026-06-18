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

