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
  email?: string | null;
  phone?: string | null;
  status: string;
}

export interface AuthMenuDto {
  id: number;
  parentId?: number | null;
  type: string;
  name: string;
  path: string;
  title: string;
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
