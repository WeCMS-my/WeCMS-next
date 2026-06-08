 -- WeCMS Next: Core system tables
 -- M0 minimal schema for auth + user + role + menu + permission
 
 create table sys_user (
   id bigint primary key auto_increment,
   username varchar(64) not null,
   display_name varchar(128) not null,
   email varchar(128) null,
   phone varchar(32) null,
   avatar_file_id bigint null,
   password_hash varchar(512) not null,
   password_hash_algorithm varchar(64) not null default 'pbkdf2-sha256',
   password_migrated_at datetime null,
   status varchar(32) not null default 'active',
   is_super_admin tinyint(1) not null default 0,
   security_stamp varchar(64) not null,
   permission_version bigint not null default 1,
   two_factor_enabled tinyint(1) not null default 0,
   two_factor_rebind_required tinyint(1) not null default 0,
   last_login_at datetime null,
   last_login_ip varchar(64) null,
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   deleted_at datetime null,
   deleted_by bigint null,
   row_version bigint not null default 1,
   legacy_id bigint null,
   unique key uk_sys_user_username (username),
   key ix_sys_user_status (status),
   key ix_sys_user_deleted (deleted_at)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_role (
   id bigint primary key auto_increment,
   code varchar(64) not null,
   name varchar(128) not null,
   description varchar(512) null,
   status varchar(32) not null default 'active',
   sort int not null default 0,
   data_scope varchar(32) not null default 'all',
   is_system tinyint(1) not null default 0,
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   deleted_at datetime null,
   legacy_id bigint null,
   unique key uk_sys_role_code (code),
   key ix_sys_role_deleted (deleted_at)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_user_role (
   user_id bigint not null,
   role_id bigint not null,
   created_at datetime not null default current_timestamp,
   primary key (user_id, role_id),
   key ix_sur_role_id (role_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_menu (
   id bigint primary key auto_increment,
   parent_id bigint null,
   type varchar(32) not null comment 'catalog, menu, button, link',
   name varchar(128) not null,
   path varchar(256) null,
   component varchar(256) null,
   title varchar(128) not null,
   i18n_key varchar(128) null,
   icon varchar(128) null,
   sort int not null default 0,
   hidden tinyint(1) not null default 0,
   keep_alive tinyint(1) not null default 0,
   external_url varchar(512) null,
   permission_code varchar(128) null,
   status varchar(32) not null default 'active',
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   deleted_at datetime null,
   legacy_id bigint null,
   legacy_rule_name varchar(256) null,
   key ix_sys_menu_parent_sort (parent_id, sort),
   key ix_sys_menu_deleted (deleted_at)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_permission (
   id bigint primary key auto_increment,
   code varchar(128) not null,
   name varchar(128) not null,
   module varchar(64) not null,
   resource varchar(64) not null,
   action varchar(64) not null,
   http_method varchar(16) null,
   route_pattern varchar(256) null,
   status varchar(32) not null default 'active',
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   legacy_id bigint null,
   legacy_rule_name varchar(256) null,
   unique key uk_sys_permission_code (code)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_role_menu (
   role_id bigint not null,
   menu_id bigint not null,
   primary key (role_id, menu_id),
   key ix_srm_menu_id (menu_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_role_permission (
   role_id bigint not null,
   permission_id bigint not null,
   primary key (role_id, permission_id),
   key ix_srp_permission_id (permission_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_refresh_token (
   id bigint primary key auto_increment,
   user_id bigint not null,
   token_hash varchar(256) not null,
   family_id varchar(64) not null,
   expires_at datetime not null,
   revoked_at datetime null,
   replaced_by_token_id bigint null,
   created_ip varchar(64) null,
   user_agent varchar(512) null,
   created_at datetime not null default current_timestamp,
   key ix_srt_user_id (user_id),
   key ix_srt_token_hash (token_hash(64)),
   key ix_srt_family_id (family_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_login_log (
   id bigint primary key auto_increment,
   user_id bigint null,
   username varchar(64) not null,
   login_type varchar(32) not null default 'password',
   status varchar(32) not null comment 'success, fail, locked, 2fa_required',
   ip varchar(64) null,
   user_agent varchar(512) null,
   fail_reason varchar(256) null,
   created_at datetime not null default current_timestamp,
   key ix_sll_user_id (user_id),
   key ix_sll_created (created_at),
   key ix_sll_status (status)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_security_event (
   id bigint primary key auto_increment,
   event_type varchar(64) not null,
   severity varchar(16) not null default 'info',
   user_id bigint null,
   username varchar(64) null,
   ip varchar(64) null,
   user_agent varchar(512) null,
   detail varchar(1024) null,
   created_at datetime not null default current_timestamp,
   key ix_sse_type (event_type),
   key ix_sse_created (created_at),
   key ix_sse_user (user_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
