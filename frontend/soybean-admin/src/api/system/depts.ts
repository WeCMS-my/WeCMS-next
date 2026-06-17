import { requestJson } from "@/api/request";
import type {
  ApiResult,
  CreateDepartmentRequest,
  DepartmentDetailDto,
  DepartmentMutationResponse,
  DepartmentTreeList,
  UpdateDepartmentRequest
} from "@/api/types/generated";

export function getDepartmentTreeApi(): Promise<ApiResult<DepartmentTreeList>> {
  return requestJson<DepartmentTreeList>("/api/v1/system/depts/tree");
}

export function getDepartmentApi(id: number): Promise<ApiResult<DepartmentDetailDto>> {
  return requestJson<DepartmentDetailDto>(`/api/v1/system/depts/${id}`);
}

export function createDepartmentApi(request: CreateDepartmentRequest): Promise<ApiResult<DepartmentMutationResponse>> {
  return requestJson<DepartmentMutationResponse>("/api/v1/system/depts", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateDepartmentApi(
  id: number,
  request: UpdateDepartmentRequest
): Promise<ApiResult<DepartmentMutationResponse>> {
  return requestJson<DepartmentMutationResponse>(`/api/v1/system/depts/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteDepartmentApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/depts/${id}`, { method: "DELETE" });
}

export function enableDepartmentApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/depts/${id}/enable`, { method: "POST" });
}

export function disableDepartmentApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/depts/${id}/disable`, { method: "POST" });
}
