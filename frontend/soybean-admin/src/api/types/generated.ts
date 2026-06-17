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

export interface PostSummaryDto {
  id: number;
  code: string;
  name: string;
  sortOrder: number;
  status: string;
  createdAt: string;
}

export type PagedPostSummary = PagedResult<PostSummaryDto>;

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
