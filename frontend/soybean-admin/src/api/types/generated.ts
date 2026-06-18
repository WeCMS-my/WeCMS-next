// Generated from artifacts/openapi/wecms-api-v1.json.
// Do not edit by hand. Run: python3 scripts/generate-openapi-types.py artifacts/openapi/wecms-api-v1.json frontend/soybean-admin/src/api/types/generated.ts

export type JsonObject = Record<string, unknown>;

export interface ApiResult<TData = unknown> {
  code: number;
  msg: string;
  data: TData;
  traceId?: string | null;
  fieldErrors?: Record<string, string[]> | null;
}

export type Object = Record<string, unknown>;

export interface LoginRequest {
  username: string;
  password: string;
}

export interface TwoFactorVerifyRequest {
  challengeId: string;
  code: string;
}

export interface TwoFactorRecoveryCodeRequest {
  challengeId: string;
  recoveryCode: string;
}

export interface AccountTwoFactorConfirmRequest {
  code: string;
}

export interface AccountTwoFactorDisableRequest {
  currentPassword?: string | null;
  code?: string | null;
}

export interface AccountTwoFactorRegenerateRecoveryCodesRequest {
  currentPassword?: string | null;
  code?: string | null;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user?: AuthUserDto | null;
  roles: string[];
  permissions: string[];
  menus: MenuTreeDto[];
  requiresTwoFactor: boolean;
  twoFactorChallengeId?: string | null;
  twoFactorChallengeExpiresAt?: string | null;
}

export interface AuthMeResponse {
  user: AuthUserDto;
  roles: string[];
  permissions: string[];
  menus: MenuTreeDto[];
}

export interface AuthUserDto {
  id: number;
  username: string;
  displayName: string;
  isSuperAdmin: boolean;
}

export interface AccountTwoFactorStatusResponse {
  enabled: boolean;
  confirmedAt: string | null;
  recoveryCodesRemaining: number;
  resetRequired: boolean;
}

export interface AccountTwoFactorSetupResponse {
  secret: string;
  otpAuthUri: string;
  recoveryCodes: string[];
}

export interface AccountTwoFactorRecoveryCodesResponse {
  recoveryCodes: string[];
}

export interface AccountProfileResponse {
  id: number;
  username: string;
  displayName: string;
  email?: string | null;
  phone?: string | null;
  avatarUrl?: string | null;
}

export interface UpdateAccountProfileRequest {
  displayName: string;
  email?: string | null;
  phone?: string | null;
}

export interface ChangeAccountPasswordRequest {
  oldPassword: string;
  newPassword: string;
}

export interface AccountAvatarUploadRequest {
  originalName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  file: string;
}

export interface AccountAvatarResponse {
  avatarUrl: string;
}

export interface AccountSecurityResponse {
  twoFactorEnabled: boolean;
  twoFactorResetRequired: boolean;
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
  lastLoginIp?: string | null;
}

export interface SystemLiveResponse {
  status: string;
}

export interface SystemReadyResponse {
  status: string;
  database: boolean;
}

export interface SystemPingResponse {
  status: string;
}

export interface SystemVersionResponse {
  version: string;
}

export interface SystemDbCheckResponse {
  status: string;
  database: boolean;
}

export interface SecurePingResponse {
  status: string;
}

