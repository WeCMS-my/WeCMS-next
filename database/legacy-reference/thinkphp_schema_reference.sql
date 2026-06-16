-- WeCMS M0-BE ThinkPHP schema reference.
-- This file is reference-only. It is not a migration, seed, or compatibility script.
-- Do not execute it as part of backend initialization.
-- Source: docs/context/WeCMS_ThinkPHP_系统详细说明文档.md

-- Legacy user account reference.
-- New target: sys_user.
-- M0-BE decision: no legacy user import and no old password hash compatibility.
CREATE TABLE think_admin_reference (
  id BIGINT,
  username VARCHAR(64),
  password_hash VARCHAR(512),
  nickname VARCHAR(120),
  groupid VARCHAR(255),
  status VARCHAR(32),
  token VARCHAR(512),
  token_expire_at DATETIME,
  twofa_secret TEXT,
  twofa_backup_codes TEXT,
  created_at DATETIME,
  updated_at DATETIME
);

-- Legacy role/group reference.
-- New target: sys_role.
-- M0-BE decision: role data is re-seeded, not migrated.
CREATE TABLE think_auth_group_reference (
  id BIGINT,
  title VARCHAR(120),
  status VARCHAR(32),
  rules TEXT,
  created_at DATETIME,
  updated_at DATETIME
);

-- Legacy user-role relation reference.
-- New target: sys_user_role.
-- M0-BE decision: normalized relation is initialized from new seed data only.
CREATE TABLE think_auth_group_access_reference (
  uid BIGINT,
  group_id BIGINT
);

-- Legacy mixed menu and permission rule reference.
-- New target: sys_menu plus sys_permission.
-- M0-BE decision: dynamic URL matching is replaced by explicit permission codes.
CREATE TABLE think_auth_rule_reference (
  id BIGINT,
  name VARCHAR(240),
  title VARCHAR(120),
  type VARCHAR(32),
  status VARCHAR(32),
  css VARCHAR(120),
  condition_expression TEXT,
  pid BIGINT,
  sort INT,
  lang_code VARCHAR(160)
);

-- Legacy configuration reference.
-- Future target: sys_setting.
-- M0-BE decision: settings are deferred; old config and encrypted secrets are not imported.
CREATE TABLE think_config_reference (
  id BIGINT,
  name VARCHAR(160),
  title VARCHAR(160),
  value TEXT,
  type VARCHAR(64),
  group_name VARCHAR(120),
  sort INT,
  created_at DATETIME,
  updated_at DATETIME
);
