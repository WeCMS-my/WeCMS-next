 -- WeCMS Next: Base seed data
 -- Creates super admin, core roles, and base permissions
 
 -- ============================================================
 -- Roles
 -- ============================================================
 insert into sys_role (code, name, description, status, sort, data_scope, is_system)
 values
   ('super_admin', 'Super Admin', 'System super administrator', 'active', 1, 'all', 1),
   ('admin', 'Admin', 'System administrator', 'active', 2, 'all', 1),
   ('editor', 'Editor', 'Content editor', 'active', 3, 'self', 0);
 
 -- ============================================================
 -- Super admin user (password: Admin@123)
 -- Hash: PBKDF2-SHA256, salt will differ at runtime
 -- Use placeholder hash that will be replaced by registration script
 -- ============================================================
 -- IMPORTANT: This seed SQL provides schema structure only.
 -- The actual password hash must be generated at runtime by the
 -- application during first setup, using IPasswordHasher.
 -- 
 -- For development: run the dev-init script which calls the API
 -- to create the super admin with a proper password hash.
 
 -- ============================================================
 -- Permissions (M0 minimal set)
 -- ============================================================
 insert into sys_permission (code, name, module, resource, action, status)
 values
   ('sys:user:list', 'View users', 'system', 'user', 'list', 'active'),
   ('sys:user:create', 'Create user', 'system', 'user', 'create', 'active'),
   ('sys:user:update', 'Update user', 'system', 'user', 'update', 'active'),
   ('sys:user:delete', 'Delete user', 'system', 'user', 'delete', 'active'),
   ('sys:role:list', 'View roles', 'system', 'role', 'list', 'active'),
   ('sys:role:create', 'Create role', 'system', 'role', 'create', 'active'),
   ('sys:role:update', 'Update role', 'system', 'role', 'update', 'active'),
   ('sys:role:delete', 'Delete role', 'system', 'role', 'delete', 'active'),
   ('sys:menu:list', 'View menus', 'system', 'menu', 'list', 'active'),
   ('sys:menu:create', 'Create menu', 'system', 'menu', 'create', 'active'),
   ('sys:menu:update', 'Update menu', 'system', 'menu', 'update', 'active'),
   ('sys:menu:delete', 'Delete menu', 'system', 'menu', 'delete', 'active'),
   ('sys:permission:list', 'View permissions', 'system', 'permission', 'list', 'active');
 
 -- ============================================================
 -- Menus (M0 minimal: just System Management root)
 -- ============================================================
 insert into sys_menu (parent_id, type, name, path, component, title, icon, sort, status)
 values
   (null, 'catalog', 'system', '/system', null, 'System', 'mdi:cog-outline', 1, 'active'),
   (1, 'menu', 'system_user', '/system/user', 'views/system/user/index', 'User Management', 'mdi:account-group-outline', 1, 'active'),
   (1, 'menu', 'system_role', '/system/role', 'views/system/role/index', 'Role Management', 'mdi:shield-account-outline', 2, 'active'),
   (1, 'menu', 'system_menu', '/system/menu', 'views/system/menu/index', 'Menu Management', 'mdi:menu-open', 3, 'active'),
   (1, 'menu', 'system_permission', '/system/permission', 'views/system/permission/index', 'Permission Management', 'mdi:shield-key-outline', 4, 'active');
