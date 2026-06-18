using System.Text.Json.Nodes;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Security;
using WeCms.Modules.System.Settings;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static JsonObject ManagementSchemas()
    {
        return new JsonObject
        {
            ["PagedRoleSummary"] = PagedSchema(nameof(RoleSummaryDto)),
            [nameof(RoleSummaryDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "code", "name", "status", "isBuiltin", "isLocked", "createdAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["code"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["status"] = StringSchema(),
                    ["isBuiltin"] = BooleanSchema(),
                    ["isLocked"] = BooleanSchema(),
                    ["createdAt"] = DateTimeSchema()
                }
            },
            [nameof(RoleDetailDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "code", "name", "status", "isBuiltin", "isLocked", "permissionIds", "menuIds", "createdAt", "updatedAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["code"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["status"] = StringSchema(),
                    ["isBuiltin"] = BooleanSchema(),
                    ["isLocked"] = BooleanSchema(),
                    ["permissionIds"] = ArrayOf(IntegerSchema()),
                    ["menuIds"] = ArrayOf(IntegerSchema()),
                    ["createdAt"] = DateTimeSchema(),
                    ["updatedAt"] = DateTimeSchema()
                }
            },
            [nameof(CreateRoleRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("code", "name"),
                ["properties"] = new JsonObject
                {
                    ["code"] = StringSchema(),
                    ["name"] = StringSchema(),
                    ["permissionIds"] = NullableArrayOf(IntegerSchema()),
                    ["menuIds"] = NullableArrayOf(IntegerSchema())
                }
            },
            [nameof(UpdateRoleRequest)] = ObjectSchema(("name", "string")),
            [nameof(AssignRolePermissionsRequest)] = IdListSchema("permissionIds"),
            [nameof(AssignRoleMenusRequest)] = IdListSchema("menuIds"),
            [nameof(RoleMutationResponse)] = ObjectSchema(("id", "integer")),
            ["MenuSummaryList"] = ArrayOf(SchemaRef(nameof(MenuSummaryDto))),
            ["MenuTreeList"] = ArrayOf(SchemaRef(nameof(MenuTreeDto))),
            [nameof(MenuSummaryDto)] = MenuSchema(includeChildren: false, includeTimestamps: false),
            [nameof(MenuTreeDto)] = MenuSchema(includeChildren: true, includeTimestamps: false),
            [nameof(MenuDetailDto)] = MenuSchema(includeChildren: false, includeTimestamps: true),
            [nameof(CreateMenuRequest)] = MenuMutationSchema(includeCode: true),
            [nameof(UpdateMenuRequest)] = MenuMutationSchema(includeCode: false),
            [nameof(MenuMutationResponse)] = ObjectSchema(("id", "integer")),
            ["PermissionSummaryList"] = ArrayOf(SchemaRef(nameof(PermissionSummaryDto))),
            ["PermissionTreeList"] = ArrayOf(SchemaRef(nameof(PermissionTreeDto))),
            [nameof(PermissionSummaryDto)] = PermissionSchema(includeTimestamps: false),
            [nameof(PermissionTreeDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("module", "permissions"),
                ["properties"] = new JsonObject
                {
                    ["module"] = StringSchema(),
                    ["permissions"] = ArrayOf(SchemaRef(nameof(PermissionSummaryDto)))
                }
            },
            [nameof(PermissionDetailDto)] = PermissionSchema(includeTimestamps: true),
            [nameof(CreatePermissionRequest)] = PermissionMutationSchema(includeCode: true),
            [nameof(UpdatePermissionRequest)] = PermissionMutationSchema(includeCode: false),
            [nameof(PermissionMutationResponse)] = ObjectSchema(("id", "integer")),
            ["DepartmentSummaryList"] = ArrayOf(SchemaRef(nameof(DepartmentSummaryDto))),
            ["DepartmentTreeList"] = ArrayOf(SchemaRef(nameof(DepartmentTreeDto))),
            [nameof(DepartmentSummaryDto)] = DepartmentSchema(includeChildren: false, includeTimestamps: false),
            [nameof(DepartmentTreeDto)] = DepartmentSchema(includeChildren: true, includeTimestamps: false),
            [nameof(DepartmentDetailDto)] = DepartmentSchema(includeChildren: false, includeTimestamps: true),
            [nameof(CreateDepartmentRequest)] = DepartmentMutationSchema(includeCode: true),
            [nameof(UpdateDepartmentRequest)] = DepartmentMutationSchema(includeCode: false),
            [nameof(DepartmentMutationResponse)] = ObjectSchema(("id", "integer")),
            ["PagedPostSummary"] = PagedSchema(nameof(PostSummaryDto)),
            [nameof(PostSummaryDto)] = PostSchema(includeTimestamps: false),
            [nameof(PostDetailDto)] = PostSchema(includeTimestamps: true),
            [nameof(CreatePostRequest)] = PostMutationSchema(includeCode: true),
            [nameof(UpdatePostRequest)] = PostMutationSchema(includeCode: false),
            [nameof(PostMutationResponse)] = ObjectSchema(("id", "integer")),
            ["PagedDictTypeSummary"] = PagedSchema(nameof(DictTypeSummaryDto)),
            [nameof(DictTypeSummaryDto)] = DictTypeSchema(includeTimestamps: false),
            [nameof(DictTypeDetailDto)] = DictTypeSchema(includeTimestamps: true),
            [nameof(CreateDictTypeRequest)] = DictTypeMutationSchema(includeCode: true),
            [nameof(UpdateDictTypeRequest)] = DictTypeMutationSchema(includeCode: false),
            ["DictValueList"] = ArrayOf(SchemaRef(nameof(DictValueDto))),
            [nameof(DictValueDto)] = DictValueSchema(),
            [nameof(CreateDictValueRequest)] = DictValueMutationSchema(),
            [nameof(UpdateDictValueRequest)] = DictValueMutationSchema(),
            [nameof(DictMutationResponse)] = ObjectSchema(("id", "integer")),
            ["PagedSettingSummary"] = PagedSchema(nameof(SettingSummaryDto)),
            [nameof(SettingSummaryDto)] = SettingSchema(),
            [nameof(SettingDetailDto)] = SettingSchema(),
            [nameof(UpdateSettingRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject { ["value"] = NullableStringSchema() }
            },
            [nameof(SettingMutationResponse)] = ObjectSchema(("key", "string")),
            ["PagedLoginLogSummary"] = PagedSchema(nameof(LoginLogSummaryDto)),
            [nameof(LoginLogSummaryDto)] = LoginLogSchema(includeUserAgent: false),
            [nameof(LoginLogDetailDto)] = LoginLogSchema(includeUserAgent: true),
            ["PagedAuditLogSummary"] = PagedSchema(nameof(AuditLogSummaryDto)),
            [nameof(AuditLogSummaryDto)] = AuditLogSchema(includeRequest: false),
            [nameof(AuditLogDetailDto)] = AuditLogSchema(includeRequest: true),
            [nameof(SecurityStatusDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("activeBans", "activeIpBans", "activeUserBans", "criticalActiveBans", "generatedAt"),
                ["properties"] = new JsonObject
                {
                    ["activeBans"] = IntegerSchema(),
                    ["activeIpBans"] = IntegerSchema(),
                    ["activeUserBans"] = IntegerSchema(),
                    ["criticalActiveBans"] = IntegerSchema(),
                    ["generatedAt"] = DateTimeSchema()
                }
            },
            ["PagedSecurityBanSummary"] = PagedSchema(nameof(SecurityBanSummaryDto)),
            [nameof(SecurityBanSummaryDto)] = SecurityBanSchema(includeRevokeDetail: false),
            [nameof(SecurityBanDetailDto)] = SecurityBanSchema(includeRevokeDetail: true),
            [nameof(UnbanSecurityBanRequest)] = ObjectSchema(("reason", "string")),
            [nameof(BatchUnbanSecurityBansRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("ids", "reason"),
                ["properties"] = new JsonObject
                {
                    ["ids"] = ArrayOf(IntegerSchema()),
                    ["reason"] = StringSchema()
                }
            },
            [nameof(SecurityBanMutationResponse)] = ObjectSchema(("id", "integer")),
            [nameof(BatchUnbanSecurityBansResponse)] = IdListSchema("ids"),
            ["PagedSecurityEventSummary"] = PagedSchema(nameof(SecurityEventSummaryDto)),
            [nameof(SecurityEventSummaryDto)] = SecurityEventSchema(),
            [nameof(SecurityEventDetailDto)] = SecurityEventSchema(),
            ["PagedFileSummary"] = PagedSchema(nameof(FileSummaryDto)),
            [nameof(FileSummaryDto)] = FileSchema(),
            [nameof(FileDetailDto)] = FileSchema(),
            [nameof(CreateFileRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("originalName", "mimeType", "sizeBytes", "sha256", "file"),
                ["properties"] = new JsonObject
                {
                    ["originalName"] = StringSchema(),
                    ["mimeType"] = StringSchema(),
                    ["sizeBytes"] = IntegerSchema(),
                    ["sha256"] = StringSchema(),
                    ["file"] = new JsonObject { ["type"] = "string", ["format"] = "binary" }
                }
            },
            [nameof(FileMutationResponse)] = ObjectSchema(("id", "integer"))
        };
    }

    private static JsonObject PagedSchema(string recordSchemaName)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("records", "page", "pageSize", "total"),
            ["properties"] = new JsonObject
            {
                ["records"] = ArrayOf(SchemaRef(recordSchemaName)),
                ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                ["total"] = IntegerSchema()
            }
        };
    }

    private static JsonObject IdListSchema(string propertyName)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required(propertyName),
            ["properties"] = new JsonObject { [propertyName] = ArrayOf(IntegerSchema()) }
        };
    }
}
