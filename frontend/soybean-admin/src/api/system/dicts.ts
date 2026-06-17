import { requestJson } from "@/api/request";
import type {
  ApiResult,
  CreateDictTypeRequest,
  CreateDictValueRequest,
  DictMutationResponse,
  DictTypeDetailDto,
  DictValueList,
  PagedDictTypeSummary,
  UpdateDictTypeRequest,
  UpdateDictValueRequest
} from "@/api/types/generated";

export function getDictTypesApi(): Promise<ApiResult<PagedDictTypeSummary>> {
  return requestJson<PagedDictTypeSummary>("/api/v1/system/dict-types?page=1&pageSize=100");
}

export function getDictTypeApi(id: number): Promise<ApiResult<DictTypeDetailDto>> {
  return requestJson<DictTypeDetailDto>(`/api/v1/system/dict-types/${id}`);
}

export function createDictTypeApi(request: CreateDictTypeRequest): Promise<ApiResult<DictMutationResponse>> {
  return requestJson<DictMutationResponse>("/api/v1/system/dict-types", { method: "POST", body: JSON.stringify(request) });
}

export function updateDictTypeApi(id: number, request: UpdateDictTypeRequest): Promise<ApiResult<DictMutationResponse>> {
  return requestJson<DictMutationResponse>(`/api/v1/system/dict-types/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function deleteDictTypeApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/dict-types/${id}`, { method: "DELETE" });
}

export function getDictValuesApi(typeCode: string): Promise<ApiResult<DictValueList>> {
  return requestJson<DictValueList>(`/api/v1/system/dict-types/${encodeURIComponent(typeCode)}/values`);
}

export function createDictValueApi(typeCode: string, request: CreateDictValueRequest): Promise<ApiResult<DictMutationResponse>> {
  return requestJson<DictMutationResponse>(`/api/v1/system/dict-types/${encodeURIComponent(typeCode)}/values`, { method: "POST", body: JSON.stringify(request) });
}

export function updateDictValueApi(id: number, request: UpdateDictValueRequest): Promise<ApiResult<DictMutationResponse>> {
  return requestJson<DictMutationResponse>(`/api/v1/system/dict-values/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function deleteDictValueApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/dict-values/${id}`, { method: "DELETE" });
}
