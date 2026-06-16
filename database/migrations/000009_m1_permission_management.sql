ALTER TABLE sys_permission
  ADD COLUMN status VARCHAR(32) NOT NULL DEFAULT 'enabled' AFTER description,
  ADD COLUMN is_builtin BOOLEAN NOT NULL DEFAULT FALSE AFTER status,
  ADD COLUMN deleted_at DATETIME(6) NULL AFTER updated_at,
  ADD KEY ix_sys_permission_deleted_at (deleted_at);