export interface PagedUserSummary {
  records: UserSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface UserSummaryDto {
  id: number;
  username: string;
  displayName: string;
  email?: string | null;
  phone?: string | null;
  deptId?: number | null;
  status: string;
  isSuperAdmin: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
}

export interface UserDetailDto {
  id: number;
  username: string;
  displayName: string;
  email?: string | null;
  phone?: string | null;
  deptId?: number | null;
  status: string;
  isSuperAdmin: boolean;
  permissionVersion: number;
  lastLoginAt?: string | null;
  roleIds: number[];
  postIds: number[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserRequest {
  username: string;
  displayName: string;
  password: string;
  email?: string | null;
  phone?: string | null;
  deptId?: number | null;
  roleIds?: number[] | null;
  postIds?: number[] | null;
}

export interface UpdateUserRequest {
  displayName: string;
  email?: string | null;
  phone?: string | null;
  deptId?: number | null;
}

export interface ResetUserPasswordRequest {
  password: string;
}

export interface ResetUserTwoFactorRequest {
  reason: string;
}

export interface AssignUserRolesRequest {
  roleIds: number[];
}

export interface AssignUserPostsRequest {
  postIds: number[];
}

export interface UserMutationResponse {
  id: number;
}

export interface PagedRoleSummary {
  records: RoleSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface RoleSummaryDto {
  id: number;
  code: string;
  name: string;
  status: string;
  isBuiltin: boolean;
  isLocked: boolean;
  createdAt: string;
}

export interface RoleDetailDto {
  id: number;
  code: string;
  name: string;
  status: string;
  isBuiltin: boolean;
  isLocked: boolean;
  permissionIds: number[];
  menuIds: number[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateRoleRequest {
  code: string;
  name: string;
  permissionIds?: number[] | null;
  menuIds?: number[] | null;
}

export interface UpdateRoleRequest {
  name: string;
}

export interface AssignRolePermissionsRequest {
  permissionIds: number[];
}

export interface AssignRoleMenusRequest {
  menuIds: number[];
}

export interface RoleMutationResponse {
  id: number;
}

export type MenuSummaryList = MenuSummaryDto[];

export type MenuTreeList = MenuTreeDto[];

export interface MenuSummaryDto {
  id: number;
  parentId?: number | null;
  type: string;
  code: string;
  path: string;
  component?: string | null;
  title: string;
  i18nKey?: string | null;
  icon?: string | null;
  sort: number;
  hidden: boolean;
  keepAlive: boolean;
  externalUrl?: string | null;
  permissionCode?: string | null;
  status: string;
  isBuiltin: boolean;
}

export interface MenuTreeDto {
  id: number;
  parentId?: number | null;
  type: string;
  code: string;
  path: string;
  component?: string | null;
  title: string;
  i18nKey?: string | null;
  icon?: string | null;
  sort: number;
  hidden: boolean;
  keepAlive: boolean;
  externalUrl?: string | null;
  permissionCode?: string | null;
  status: string;
  isBuiltin: boolean;
  children?: MenuTreeDto[];
}

export interface MenuDetailDto {
  id: number;
  parentId?: number | null;
  type: string;
  code: string;
  path: string;
  component?: string | null;
  title: string;
  i18nKey?: string | null;
  icon?: string | null;
  sort: number;
  hidden: boolean;
  keepAlive: boolean;
  externalUrl?: string | null;
  permissionCode?: string | null;
  status: string;
  isBuiltin: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMenuRequest {
  parentId?: number | null;
  type: string;
  path: string;
  component?: string | null;
  title: string;
  i18nKey?: string | null;
  icon?: string | null;
  sort: number;
  hidden: boolean;
  keepAlive: boolean;
  externalUrl?: string | null;
  permissionCode?: string | null;
  status: string;
  code: string;
}

export interface UpdateMenuRequest {
  parentId?: number | null;
  type: string;
  path: string;
  component?: string | null;
  title: string;
  i18nKey?: string | null;
  icon?: string | null;
  sort: number;
  hidden: boolean;
  keepAlive: boolean;
  externalUrl?: string | null;
  permissionCode?: string | null;
  status: string;
}

export interface MenuMutationResponse {
  id: number;
}

export type PermissionSummaryList = PermissionSummaryDto[];

export type PermissionTreeList = PermissionTreeDto[];

export interface PermissionSummaryDto {
  id: number;
  code: string;
  name: string;
  module: string;
  description?: string | null;
  status: string;
  isBuiltin: boolean;
  isRoleBound: boolean;
}

export interface PermissionTreeDto {
  module: string;
  permissions: PermissionSummaryDto[];
}

export interface PermissionDetailDto {
  id: number;
  code: string;
  name: string;
  module: string;
  description?: string | null;
  status: string;
  isBuiltin: boolean;
  isRoleBound: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePermissionRequest {
  name: string;
  module: string;
  description?: string | null;
  code: string;
}

export interface UpdatePermissionRequest {
  name: string;
  module: string;
  description?: string | null;
}

export interface PermissionMutationResponse {
  id: number;
}

export type DepartmentSummaryList = DepartmentSummaryDto[];

export type DepartmentTreeList = DepartmentTreeDto[];

export interface DepartmentSummaryDto {
  id: number;
  parentId?: number | null;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
}

export interface DepartmentTreeDto {
  id: number;
  parentId?: number | null;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  children?: DepartmentTreeDto[];
}

export interface DepartmentDetailDto {
  id: number;
  parentId?: number | null;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDepartmentRequest {
  parentId?: number | null;
  name: string;
  sortOrder: number;
  status: string;
  code: string;
}

export interface UpdateDepartmentRequest {
  parentId?: number | null;
  name: string;
  sortOrder: number;
  status: string;
}

export interface DepartmentMutationResponse {
  id: number;
}

export interface PagedPostSummary {
  records: PostSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface PostSummaryDto {
  id: number;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  createdAt: string;
}

export interface PostDetailDto {
  id: number;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePostRequest {
  name: string;
  sortOrder: number;
  status: string;
  code: string;
}

export interface UpdatePostRequest {
  name: string;
  sortOrder: number;
  status: string;
}

export interface PostMutationResponse {
  id: number;
}

export interface PagedDictTypeSummary {
  records: DictTypeSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface DictTypeSummaryDto {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isSystem: boolean;
  status: string;
  sortOrder: number;
  createdAt: string;
}

export interface DictTypeDetailDto {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isSystem: boolean;
  status: string;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDictTypeRequest {
  name: string;
  description?: string | null;
  sortOrder: number;
  status: string;
  code: string;
}

export interface UpdateDictTypeRequest {
  name: string;
  description?: string | null;
  sortOrder: number;
  status: string;
}

export type DictValueList = DictValueDto[];

export interface DictValueDto {
  label: string;
  value: string;
  description?: string | null;
  sortOrder: number;
  isDefault: boolean;
  status: string;
  id: number;
  typeId: number;
  typeCode: string;
}

export interface CreateDictValueRequest {
  label: string;
  value: string;
  description?: string | null;
  sortOrder: number;
  isDefault: boolean;
  status: string;
}

export interface UpdateDictValueRequest {
  label: string;
  value: string;
  description?: string | null;
  sortOrder: number;
  isDefault: boolean;
  status: string;
}

export interface DictMutationResponse {
  id: number;
}

export interface PagedSettingSummary {
  records: SettingSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface SettingSummaryDto {
  key: string;
  value?: string | null;
  valueType: string;
  groupCode: string;
  name: string;
  description?: string | null;
  isSensitive: boolean;
  isSystem: boolean;
  updatedAt: string;
  updatedBy?: number | null;
}

export interface SettingDetailDto {
  key: string;
  value?: string | null;
  valueType: string;
  groupCode: string;
  name: string;
  description?: string | null;
  isSensitive: boolean;
  isSystem: boolean;
  updatedAt: string;
  updatedBy?: number | null;
}

export interface UpdateSettingRequest {
  value?: string | null;
}

export interface SettingMutationResponse {
  key: string;
}

export interface PagedLoginLogSummary {
  records: LoginLogSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface LoginLogSummaryDto {
  id: number;
  username: string;
  userId?: number | null;
  ip?: string | null;
  result: string;
  reason?: string | null;
  createdAt: string;
}

export interface LoginLogDetailDto {
  id: number;
  username: string;
  userId?: number | null;
  ip?: string | null;
  result: string;
  reason?: string | null;
  createdAt: string;
  userAgent?: string | null;
}

export interface PagedAuditLogSummary {
  records: AuditLogSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface AuditLogSummaryDto {
  id: number;
  userId?: number | null;
  username?: string | null;
  module: string;
  resource: string;
  action: string;
  targetId?: string | null;
  result: string;
  createdAt: string;
}

export interface AuditLogDetailDto {
  id: number;
  userId?: number | null;
  username?: string | null;
  module: string;
  resource: string;
  action: string;
  targetId?: string | null;
  result: string;
  createdAt: string;
  requestMethod: string;
  requestPath: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  traceId?: string | null;
  detail: string;
}

export interface SecurityStatusDto {
  activeBans: number;
  activeIpBans: number;
  activeUserBans: number;
  criticalActiveBans: number;
  generatedAt: string;
}

export interface PagedSecurityBanSummary {
  records: SecurityBanSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface SecurityBanSummaryDto {
  id: number;
  banType: string;
  target: string;
  reason: string;
  severity: string;
  source: string;
  expiresAt?: string | null;
  revokedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SecurityBanDetailDto {
  id: number;
  banType: string;
  target: string;
  reason: string;
  severity: string;
  source: string;
  expiresAt?: string | null;
  revokedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  revokedBy?: number | null;
  revokeReason?: string | null;
  createdBy?: number | null;
  createdByUsername?: string | null;
}

export interface UnbanSecurityBanRequest {
  reason: string;
}

export interface BatchUnbanSecurityBansRequest {
  ids: number[];
  reason: string;
}

export interface SecurityBanMutationResponse {
  id: number;
}

export interface BatchUnbanSecurityBansResponse {
  ids: number[];
}

export interface PagedSecurityEventSummary {
  records: SecurityEventSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface SecurityEventSummaryDto {
  id: number;
  eventType: string;
  userId?: number | null;
  username?: string | null;
  ip?: string | null;
  severity: string;
  message: string;
  createdAt: string;
}

export interface SecurityEventDetailDto {
  id: number;
  eventType: string;
  userId?: number | null;
  username?: string | null;
  ip?: string | null;
  severity: string;
  message: string;
  createdAt: string;
}

export interface PagedFileSummary {
  records: FileSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
}

export interface FileSummaryDto {
  id: number;
  originalName: string;
  fileExt: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  status: string;
  createdBy: number;
  createdAt: string;
}

export interface FileDetailDto {
  id: number;
  originalName: string;
  fileExt: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  status: string;
  createdBy: number;
  createdAt: string;
}

export interface CreateFileRequest {
  originalName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  file: string;
}

export interface FileMutationResponse {
  id: number;
}

export interface ApiOperations {
  "/api/v1/account/2fa/confirm": {
    post: {
      response: ApiResult<AccountTwoFactorStatusResponse>;
      requestBody: AccountTwoFactorConfirmRequest;
    };
  };
  "/api/v1/account/2fa/disable": {
    post: {
      response: ApiResult<Object>;
      requestBody: AccountTwoFactorDisableRequest;
    };
  };
  "/api/v1/account/2fa/recovery-codes/regenerate": {
    post: {
      response: ApiResult<AccountTwoFactorRecoveryCodesResponse>;
      requestBody: AccountTwoFactorRegenerateRecoveryCodesRequest;
    };
  };
  "/api/v1/account/2fa/setup": {
    post: {
      response: ApiResult<AccountTwoFactorSetupResponse>;
    };
  };
  "/api/v1/account/2fa/status": {
    get: {
      response: ApiResult<AccountTwoFactorStatusResponse>;
    };
  };
  "/api/v1/account/avatar": {
    post: {
      response: ApiResult<AccountAvatarResponse>;
      requestBody: unknown;
    };
  };
  "/api/v1/account/avatar/content": {
    get: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/account/password": {
    put: {
      response: ApiResult<Object>;
      requestBody: ChangeAccountPasswordRequest;
    };
  };
  "/api/v1/account/profile": {
    get: {
      response: ApiResult<AccountProfileResponse>;
    };
    put: {
      response: ApiResult<AccountProfileResponse>;
      requestBody: UpdateAccountProfileRequest;
    };
  };
  "/api/v1/account/security": {
    get: {
      response: ApiResult<AccountSecurityResponse>;
    };
  };
  "/api/v1/auth/2fa/recovery-code": {
    post: {
      response: ApiResult<LoginResponse>;
      requestBody: TwoFactorRecoveryCodeRequest;
    };
  };
  "/api/v1/auth/2fa/verify": {
    post: {
      response: ApiResult<LoginResponse>;
      requestBody: TwoFactorVerifyRequest;
    };
  };
  "/api/v1/auth/login": {
    post: {
      response: ApiResult<LoginResponse>;
      requestBody: LoginRequest;
    };
  };
  "/api/v1/auth/logout": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/auth/me": {
    get: {
      response: ApiResult<AuthMeResponse>;
    };
  };
  "/api/v1/auth/refresh": {
    post: {
      response: ApiResult<LoginResponse>;
    };
  };
  "/api/v1/system/audit-logs": {
    get: {
      response: ApiResult<PagedAuditLogSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          user?: string;
          module?: string;
          resource?: string;
          action?: string;
          result?: string;
          from?: string;
          to?: string;
        };
      };
    };
  };
  "/api/v1/system/audit-logs/{id:long}": {
    get: {
      response: ApiResult<AuditLogDetailDto>;
    };
  };
  "/api/v1/system/db-check": {
    get: {
      response: ApiResult<SystemDbCheckResponse>;
    };
  };
  "/api/v1/system/depts": {
    get: {
      response: ApiResult<DepartmentSummaryList>;
    };
    post: {
      response: ApiResult<DepartmentMutationResponse>;
      requestBody: CreateDepartmentRequest;
    };
  };
  "/api/v1/system/depts/tree": {
    get: {
      response: ApiResult<DepartmentTreeList>;
    };
  };
  "/api/v1/system/depts/{id:long}": {
    get: {
      response: ApiResult<DepartmentDetailDto>;
    };
    put: {
      response: ApiResult<DepartmentMutationResponse>;
      requestBody: UpdateDepartmentRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/depts/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/depts/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/dict-types": {
    get: {
      response: ApiResult<PagedDictTypeSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          status?: string;
        };
      };
    };
    post: {
      response: ApiResult<DictMutationResponse>;
      requestBody: CreateDictTypeRequest;
    };
  };
  "/api/v1/system/dict-types/{id:long}": {
    get: {
      response: ApiResult<DictTypeDetailDto>;
    };
    put: {
      response: ApiResult<DictMutationResponse>;
      requestBody: UpdateDictTypeRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/dict-types/{typeCode}/values": {
    get: {
      response: ApiResult<DictValueList>;
    };
    post: {
      response: ApiResult<DictMutationResponse>;
      requestBody: CreateDictValueRequest;
    };
  };
  "/api/v1/system/dict-values/{id:long}": {
    put: {
      response: ApiResult<DictMutationResponse>;
      requestBody: UpdateDictValueRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/files": {
    get: {
      response: ApiResult<PagedFileSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          mimeType?: string;
          status?: string;
        };
      };
    };
    post: {
      response: ApiResult<FileMutationResponse>;
      requestBody: unknown;
    };
  };
  "/api/v1/system/files/{id:long}": {
    get: {
      response: ApiResult<FileDetailDto>;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/files/{id:long}/download": {
    get: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/files/{id:long}/preview": {
    get: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/login-logs": {
    get: {
      response: ApiResult<PagedLoginLogSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          username?: string;
          ip?: string;
          result?: string;
          from?: string;
          to?: string;
        };
      };
    };
  };
  "/api/v1/system/login-logs/{id:long}": {
    get: {
      response: ApiResult<LoginLogDetailDto>;
    };
  };
  "/api/v1/system/menus": {
    get: {
      response: ApiResult<MenuSummaryList>;
    };
    post: {
      response: ApiResult<MenuMutationResponse>;
      requestBody: CreateMenuRequest;
    };
  };
  "/api/v1/system/menus/tree": {
    get: {
      response: ApiResult<MenuTreeList>;
    };
  };
  "/api/v1/system/menus/{id:long}": {
    get: {
      response: ApiResult<MenuDetailDto>;
    };
    put: {
      response: ApiResult<MenuMutationResponse>;
      requestBody: UpdateMenuRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/menus/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/menus/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/permissions": {
    get: {
      response: ApiResult<PermissionSummaryList>;
    };
    post: {
      response: ApiResult<PermissionMutationResponse>;
      requestBody: CreatePermissionRequest;
    };
  };
  "/api/v1/system/permissions/tree": {
    get: {
      response: ApiResult<PermissionTreeList>;
    };
  };
  "/api/v1/system/permissions/{id:long}": {
    get: {
      response: ApiResult<PermissionDetailDto>;
    };
    put: {
      response: ApiResult<PermissionMutationResponse>;
      requestBody: UpdatePermissionRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/permissions/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/permissions/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/ping": {
    get: {
      response: ApiResult<SystemPingResponse>;
    };
  };
  "/api/v1/system/posts": {
    get: {
      response: ApiResult<PagedPostSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          status?: string;
        };
      };
    };
    post: {
      response: ApiResult<PostMutationResponse>;
      requestBody: CreatePostRequest;
    };
  };
  "/api/v1/system/posts/{id:long}": {
    get: {
      response: ApiResult<PostDetailDto>;
    };
    put: {
      response: ApiResult<PostMutationResponse>;
      requestBody: UpdatePostRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/posts/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/posts/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/roles": {
    get: {
      response: ApiResult<PagedRoleSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          status?: string;
        };
      };
    };
    post: {
      response: ApiResult<RoleMutationResponse>;
      requestBody: CreateRoleRequest;
    };
  };
  "/api/v1/system/roles/{id:long}": {
    get: {
      response: ApiResult<RoleDetailDto>;
    };
    put: {
      response: ApiResult<RoleMutationResponse>;
      requestBody: UpdateRoleRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/roles/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/roles/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/roles/{id:long}/menus": {
    put: {
      response: ApiResult<Object>;
      requestBody: AssignRoleMenusRequest;
    };
  };
  "/api/v1/system/roles/{id:long}/permissions": {
    put: {
      response: ApiResult<Object>;
      requestBody: AssignRolePermissionsRequest;
    };
  };
  "/api/v1/system/secure-ping": {
    get: {
      response: ApiResult<SecurePingResponse>;
    };
  };
  "/api/v1/system/security-events": {
    get: {
      response: ApiResult<PagedSecurityEventSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          eventType?: string;
          severity?: string;
          user?: string;
          ip?: string;
          from?: string;
          to?: string;
        };
      };
    };
  };
  "/api/v1/system/security-events/{id:long}": {
    get: {
      response: ApiResult<SecurityEventDetailDto>;
    };
  };
  "/api/v1/system/security/bans": {
    get: {
      response: ApiResult<PagedSecurityBanSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          banType?: string;
          target?: string;
          severity?: string;
          source?: string;
          activeOnly?: boolean;
        };
      };
    };
  };
  "/api/v1/system/security/bans/batch-unban": {
    post: {
      response: ApiResult<BatchUnbanSecurityBansResponse>;
      requestBody: BatchUnbanSecurityBansRequest;
    };
  };
  "/api/v1/system/security/bans/{id:long}": {
    get: {
      response: ApiResult<SecurityBanDetailDto>;
    };
  };
  "/api/v1/system/security/bans/{id:long}/unban": {
    post: {
      response: ApiResult<SecurityBanMutationResponse>;
      requestBody: UnbanSecurityBanRequest;
    };
  };
  "/api/v1/system/security/status": {
    get: {
      response: ApiResult<SecurityStatusDto>;
    };
  };
  "/api/v1/system/settings": {
    get: {
      response: ApiResult<PagedSettingSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          groupCode?: string;
        };
      };
    };
  };
  "/api/v1/system/settings/{key}": {
    get: {
      response: ApiResult<SettingDetailDto>;
    };
    put: {
      response: ApiResult<SettingMutationResponse>;
      requestBody: UpdateSettingRequest;
    };
  };
  "/api/v1/system/users": {
    get: {
      response: ApiResult<PagedUserSummary>;
      parameters: {
        query: {
          page?: number;
          pageSize?: number;
          keyword?: string;
          status?: string;
        };
      };
    };
    post: {
      response: ApiResult<UserMutationResponse>;
      requestBody: CreateUserRequest;
    };
  };
  "/api/v1/system/users/{id:long}": {
    get: {
      response: ApiResult<UserDetailDto>;
    };
    put: {
      response: ApiResult<UserMutationResponse>;
      requestBody: UpdateUserRequest;
    };
    delete: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/users/{id:long}/disable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/users/{id:long}/enable": {
    post: {
      response: ApiResult<Object>;
    };
  };
  "/api/v1/system/users/{id:long}/posts": {
    put: {
      response: ApiResult<Object>;
      requestBody: AssignUserPostsRequest;
    };
  };
  "/api/v1/system/users/{id:long}/reset-2fa": {
    post: {
      response: ApiResult<Object>;
      requestBody: ResetUserTwoFactorRequest;
    };
  };
  "/api/v1/system/users/{id:long}/reset-password": {
    post: {
      response: ApiResult<Object>;
      requestBody: ResetUserPasswordRequest;
    };
  };
  "/api/v1/system/users/{id:long}/roles": {
    put: {
      response: ApiResult<Object>;
      requestBody: AssignUserRolesRequest;
    };
  };
  "/api/v1/system/version": {
    get: {
      response: ApiResult<SystemVersionResponse>;
    };
  };
  "/health/live": {
    get: {
      response: ApiResult<SystemLiveResponse>;
    };
  };
  "/health/ready": {
    get: {
      response: ApiResult<SystemReadyResponse>;
    };
  };
}
