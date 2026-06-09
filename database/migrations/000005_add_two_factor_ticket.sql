-- M1: Two-factor ticket storage moved from in-memory ConcurrentDictionary to MySQL

create table sys_two_factor_ticket (
  id bigint primary key auto_increment,
  ticket varchar(128) not null unique,
  user_id bigint not null,
  username varchar(64) not null,
  security_stamp varchar(64) not null,
  permission_version bigint not null,
  expires_at datetime not null,
  created_at datetime not null default current_timestamp,
  key ix_stt_expires (expires_at),
  key ix_stt_ticket (ticket)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
