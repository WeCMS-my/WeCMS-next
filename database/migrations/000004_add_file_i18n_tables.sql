-- M3: File storage and I18n message tables

create table if not exists sys_file (
  id bigint primary key auto_increment,
  original_name varchar(512) not null,
  storage_name varchar(256) not null,
  storage_path varchar(1024) not null,
  size bigint not null,
  mime_type varchar(128) not null,
  extension varchar(32) not null,
  created_at datetime not null default current_timestamp,
  updated_at datetime not null default current_timestamp on update current_timestamp,
  deleted_at datetime null,
  deleted_by bigint null,
  key ix_sys_file_deleted (deleted_at)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;

create table if not exists sys_i18n_message (
  id bigint primary key auto_increment,
  locale varchar(16) not null,
  message_key varchar(256) not null,
  message_value text null,
  remark varchar(512) null,
  created_at datetime not null default current_timestamp,
  updated_at datetime not null default current_timestamp on update current_timestamp,
  deleted_at datetime null,
  key ix_sim_locale_key (locale, message_key),
  key ix_sim_deleted (deleted_at)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
