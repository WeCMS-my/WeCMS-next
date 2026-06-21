import { requestJson } from "@/api/request";
import type { ApiResult, CreatePositionRequest, PagedPositionSummary, PositionDetailDto, PositionMutationResponse, UpdatePositionRequest } from "@/api/types/generated";

export interface PositionListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  status?: string;
}

export function getPositionsApi(query: PositionListQuery = { page: 1, pageSize: 20 }): Promise<ApiResult<PagedPositionSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "status", query.status);
  return requestJson<PagedPositionSummary>(`/api/v1/system/positions?${params}`);
}

export function getPositionApi(id: number): Promise<ApiResult<PositionDetailDto>> {
  return requestJson<PositionDetailDto>(`/api/v1/system/positions/${id}`);
}

export function createPositionApi(request: CreatePositionRequest): Promise<ApiResult<PositionMutationResponse>> {
  return requestJson<PositionMutationResponse>("/api/v1/system/positions", { method: "POST", body: JSON.stringify(request) });
}

export function updatePositionApi(id: number, request: UpdatePositionRequest): Promise<ApiResult<PositionMutationResponse>> {
  return requestJson<PositionMutationResponse>(`/api/v1/system/positions/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function deletePositionApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/positions/${id}`, { method: "DELETE" });
}

export function enablePositionApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/positions/${id}/enable`, { method: "POST" });
}

export function disablePositionApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/positions/${id}/disable`, { method: "POST" });
}

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}
