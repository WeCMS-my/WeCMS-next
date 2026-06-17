// OpenAPI-aligned placeholder for artifacts/openapi/wecms-api-v1.json.
// M2-FE-010 must replace or verify this file through the API contract gate.

export interface ApiResult<TData> {
  code: number;
  msg: string;
  data: TData;
  traceId?: string | null;
}

export interface PagedResult<TRecord> {
  records: TRecord[];
  page: number;
  pageSize: number;
  total: number;
}

export interface AuthUserDto {
  id: number;
  username: string;
  displayName: string;
  isSuperAdmin: boolean;
}

export interface AuthMenuDto {
  id: number;
  parentId?: number | null;
  type: string;
  name: string;
  path: string;
  title: string;
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

export type MenuTreeList = MenuTreeDto[];

export interface MenuDetailDto extends MenuTreeDto {
  createdAt: string;
  updatedAt: string;
}

export interface CreateMenuRequest {
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
  status: string;
}

export interface MenuMutationResponse {
  id: number;
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

export interface UserDetailDto extends UserSummaryDto {
  permissionVersion: number;
  roleIds: number[];
  postIds: number[];
  updatedAt: string;
}

export type PagedUserSummary = PagedResult<UserSummaryDto>;

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

export interface AssignUserRolesRequest {
  roleIds: number[];
}

export interface AssignUserPostsRequest {
  postIds: number[];
}

export interface UserMutationResponse {
  id: number;
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

export type PagedRoleSummary = PagedResult<RoleSummaryDto>;

export interface RoleDetailDto extends RoleSummaryDto {
  permissionIds: number[];
  menuIds: number[];
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

export interface PermissionDetailDto extends PermissionSummaryDto {
  createdAt: string;
  updatedAt: string;
}

export interface PermissionTreeDto {
  module: string;
  permissions: PermissionSummaryDto[];
}

export type PermissionTreeList = PermissionTreeDto[];
export type PermissionSummaryList = PermissionSummaryDto[];

export interface CreatePermissionRequest {
  code: string;
  name: string;
  module: string;
  description?: string | null;
}

export interface UpdatePermissionRequest {
  name: string;
  module: string;
  description?: string | null;
}

export interface PermissionMutationResponse {
  id: number;
}

export interface PostSummaryDto {
  id: number;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  createdAt: string;
}

export type PagedPostSummary = PagedResult<PostSummaryDto>;

export interface PostDetailDto extends PostSummaryDto {
  updatedAt: string;
}

export interface CreatePostRequest {
  code: string;
  name: string;
  sortOrder: number;
  status: string;
}

export interface UpdatePostRequest {
  name: string;
  sortOrder: number;
  status: string;
}

export interface PostMutationResponse {
  id: number;
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

export interface DictTypeDetailDto extends DictTypeSummaryDto {
  updatedAt: string;
}

export type PagedDictTypeSummary = PagedResult<DictTypeSummaryDto>;

export interface CreateDictTypeRequest {
  code: string;
  name: string;
  description?: string | null;
  sortOrder: number;
  status: string;
}

export interface UpdateDictTypeRequest {
  name: string;
  description?: string | null;
  sortOrder: number;
  status: string;
}

export interface DictValueDto {
  id: number;
  typeId: number;
  typeCode: string;
  label: string;
  value: string;
  description?: string | null;
  sortOrder: number;
  isDefault: boolean;
  status: string;
}

export type DictValueList = DictValueDto[];

export interface CreateDictValueRequest {
  label: string;
  value: string;
  description?: string | null;
  sortOrder: number;
  isDefault: boolean;
  status: string;
}

export type UpdateDictValueRequest = CreateDictValueRequest;

export interface DictMutationResponse {
  id: number;
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

export type SettingDetailDto = SettingSummaryDto;
export type PagedSettingSummary = PagedResult<SettingSummaryDto>;

export interface UpdateSettingRequest {
  value?: string | null;
}

export interface SettingMutationResponse {
  key: string;
}

export interface LoginLogSummaryDto {
  id: number;
  username: string;
  userId?: number | null;
  ip: string;
  result: string;
  reason?: string | null;
  createdAt: string;
  userAgent?: string | null;
}

export type LoginLogDetailDto = LoginLogSummaryDto;
export type PagedLoginLogSummary = PagedResult<LoginLogSummaryDto>;

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
  requestMethod?: string | null;
  requestPath?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  traceId?: string | null;
  detail?: string | null;
}

export type AuditLogDetailDto = AuditLogSummaryDto;
export type PagedAuditLogSummary = PagedResult<AuditLogSummaryDto>;

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

export type SecurityEventDetailDto = SecurityEventSummaryDto;
export type PagedSecurityEventSummary = PagedResult<SecurityEventSummaryDto>;

export interface DepartmentTreeDto {
  id: number;
  parentId?: number | null;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  children?: DepartmentTreeDto[];
}

export type DepartmentTreeList = DepartmentTreeDto[];

export interface DepartmentDetailDto extends DepartmentTreeDto {
  createdAt: string;
  updatedAt: string;
}

export interface CreateDepartmentRequest {
  parentId?: number | null;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
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

export interface AuthMeResponse {
  user: AuthUserDto;
  roles: string[];
  permissions: string[];
  menus: AuthMenuDto[];
}

export interface LoginResponse extends AuthMeResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}
