import { requestJson } from "@/api/request";
import type {
  ApiResult,
  AssignUserPostsRequest,
  AssignUserRolesRequest,
  CreateUserRequest,
  PagedUserSummary,
  ResetUserPasswordRequest,
  ResetUserTwoFactorRequest,
  UpdateUserRequest,
  UserDetailDto,
  UserMutationResponse
} from "@/api/types/generated";

export interface UserListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  status?: string;
  deptId?: number;
}

export function getUsersApi(query: UserListQuery): Promise<ApiResult<PagedUserSummary>> {
  return requestJson<PagedUserSummary>(`/api/v1/system/users?${toQueryString(query)}`);
}

export function getUserApi(id: number): Promise<ApiResult<UserDetailDto>> {
  return requestJson<UserDetailDto>(`/api/v1/system/users/${id}`);
}

export function createUserApi(request: CreateUserRequest): Promise<ApiResult<UserMutationResponse>> {
  return requestJson<UserMutationResponse>("/api/v1/system/users", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateUserApi(id: number, request: UpdateUserRequest): Promise<ApiResult<UserMutationResponse>> {
  return requestJson<UserMutationResponse>(`/api/v1/system/users/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteUserApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}`, {
    method: "DELETE"
  });
}

export function enableUserApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/enable`, {
    method: "POST"
  });
}

export function disableUserApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/disable`, {
    method: "POST"
  });
}

export function resetUserPasswordApi(
  id: number,
  request: ResetUserPasswordRequest
): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/reset-password`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function resetUserTwoFactorApi(
  id: number,
  request: ResetUserTwoFactorRequest
): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/reset-2fa`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function assignUserRolesApi(
  id: number,
  request: AssignUserRolesRequest
): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/roles`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function assignUserPostsApi(
  id: number,
  request: AssignUserPostsRequest
): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/users/${id}/posts`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

function toQueryString(query: UserListQuery): string {
  const parameters = new URLSearchParams();
  parameters.set("page", String(query.page));
  parameters.set("pageSize", String(query.pageSize));
  if (query.keyword) {
    parameters.set("keyword", query.keyword);
  }
  if (query.status) {
    parameters.set("status", query.status);
  }
  if (query.deptId) {
    parameters.set("deptId", String(query.deptId));
  }
  return parameters.toString();
}
