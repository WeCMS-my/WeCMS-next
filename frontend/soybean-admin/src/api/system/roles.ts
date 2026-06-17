import { requestJson } from "@/api/request";
import type {
  ApiResult,
  AssignRoleMenusRequest,
  AssignRolePermissionsRequest,
  CreateRoleRequest,
  PagedRoleSummary,
  RoleDetailDto,
  RoleMutationResponse,
  UpdateRoleRequest
} from "@/api/types/generated";

export function getRolesApi(): Promise<ApiResult<PagedRoleSummary>> {
  return requestJson<PagedRoleSummary>("/api/v1/system/roles?page=1&pageSize=100");
}

export function getRoleApi(id: number): Promise<ApiResult<RoleDetailDto>> {
  return requestJson<RoleDetailDto>(`/api/v1/system/roles/${id}`);
}

export function createRoleApi(request: CreateRoleRequest): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>("/api/v1/system/roles", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateRoleApi(id: number, request: UpdateRoleRequest): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>(`/api/v1/system/roles/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteRoleApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/roles/${id}`, { method: "DELETE" });
}

export function enableRoleApi(id: number): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>(`/api/v1/system/roles/${id}/enable`, { method: "POST" });
}

export function disableRoleApi(id: number): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>(`/api/v1/system/roles/${id}/disable`, { method: "POST" });
}

export function assignRolePermissionsApi(
  id: number,
  request: AssignRolePermissionsRequest
): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>(`/api/v1/system/roles/${id}/permissions`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function assignRoleMenusApi(
  id: number,
  request: AssignRoleMenusRequest
): Promise<ApiResult<RoleMutationResponse>> {
  return requestJson<RoleMutationResponse>(`/api/v1/system/roles/${id}/menus`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}
