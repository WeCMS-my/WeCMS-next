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

export interface DictTypeListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  status?: string;
}

export function getDictTypesApi(query: DictTypeListQuery = { page: 1, pageSize: 20 }): Promise<ApiResult<PagedDictTypeSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "status", query.status);
  return requestJson<PagedDictTypeSummary>(`/api/v1/system/dict-types?${params}`);
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

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}
