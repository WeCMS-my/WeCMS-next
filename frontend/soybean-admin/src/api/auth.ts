import { requestJson } from "./request";
import type {
  ApiResult,
  AuthMeResponse,
  LoginRequest,
  LoginResponse,
  LogoutRequest,
  RefreshTokenRequest
} from "./types/generated";

export function loginApi(request: LoginRequest): Promise<ApiResult<LoginResponse>> {
  return requestJson<LoginResponse>("/api/v1/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
    skipAuth: true,
    skipRefresh: true
  });
}

export function refreshApi(request: RefreshTokenRequest): Promise<ApiResult<LoginResponse>> {
  return requestJson<LoginResponse>("/api/v1/auth/refresh", {
    method: "POST",
    body: JSON.stringify(request),
    skipAuth: true,
    skipRefresh: true
  });
}

export function logoutApi(request: LogoutRequest): Promise<ApiResult<unknown>> {
  return requestJson<unknown>("/api/v1/auth/logout", {
    method: "POST",
    body: JSON.stringify(request),
    skipAuth: true,
    skipRefresh: true
  });
}

export function authMeApi(): Promise<ApiResult<AuthMeResponse>> {
  return requestJson<AuthMeResponse>("/api/v1/auth/me");
}
