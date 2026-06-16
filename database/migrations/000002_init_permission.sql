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
