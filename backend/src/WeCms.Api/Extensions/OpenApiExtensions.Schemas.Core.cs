using System.Text.Json.Nodes;
using WeCms.Modules.System.Auth;
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
using WeCms.Modules.System.System;
using WeCms.Modules.System.Users;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static JsonObject Components()
    {
        return new JsonObject
        {
            ["securitySchemes"] = new JsonObject
            {
                ["bearerAuth"] = new JsonObject
                {
                    ["type"] = "http",
                    ["scheme"] = "bearer"
                }
            },
            ["schemas"] = Schemas()
        };
    }

    private static JsonObject Schemas()
    {
        var schemas = CoreSchemas();
        AddSchemas(schemas, ManagementSchemas());
        return schemas;
    }

    private static JsonObject CoreSchemas()
    {
        return new JsonObject
        {
            ["ApiResult"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("code", "msg", "data"),
                ["properties"] = new JsonObject
                {
                    ["code"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["msg"] = StringSchema(),
                    ["data"] = new JsonObject { ["nullable"] = true },
                    ["traceId"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                    ["fieldErrors"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["additionalProperties"] = ArrayOf(StringSchema())
                    }
                }
            },
            ["Object"] = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = true
            },
            ["LoginRequest"] = ObjectSchema(("username", "string"), ("password", "string")),
            [nameof(TwoFactorVerifyRequest)] = ObjectSchema(("challengeId", "string"), ("code", "string")),
            [nameof(TwoFactorRecoveryCodeRequest)] = ObjectSchema(("challengeId", "string"), ("recoveryCode", "string")),
            [nameof(AccountTwoFactorConfirmRequest)] = ObjectSchema(("code", "string")),
            [nameof(AccountTwoFactorDisableRequest)] = NullableCredentialSchema(),
            [nameof(AccountTwoFactorRegenerateRecoveryCodesRequest)] = NullableCredentialSchema(),
            ["LoginResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("accessToken", "expiresAt", "roles", "permissions", "menus", "requiresTwoFactor"),
                ["properties"] = new JsonObject
                {
                    ["accessToken"] = StringSchema(),
                    ["expiresAt"] = new JsonObject { ["type"] = "string", ["format"] = "date-time" },
                    ["user"] = NullableRef("AuthUserDto"),
                    ["roles"] = ArrayOf(StringSchema()),
                    ["permissions"] = ArrayOf(StringSchema()),
                    ["menus"] = ArrayOf(SchemaRef(nameof(MenuTreeDto))),
                    ["requiresTwoFactor"] = BooleanSchema(),
                    ["twoFactorChallengeId"] = NullableStringSchema(),
                    ["twoFactorChallengeExpiresAt"] = DateTimeSchema(nullable: true)
                }
            },
            ["AuthMeResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("user", "roles", "permissions", "menus"),
                ["properties"] = new JsonObject
                {
                    ["user"] = SchemaRef("AuthUserDto"),
                    ["roles"] = ArrayOf(StringSchema()),
                    ["permissions"] = ArrayOf(StringSchema()),
                    ["menus"] = ArrayOf(SchemaRef(nameof(MenuTreeDto)))
                }
            },
            ["AuthUserDto"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "isSuperAdmin"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema()
                }
            },
            [nameof(AccountTwoFactorStatusResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("enabled", "confirmedAt", "recoveryCodesRemaining", "resetRequired"),
                ["properties"] = new JsonObject
                {
                    ["enabled"] = BooleanSchema(),
                    ["confirmedAt"] = DateTimeSchema(nullable: true),
                    ["recoveryCodesRemaining"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["resetRequired"] = BooleanSchema()
                }
            },
            [nameof(AccountTwoFactorSetupResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("secret", "otpAuthUri", "recoveryCodes"),
                ["properties"] = new JsonObject
                {
                    ["secret"] = StringSchema(),
                    ["otpAuthUri"] = StringSchema(),
                    ["recoveryCodes"] = ArrayOf(StringSchema())
                }
            },
            [nameof(AccountTwoFactorRecoveryCodesResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("recoveryCodes"),
                ["properties"] = new JsonObject
                {
                    ["recoveryCodes"] = ArrayOf(StringSchema())
                }
            },
            [nameof(AccountProfileResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["avatarUrl"] = NullableStringSchema()
                }
            },
            [nameof(UpdateAccountProfileRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("displayName"),
                ["properties"] = new JsonObject
                {
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema()
                }
            },
            [nameof(ChangeAccountPasswordRequest)] = ObjectSchema(("oldPassword", "string"), ("newPassword", "string")),
            [nameof(AccountAvatarUploadRequest)] = new JsonObject
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
            [nameof(AccountAvatarResponse)] = ObjectSchema(("avatarUrl", "string")),
            [nameof(AccountSecurityResponse)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("twoFactorEnabled", "twoFactorResetRequired", "mustChangePassword"),
                ["properties"] = new JsonObject
                {
                    ["twoFactorEnabled"] = BooleanSchema(),
                    ["twoFactorResetRequired"] = BooleanSchema(),
                    ["mustChangePassword"] = BooleanSchema(),
                    ["lastLoginAt"] = DateTimeSchema(nullable: true),
                    ["lastLoginIp"] = NullableStringSchema()
                }
            },
            ["SystemLiveResponse"] = ObjectSchema(("status", "string")),
            ["SystemReadyResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("status", "database"),
                ["properties"] = new JsonObject
                {
                    ["status"] = StringSchema(),
                    ["database"] = BooleanSchema()
                }
            },
            ["SystemPingResponse"] = ObjectSchema(("status", "string")),
            ["SystemVersionResponse"] = ObjectSchema(("version", "string")),
            ["SystemDbCheckResponse"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("status", "database"),
                ["properties"] = new JsonObject
                {
                    ["status"] = StringSchema(),
                    ["database"] = BooleanSchema()
                }
            },
            ["SecurePingResponse"] = ObjectSchema(("status", "string")),
            ["PagedUserSummary"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("records", "page", "pageSize", "total"),
                ["properties"] = new JsonObject
                {
                    ["records"] = ArrayOf(SchemaRef(nameof(UserSummaryDto))),
                    ["page"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                    ["total"] = IntegerSchema()
                }
            },
            [nameof(UserSummaryDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "status", "isSuperAdmin", "createdAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true),
                    ["status"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema(),
                    ["lastLoginAt"] = DateTimeSchema(nullable: true),
                    ["createdAt"] = DateTimeSchema()
                }
            },
            [nameof(UserDetailDto)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("id", "username", "displayName", "status", "isSuperAdmin", "permissionVersion", "roleIds", "postIds", "createdAt", "updatedAt"),
                ["properties"] = new JsonObject
                {
                    ["id"] = IntegerSchema(),
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true),
                    ["status"] = StringSchema(),
                    ["isSuperAdmin"] = BooleanSchema(),
                    ["permissionVersion"] = IntegerSchema(),
                    ["lastLoginAt"] = DateTimeSchema(nullable: true),
                    ["roleIds"] = ArrayOf(IntegerSchema()),
                    ["postIds"] = ArrayOf(IntegerSchema()),
                    ["createdAt"] = DateTimeSchema(),
                    ["updatedAt"] = DateTimeSchema()
                }
            },
            [nameof(CreateUserRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("username", "displayName", "password"),
                ["properties"] = new JsonObject
                {
                    ["username"] = StringSchema(),
                    ["displayName"] = StringSchema(),
                    ["password"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true),
                    ["roleIds"] = NullableArrayOf(IntegerSchema()),
                    ["postIds"] = NullableArrayOf(IntegerSchema())
                }
            },
            [nameof(UpdateUserRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("displayName"),
                ["properties"] = new JsonObject
                {
                    ["displayName"] = StringSchema(),
                    ["email"] = NullableStringSchema(),
                    ["phone"] = NullableStringSchema(),
                    ["deptId"] = IntegerSchema(nullable: true)
                }
            },
            [nameof(ResetUserPasswordRequest)] = ObjectSchema(("password", "string")),
            [nameof(ResetUserTwoFactorRequest)] = ObjectSchema(("reason", "string")),
            [nameof(AssignUserRolesRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("roleIds"),
                ["properties"] = new JsonObject { ["roleIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(AssignUserPostsRequest)] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Required("postIds"),
                ["properties"] = new JsonObject { ["postIds"] = ArrayOf(IntegerSchema()) }
            },
            [nameof(UserMutationResponse)] = ObjectSchema(("id", "integer"))
        };
    }

    private static void AddSchemas(JsonObject target, JsonObject source)
    {
        foreach (var item in source)
        {
            target[item.Key] = item.Value?.DeepClone();
        }
    }
}
