ALTER TABLE sys_user
  ADD COLUMN avatar_object_key VARCHAR(255) NULL AFTER last_login_ip,
  ADD COLUMN avatar_mime_type VARCHAR(120) NULL AFTER avatar_object_key,
  ADD COLUMN avatar_file_ext VARCHAR(16) NULL AFTER avatar_mime_type,
  ADD COLUMN avatar_updated_at DATETIME(6) NULL AFTER avatar_file_ext;
