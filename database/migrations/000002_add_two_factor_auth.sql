 -- M1: Add 2FA columns + password reset + session tracking
 
 alter table sys_user
   add column two_factor_temp_secret varchar(256) null after two_factor_rebind_required,
   add column two_factor_temp_codes text null after two_factor_temp_secret,
   add column two_factor_secret varchar(256) null after two_factor_temp_codes,
   add column two_factor_backup_codes text null after two_factor_secret,
   add column two_factor_confirmed_at datetime null after two_factor_backup_codes,
   add column two_factor_last_used_ts bigint not null default 0 after two_factor_confirmed_at;
 
 create table sys_user_session (
   id bigint primary key auto_increment,
   user_id bigint not null,
   refresh_token_hash varchar(256) not null,
   ip varchar(64) null,
   user_agent varchar(512) null,
   created_at datetime not null default current_timestamp,
   expires_at datetime not null,
   revoked_at datetime null,
   key ix_sus_user_id (user_id),
   key ix_sus_token_hash (refresh_token_hash(64))
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table sys_password_reset_token (
   id bigint primary key auto_increment,
   user_id bigint not null,
   token_hash varchar(256) not null,
   expires_at datetime not null,
   used_at datetime null,
   created_at datetime not null default current_timestamp,
   key ix_sprt_user_id (user_id),
   key ix_sprt_token_hash (token_hash(64))
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
