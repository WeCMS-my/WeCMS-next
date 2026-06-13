-- M0-BE-006 Migration 001: sys_user (identity table)
-- 创建用户表，包含通用字段

CREATE TABLE IF NOT EXISTS `sys_user` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `legacy_id` INT UNSIGNED NULL DEFAULT NULL COMMENT 'ThinkPHP think_admin.id',
    `username` VARCHAR(64) NOT NULL,
    `display_name` VARCHAR(128) NOT NULL DEFAULT '',
    `email` VARCHAR(256) NULL DEFAULT NULL,
    `phone` VARCHAR(32) NULL DEFAULT NULL,
    `avatar_file_id` BIGINT UNSIGNED NULL DEFAULT NULL,
    `password_hash` VARCHAR(512) NOT NULL DEFAULT '',
    `password_hash_algorithm` VARCHAR(32) NOT NULL DEFAULT 'pbkdf2-sha256',
    `status` TINYINT NOT NULL DEFAULT 1 COMMENT '0=禁用,1=正常',
    `security_stamp` CHAR(36) NOT NULL DEFAULT '' COMMENT '用于使token/重置密码失效',
    `permission_version` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '权限快照版本，角色变更时递增',
    `two_factor_enabled` TINYINT(1) NOT NULL DEFAULT 0,
    `two_factor_rebind_required` TINYINT(1) NOT NULL DEFAULT 0,
    `last_login_at` DATETIME(3) NULL DEFAULT NULL,
    `last_login_ip` VARCHAR(45) NULL DEFAULT NULL,
    `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
    `updated_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by` BIGINT UNSIGNED NULL DEFAULT NULL,
    `deleted_at` DATETIME(3) NULL DEFAULT NULL,
    `deleted_by` BIGINT UNSIGNED NULL DEFAULT NULL,
    `row_version` INT UNSIGNED NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_username` (`username`),
    UNIQUE KEY `uk_email` (`email`),
    KEY `idx_deleted_at` (`deleted_at`),
    KEY `idx_status` (`status`),
    KEY `idx_legacy_id` (`legacy_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='系统用户';
