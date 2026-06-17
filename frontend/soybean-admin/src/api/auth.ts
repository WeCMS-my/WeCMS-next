import { requestJson } from "./request";
import type {
  ApiResult,
  AuthMeResponse,
  LoginRequest,
  LoginResponse
} from "./types/generated";

export function loginApi(request: LoginRequest): Promise<ApiResult<LoginResponse>> {
  return requestJson<LoginResponse>("/api/v1/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
    credentials: "include",
    skipAuth: true,
    skipRefresh: true
  });
}

export function refreshApi(): Promise<ApiResult<LoginResponse>> {
  return requestJson<LoginResponse>("/api/v1/auth/refresh", {
    method: "POST",
    credentials: "include",
    skipAuth: true,
    skipRefresh: true
  });
}

export function logoutApi(): Promise<ApiResult<unknown>> {
  return requestJson<unknown>("/api/v1/auth/logout", {
    method: "POST",
    credentials: "include",
    skipAuth: true,
    skipRefresh: true
  });
}

export function authMeApi(): Promise<ApiResult<AuthMeResponse>> {
  return requestJson<AuthMeResponse>("/api/v1/auth/me");
}
