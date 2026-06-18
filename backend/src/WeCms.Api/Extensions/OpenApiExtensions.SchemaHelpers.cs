using System.Text.Json.Nodes;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Security;
using WeCms.Modules.System.Settings;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static JsonObject FileSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "originalName", "fileExt", "mimeType", "sizeBytes", "sha256", "status", "createdBy", "createdAt"),
            ["properties"] = new JsonObject
            {
                ["id"] = IntegerSchema(),
                ["originalName"] = StringSchema(),
                ["fileExt"] = StringSchema(),
                ["mimeType"] = StringSchema(),
                ["sizeBytes"] = IntegerSchema(),
                ["sha256"] = StringSchema(),
                ["status"] = StringSchema(),
                ["createdBy"] = IntegerSchema(),
                ["createdAt"] = DateTimeSchema()
            }
        };
    }

    private static JsonObject SecurityEventSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "eventType", "severity", "source", "traceId", "message", "createdAt"),
            ["properties"] = new JsonObject
            {
                ["id"] = IntegerSchema(),
                ["eventType"] = StringSchema(),
                ["userId"] = IntegerSchema(nullable: true),
                ["username"] = NullableStringSchema(),
                ["ip"] = NullableStringSchema(),
                ["severity"] = StringSchema(),
                ["source"] = StringSchema(),
                ["traceId"] = StringSchema(),
                ["message"] = StringSchema(),
                ["createdAt"] = DateTimeSchema()
            }
        };
    }

    private static JsonObject SecurityBanSchema(bool includeRevokeDetail)
    {
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["banType"] = StringSchema(),
            ["target"] = StringSchema(),
            ["reason"] = StringSchema(),
            ["severity"] = StringSchema(),
            ["source"] = StringSchema(),
            ["expiresAt"] = DateTimeSchema(nullable: true),
            ["revokedAt"] = DateTimeSchema(nullable: true),
            ["createdAt"] = DateTimeSchema(),
            ["updatedAt"] = DateTimeSchema()
        };

        if (includeRevokeDetail)
        {
            properties["revokedBy"] = IntegerSchema(nullable: true);
            properties["revokeReason"] = NullableStringSchema();
            properties["createdBy"] = IntegerSchema(nullable: true);
            properties["createdByUsername"] = NullableStringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "banType", "target", "reason", "severity", "source", "createdAt", "updatedAt"),
            ["properties"] = properties
        };
    }

    private static JsonObject AuditLogSchema(bool includeRequest)
    {
        var required = includeRequest
            ? Required("id", "module", "resource", "action", "requestMethod", "requestPath", "result", "detail", "createdAt")
            : Required("id", "module", "resource", "action", "result", "createdAt");
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["userId"] = IntegerSchema(nullable: true),
            ["username"] = NullableStringSchema(),
            ["module"] = StringSchema(),
            ["resource"] = StringSchema(),
            ["action"] = StringSchema(),
            ["targetId"] = NullableStringSchema(),
            ["result"] = StringSchema(),
            ["createdAt"] = DateTimeSchema()
        };
        if (includeRequest)
        {
            properties["requestMethod"] = StringSchema();
            properties["requestPath"] = StringSchema();
            properties["ipAddress"] = NullableStringSchema();
            properties["userAgent"] = NullableStringSchema();
            properties["traceId"] = NullableStringSchema();
            properties["detail"] = StringSchema();
        }

        return new JsonObject { ["type"] = "object", ["required"] = required, ["properties"] = properties };
    }

    private static JsonObject LoginLogSchema(bool includeUserAgent)
    {
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["username"] = StringSchema(),
            ["userId"] = IntegerSchema(nullable: true),
            ["ip"] = NullableStringSchema(),
            ["result"] = StringSchema(),
            ["reason"] = NullableStringSchema(),
            ["createdAt"] = DateTimeSchema()
        };
        if (includeUserAgent)
        {
            properties["userAgent"] = NullableStringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "username", "result", "createdAt"),
            ["properties"] = properties
        };
    }

    private static JsonObject SettingSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("key", "valueType", "groupCode", "name", "isSensitive", "isSystem", "updatedAt"),
            ["properties"] = new JsonObject
            {
                ["key"] = StringSchema(),
                ["value"] = NullableStringSchema(),
                ["valueType"] = StringSchema(),
                ["groupCode"] = StringSchema(),
                ["name"] = StringSchema(),
                ["description"] = NullableStringSchema(),
                ["isSensitive"] = BooleanSchema(),
                ["isSystem"] = BooleanSchema(),
                ["updatedAt"] = DateTimeSchema(),
                ["updatedBy"] = IntegerSchema(nullable: true)
            }
        };
    }

    private static JsonObject DictTypeSchema(bool includeTimestamps)
    {
        var properties = new JsonObject
        {
            ["id"] = IntegerSchema(),
            ["code"] = StringSchema(),
            ["name"] = StringSchema(),
            ["description"] = NullableStringSchema(),
            ["isSystem"] = BooleanSchema(),
            ["status"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["createdAt"] = DateTimeSchema()
        };
        if (includeTimestamps) properties["updatedAt"] = DateTimeSchema();
        return new JsonObject { ["type"] = "object", ["required"] = includeTimestamps ? Required("id", "code", "name", "isSystem", "status", "sortOrder", "createdAt", "updatedAt") : Required("id", "code", "name", "isSystem", "status", "sortOrder", "createdAt"), ["properties"] = properties };
    }

    private static JsonObject PostSchema(bool includeTimestamps)
    {
        var properties = new JsonObject { ["id"] = IntegerSchema(), ["code"] = StringSchema(), ["name"] = StringSchema(), ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" }, ["status"] = StringSchema(), ["createdAt"] = DateTimeSchema() };
        if (includeTimestamps) properties["updatedAt"] = DateTimeSchema();
        return new JsonObject { ["type"] = "object", ["required"] = includeTimestamps ? Required("id", "code", "name", "sortOrder", "status", "createdAt", "updatedAt") : Required("id", "code", "name", "sortOrder", "status", "createdAt"), ["properties"] = properties };
    }

    private static JsonObject DepartmentSchema(bool includeChildren, bool includeTimestamps)
    {
        var properties = new JsonObject { ["id"] = IntegerSchema(), ["parentId"] = IntegerSchema(nullable: true), ["code"] = StringSchema(), ["name"] = StringSchema(), ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" }, ["status"] = StringSchema() };
        if (includeChildren) properties["children"] = ArrayOf(SchemaRef(nameof(DepartmentTreeDto)));
        if (includeTimestamps) { properties["createdAt"] = DateTimeSchema(); properties["updatedAt"] = DateTimeSchema(); }
        return new JsonObject { ["type"] = "object", ["required"] = includeTimestamps ? Required("id", "code", "name", "sortOrder", "status", "createdAt", "updatedAt") : Required("id", "code", "name", "sortOrder", "status"), ["properties"] = properties };
    }

    private static JsonObject PermissionSchema(bool includeTimestamps)
    {
        var properties = new JsonObject { ["id"] = IntegerSchema(), ["code"] = StringSchema(), ["name"] = StringSchema(), ["module"] = StringSchema(), ["description"] = NullableStringSchema(), ["status"] = StringSchema(), ["isBuiltin"] = BooleanSchema(), ["isRoleBound"] = BooleanSchema() };
        if (includeTimestamps) { properties["createdAt"] = DateTimeSchema(); properties["updatedAt"] = DateTimeSchema(); }
        return new JsonObject { ["type"] = "object", ["required"] = includeTimestamps ? Required("id", "code", "name", "module", "status", "isBuiltin", "isRoleBound", "createdAt", "updatedAt") : Required("id", "code", "name", "module", "status", "isBuiltin", "isRoleBound"), ["properties"] = properties };
    }

    private static JsonObject MenuSchema(bool includeChildren, bool includeTimestamps)
    {
        var properties = new JsonObject { ["id"] = IntegerSchema(), ["parentId"] = IntegerSchema(nullable: true), ["type"] = StringSchema(), ["code"] = StringSchema(), ["path"] = StringSchema(), ["component"] = NullableStringSchema(), ["title"] = StringSchema(), ["i18nKey"] = NullableStringSchema(), ["icon"] = NullableStringSchema(), ["sort"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" }, ["hidden"] = BooleanSchema(), ["keepAlive"] = BooleanSchema(), ["externalUrl"] = NullableStringSchema(), ["permissionCode"] = NullableStringSchema(), ["status"] = StringSchema(), ["isBuiltin"] = BooleanSchema() };
        if (includeChildren) properties["children"] = ArrayOf(SchemaRef(nameof(MenuTreeDto)));
        if (includeTimestamps) { properties["createdAt"] = DateTimeSchema(); properties["updatedAt"] = DateTimeSchema(); }
        return new JsonObject { ["type"] = "object", ["required"] = includeTimestamps ? Required("id", "type", "code", "path", "title", "sort", "hidden", "keepAlive", "status", "isBuiltin", "createdAt", "updatedAt") : Required("id", "type", "code", "path", "title", "sort", "hidden", "keepAlive", "status", "isBuiltin"), ["properties"] = properties };
    }

    private static JsonObject MenuMutationSchema(bool includeCode)
    {
        var properties = new JsonObject
        {
            ["parentId"] = IntegerSchema(nullable: true),
            ["type"] = StringSchema(),
            ["path"] = StringSchema(),
            ["component"] = NullableStringSchema(),
            ["title"] = StringSchema(),
            ["i18nKey"] = NullableStringSchema(),
            ["icon"] = NullableStringSchema(),
            ["sort"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["hidden"] = BooleanSchema(),
            ["keepAlive"] = BooleanSchema(),
            ["externalUrl"] = NullableStringSchema(),
            ["permissionCode"] = NullableStringSchema(),
            ["status"] = StringSchema()
        };
        if (includeCode)
        {
            properties["code"] = StringSchema();
        }

        var required = includeCode
            ? Required("type", "code", "path", "title", "sort", "hidden", "keepAlive", "status")
            : Required("type", "path", "title", "sort", "hidden", "keepAlive", "status");

        return new JsonObject { ["type"] = "object", ["required"] = required, ["properties"] = properties };
    }

    private static JsonObject PermissionMutationSchema(bool includeCode)
    {
        var properties = new JsonObject
        {
            ["name"] = StringSchema(),
            ["module"] = StringSchema(),
            ["description"] = NullableStringSchema()
        };
        if (includeCode)
        {
            properties["code"] = StringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = includeCode ? Required("code", "name", "module") : Required("name", "module"),
            ["properties"] = properties
        };
    }

    private static JsonObject DepartmentMutationSchema(bool includeCode)
    {
        var properties = new JsonObject
        {
            ["parentId"] = IntegerSchema(nullable: true),
            ["name"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["status"] = StringSchema()
        };
        if (includeCode)
        {
            properties["code"] = StringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = includeCode ? Required("code", "name", "sortOrder", "status") : Required("name", "sortOrder", "status"),
            ["properties"] = properties
        };
    }

    private static JsonObject PostMutationSchema(bool includeCode)
    {
        var properties = new JsonObject
        {
            ["name"] = StringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["status"] = StringSchema()
        };
        if (includeCode)
        {
            properties["code"] = StringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = includeCode ? Required("code", "name", "sortOrder", "status") : Required("name", "sortOrder", "status"),
            ["properties"] = properties
        };
    }

    private static JsonObject DictTypeMutationSchema(bool includeCode)
    {
        var properties = new JsonObject
        {
            ["name"] = StringSchema(),
            ["description"] = NullableStringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["status"] = StringSchema()
        };
        if (includeCode)
        {
            properties["code"] = StringSchema();
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = includeCode ? Required("code", "name", "sortOrder", "status") : Required("name", "sortOrder", "status"),
            ["properties"] = properties
        };
    }

    private static JsonObject DictValueSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("id", "typeId", "typeCode", "label", "value", "sortOrder", "isDefault", "status"),
            ["properties"] = DictValueProperties(includeTypeIdentity: true)
        };
    }

    private static JsonObject DictValueMutationSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = Required("label", "value", "sortOrder", "isDefault", "status"),
            ["properties"] = DictValueProperties(includeTypeIdentity: false)
        };
    }

    private static JsonObject DictValueProperties(bool includeTypeIdentity)
    {
        var properties = new JsonObject
        {
            ["label"] = StringSchema(),
            ["value"] = StringSchema(),
            ["description"] = NullableStringSchema(),
            ["sortOrder"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
            ["isDefault"] = BooleanSchema(),
            ["status"] = StringSchema()
        };
        if (includeTypeIdentity)
        {
            properties["id"] = IntegerSchema();
            properties["typeId"] = IntegerSchema();
            properties["typeCode"] = StringSchema();
        }

        return properties;
    }

    private static JsonObject ObjectSchema(params (string Name, string Type)[] properties)
    {
        var schemaProperties = new JsonObject();
        foreach (var (name, type) in properties) schemaProperties[name] = new JsonObject { ["type"] = type };
        return new JsonObject { ["type"] = "object", ["required"] = Required(properties.Select(property => property.Name).ToArray()), ["properties"] = schemaProperties };
    }

    private static JsonObject NullableCredentialSchema()
    {
        return new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["currentPassword"] = NullableStringSchema(), ["code"] = NullableStringSchema() } };
    }

    private static JsonObject ApiResultRef(string dataRef)
    {
        return new JsonObject { ["allOf"] = new JsonArray(SchemaRef("ApiResult"), new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["data"] = SchemaRef(dataRef) } }) };
    }

    private static JsonObject JsonContent(JsonObject schema, string mediaType = "application/json")
    {
        return new JsonObject { [mediaType] = new JsonObject { ["schema"] = schema } };
    }

    private static JsonObject SchemaRef(string name) => new() { ["$ref"] = $"#/components/schemas/{name}" };

    private static JsonObject NullableRef(string name)
    {
        return new JsonObject { ["oneOf"] = new JsonArray(SchemaRef(name), new JsonObject { ["type"] = "null" }) };
    }

    private static JsonObject StringSchema() => new() { ["type"] = "string" };
    private static JsonObject NullableStringSchema() => new() { ["type"] = new JsonArray("string", "null") };
    private static JsonObject DateTimeSchema(bool nullable = false) => nullable ? new JsonObject { ["type"] = new JsonArray("string", "null"), ["format"] = "date-time" } : new JsonObject { ["type"] = "string", ["format"] = "date-time" };
    private static JsonObject IntegerSchema(bool nullable = false) => nullable ? new JsonObject { ["type"] = new JsonArray("integer", "null"), ["format"] = "int64" } : new JsonObject { ["type"] = "integer", ["format"] = "int64" };
    private static JsonObject BooleanSchema() => new() { ["type"] = "boolean" };
    private static JsonObject ArrayOf(JsonObject itemSchema) => new() { ["type"] = "array", ["items"] = itemSchema };
    private static JsonObject NullableArrayOf(JsonObject itemSchema) => new() { ["type"] = new JsonArray("array", "null"), ["items"] = itemSchema };

    private static JsonArray Required(params string[] names)
    {
        var required = new JsonArray();
        foreach (var name in names) required.Add(name);
        return required;
    }
}
