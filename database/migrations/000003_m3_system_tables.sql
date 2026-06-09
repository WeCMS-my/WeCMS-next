 -- M3: Settings, Dicts, Audit log tables
 
 create table if not exists sys_setting (
   id bigint primary key auto_increment,
   `key` varchar(128) not null,
   `value` text null,
   `group` varchar(64) null,
   description varchar(256) null,
   is_sensitive tinyint(1) not null default 0,
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   unique key uk_sys_setting_key (`key`)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table if not exists sys_dict_type (
   id bigint primary key auto_increment,
   code varchar(64) not null,
   name varchar(128) not null,
   status varchar(32) not null default 'active',
   sort int not null default 0,
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   deleted_at datetime null,
   unique key uk_sdt_code (code)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table if not exists sys_dict_value (
   id bigint primary key auto_increment,
   type_id bigint not null,
   code varchar(64) not null,
   name varchar(128) not null,
   `value` varchar(512) null,
   sort int not null default 0,
   status varchar(32) not null default 'active',
   created_at datetime not null default current_timestamp,
   updated_at datetime not null default current_timestamp on update current_timestamp,
   deleted_at datetime null,
   key ix_sdv_type_id (type_id)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
 
 create table if not exists sys_audit_log (
   id bigint primary key auto_increment,
   trace_id varchar(64) null,
   user_id bigint null,
   username varchar(64) null,
   permission_code varchar(128) null,
   module varchar(64) null,
   action varchar(128) null,
   http_method varchar(16) null,
   path varchar(256) null,
   query_string varchar(512) null,
   ip varchar(64) null,
   user_agent varchar(512) null,
   request_body_summary varchar(1024) null,
   status_code int null,
   elapsed_ms bigint null,
   result varchar(256) null,
   error_message varchar(1024) null,
   created_at datetime not null default current_timestamp,
   key ix_sal_user_id (user_id),
   key ix_sal_created (created_at),
   key ix_sal_module (module)
 ) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
