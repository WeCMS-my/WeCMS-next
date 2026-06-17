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
