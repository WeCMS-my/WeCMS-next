-- WeCMS Next: Drop is_super_admin columns
-- Super admin is now determined by sys_role.code = 'super_admin' role assignment

ALTER TABLE sys_user DROP COLUMN is_super_admin;
ALTER TABLE sys_two_factor_ticket DROP COLUMN is_super_admin;
