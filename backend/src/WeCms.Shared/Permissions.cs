 namespace WeCms.Shared;
 
 public static class Permissions
 {
     public const string SystemUserList = "sys:user:list";
     public const string SystemUserCreate = "sys:user:create";
     public const string SystemUserUpdate = "sys:user:update";
     public const string SystemUserDelete = "sys:user:delete";
     public const string SystemRoleList = "sys:role:list";
     public const string SystemRoleCreate = "sys:role:create";
     public const string SystemRoleUpdate = "sys:role:update";
     public const string SystemRoleDelete = "sys:role:delete";
     public const string SystemRoleAssignMenu = "sys:role:assign-menu";
     public const string SystemRoleAssignPermission = "sys:role:assign-permission";
     public const string SystemMenuList = "sys:menu:list";
     public const string SystemMenuCreate = "sys:menu:create";
     public const string SystemMenuUpdate = "sys:menu:update";
     public const string SystemMenuDelete = "sys:menu:delete";
     public const string SystemMenuSort = "sys:menu:sort";
     public const string SystemPermissionList = "sys:permission:list";
     public const string SystemPermissionSync = "sys:permission:sync";

    // Dict
    public const string SystemDictList = "sys:dict:list";
    public const string SystemDictCreate = "sys:dict:create";
    public const string SystemDictDelete = "sys:dict:delete";

    // File
    public const string SystemFileList = "sys:file:list";
    public const string SystemFileUpload = "sys:file:upload";
    public const string SystemFileDownload = "sys:file:download";
    public const string SystemFileDelete = "sys:file:delete";

    // Setting
    public const string SystemSettingList = "sys:setting:list";
    public const string SystemSettingUpdate = "sys:setting:update";

    // Log
    public const string SystemLogLoginList = "sys:log:login:list";
    public const string SystemLogAuditList = "sys:log:audit:list";

    // Security
    public const string SystemSecurityEventList = "sys:security:event:list";

    // I18n
    public const string SystemI18nList = "sys:i18n:list";
    public const string SystemI18nCreate = "sys:i18n:create";
    public const string SystemI18nUpdate = "sys:i18n:update";
    public const string SystemI18nDelete = "sys:i18n:delete";
}
