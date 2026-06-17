import { requestJson } from "@/api/request";
import type {
  ApiResult,
  CreatePermissionRequest,
  PermissionDetailDto,
  PermissionMutationResponse,
  PermissionSummaryList,
  PermissionTreeList,
  UpdatePermissionRequest
} from "@/api/types/generated";

export function getPermissionsApi(): Promise<ApiResult<PermissionSummaryList>> {
  return requestJson<PermissionSummaryList>("/api/v1/system/permissions");
}

export function getPermissionTreeApi(): Promise<ApiResult<PermissionTreeList>> {
  return requestJson<PermissionTreeList>("/api/v1/system/permissions/tree");
}

export function getPermissionApi(id: number): Promise<ApiResult<PermissionDetailDto>> {
  return requestJson<PermissionDetailDto>(`/api/v1/system/permissions/${id}`);
}

export function createPermissionApi(
  request: CreatePermissionRequest
): Promise<ApiResult<PermissionMutationResponse>> {
  return requestJson<PermissionMutationResponse>("/api/v1/system/permissions", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updatePermissionApi(
  id: number,
  request: UpdatePermissionRequest
): Promise<ApiResult<PermissionMutationResponse>> {
  return requestJson<PermissionMutationResponse>(`/api/v1/system/permissions/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deletePermissionApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/permissions/${id}`, { method: "DELETE" });
}

export function enablePermissionApi(id: number): Promise<ApiResult<PermissionMutationResponse>> {
  return requestJson<PermissionMutationResponse>(`/api/v1/system/permissions/${id}/enable`, { method: "POST" });
}

export function disablePermissionApi(id: number): Promise<ApiResult<PermissionMutationResponse>> {
  return requestJson<PermissionMutationResponse>(`/api/v1/system/permissions/${id}/disable`, { method: "POST" });
}
