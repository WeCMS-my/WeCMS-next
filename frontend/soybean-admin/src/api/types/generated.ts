// OpenAPI-aligned placeholder for artifacts/openapi/wecms-api-v1.json.
// M2-FE-010 must replace or verify this file through the API contract gate.

export interface ApiResult<TData> {
  code: number;
  msg: string;
  data: TData;
  traceId?: string | null;
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
