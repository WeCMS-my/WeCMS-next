using System.Text.Json.Nodes;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Audit.Logs;
using WeCms.Modules.Security.Events;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.Security;
using WeCms.Modules.Configuration.Settings;

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
            [nameof(SortMenusRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("items"),
                ["properties"] = new JsonObject
                {
                    ["items"] = ArrayOf(SchemaRef(nameof(SortMenuItemRequest)))
                }
            },
            [nameof(SortMenuItemRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "sort"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["parentId"] = IntegerSchema(nullable: true),
                    ["sort"] = IntegerSchema()
                }
            },
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
            ["PagedPositionSummary"] = PagedSchema(nameof(PositionSummaryDto)),
            [nameof(PositionSummaryDto)] = PositionSchema(includeTimestamps: false),
            [nameof(PositionDetailDto)] = PositionSchema(includeTimestamps: true),
            [nameof(CreatePositionRequest)] = PositionMutationSchema(includeCode: true),
            [nameof(UpdatePositionRequest)] = PositionMutationSchema(includeCode: false),
            [nameof(PositionMutationResponse)] = ObjectSchema(("id", "integer")),
            ["PagedDictTypeSummary"] = PagedSchema(nameof(DictTypeSummaryDto)),
            [nameof(DictTypeSummaryDto)] = DictTypeSchema(includeTimestamps: false),
            [nameof(DictTypeDetailDto)] = DictTypeSchema(includeTimestamps: true),
            [nameof(CreateDictTypeRequest)] = DictTypeMutationSchema(includeCode: true),
            [nameof(UpdateDictTypeRequest)] = DictTypeMutationSchema(includeCode: false),
            [nameof(DisableDictTypeRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("cascadeValues"),
                ["properties"] = new JsonObject
                {
                    ["cascadeValues"] = BooleanSchema()
                }
            },
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
            [nameof(ValidateIpRulesRequest)] = ObjectSchema(("rules", "string")),
            [nameof(ValidateIpRulesResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("valid"),
                ["properties"] = new JsonObject
                {
                    ["valid"] = BooleanSchema()
                }
            },
            [nameof(SettingMutationResponse)] = ObjectSchema(("key", "string")),
            ["PagedI18nMessageSummary"] = PagedSchema(nameof(I18nMessageSummaryDto)),
            [nameof(I18nMessageSummaryDto)] = I18nMessageSchema(includeDetail: false),
            [nameof(I18nMessageDetailDto)] = I18nMessageSchema(includeDetail: true),
            [nameof(CreateI18nMessageRequest)] = I18nCreateRequestSchema(),
            [nameof(UpdateI18nMessageRequest)] = I18nUpdateRequestSchema(),
            [nameof(SwitchAccountLocaleRequest)] = ObjectSchema(("locale", "string")),
            [nameof(I18nMessagesResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("locale", "messages"),
                ["properties"] = new JsonObject
                {
                    ["locale"] = StringSchema(),
                    ["messages"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = StringSchema()
                    }
                }
            },
            [nameof(AccountI18nSwitchResponse)] = ObjectSchema(("locale", "string")),
            [nameof(I18nMutationResponse)] = ObjectSchema(("id", "integer")),
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
            [nameof(SecurityRejectionSnapshotDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("aggregateCount", "droppedCounter", "droppedByKind"),
                ["properties"] = new JsonObject
                {
                    ["aggregateCount"] = IntegerSchema(),
                    ["droppedCounter"] = IntegerSchema(),
                    ["lastDropAt"] = DateTimeSchema(nullable: true),
                    ["droppedByKind"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = IntegerSchema()
                    }
                }
            },
            [nameof(SecurityRejectionMetricsDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required(
                    "security_rejection_buffer_aggregates",
                    "security_rejection_buffer_dropped_total",
                    "security_rejection_buffer_dropped_by_kind"),
                ["properties"] = new JsonObject
                {
                    ["security_rejection_buffer_aggregates"] = IntegerSchema(),
                    ["security_rejection_buffer_dropped_total"] = IntegerSchema(),
                    ["security_rejection_buffer_last_drop_at"] = DateTimeSchema(nullable: true),
                    ["security_rejection_buffer_dropped_by_kind"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = IntegerSchema()
                    }
                }
            },
            ["PagedFileSummary"] = PagedSchema(nameof(FileSummaryDto)),
            [nameof(FileSummaryDto)] = FileSchema(),
            [nameof(FileDetailDto)] = FileSchema(),
            [nameof(FileUploadConcurrencyMetricsDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required(
                    "file_upload_large_file_concurrency_limit",
                    "file_upload_large_file_active",
                    "file_upload_large_file_rejected_total",
                    "file_upload_large_file_threshold_bytes"),
                ["properties"] = new JsonObject
                {
                    ["file_upload_large_file_concurrency_limit"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["file_upload_large_file_active"] = IntegerSchema(),
                    ["file_upload_large_file_rejected_total"] = IntegerSchema(),
                    ["file_upload_large_file_threshold_bytes"] = IntegerSchema()
                }
            },
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
                    ["policy"] = StringSchema(),
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

    private static JsonObject I18nMessageSchema(bool includeDetail)
    {
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["locale"] = StringSchema(),
            ["module"] = StringSchema(),
            ["messageKey"] = StringSchema(),
            ["messageValue"] = StringSchema(),
            ["status"] = StringSchema(),
            ["updatedAt"] = DateTimeSchema()
        };

        if (includeDetail)
        {
            properties["remark"] = NullableStringSchema();
            properties["createdAt"] = DateTimeSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = includeDetail
                ? Required("id", "locale", "module", "messageKey", "messageValue", "status", "createdAt", "updatedAt")
                : Required("id", "locale", "module", "messageKey", "messageValue", "status", "updatedAt"),
            ["properties"] = properties
        };
    }

    private static JsonObject I18nCreateRequestSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("locale", "module", "messageKey", "messageValue", "status"),
            ["properties"] = new JsonObject
            {
                ["locale"] = StringSchema(),
                ["module"] = StringSchema(),
                ["messageKey"] = StringSchema(),
                ["messageValue"] = StringSchema(),
                ["remark"] = NullableStringSchema(),
                ["status"] = StringSchema()
            }
        };
    }

    private static JsonObject I18nUpdateRequestSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("module", "messageValue", "status"),
            ["properties"] = new JsonObject
            {
                ["module"] = StringSchema(),
                ["messageValue"] = StringSchema(),
                ["remark"] = NullableStringSchema(),
                ["status"] = StringSchema()
            }
        };
    }
}
