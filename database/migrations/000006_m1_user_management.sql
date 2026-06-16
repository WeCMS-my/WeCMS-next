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

CREATE TABLE sys_post (
  id BIGINT NOT NULL AUTO_INCREMENT,
  code VARCHAR(80) NOT NULL,
  name VARCHAR(120) NOT NULL,
  sort_order INT NOT NULL DEFAULT 0,
  status VARCHAR(32) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  deleted_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_post_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE sys_user_post (
  user_id BIGINT NOT NULL,
  post_id BIGINT NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (user_id, post_id),
  CONSTRAINT fk_sys_user_post_user FOREIGN KEY (user_id) REFERENCES sys_user (id),
  CONSTRAINT fk_sys_user_post_post FOREIGN KEY (post_id) REFERENCES sys_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE sys_user
  ADD CONSTRAINT fk_sys_user_dept FOREIGN KEY (dept_id) REFERENCES sys_dept (id);
