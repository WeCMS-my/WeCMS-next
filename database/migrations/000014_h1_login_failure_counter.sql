CREATE TABLE sys_login_failure_counter (
  id BIGINT NOT NULL AUTO_INCREMENT,
  scope VARCHAR(32) NOT NULL,
  target VARCHAR(128) NOT NULL,
  failure_count INT NOT NULL,
  window_started_at DATETIME(6) NOT NULL,
  last_failed_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY ux_sys_login_failure_counter_scope_target (scope, target),
  KEY ix_sys_login_failure_counter_updated_at (updated_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
