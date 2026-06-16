ALTER TABLE sys_menu
  ADD COLUMN is_builtin BOOLEAN NOT NULL DEFAULT FALSE AFTER status,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD KEY ix_sys_menu_deleted_at (deleted_at);
