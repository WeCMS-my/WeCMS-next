import { requestJson } from "@/api/request";
import type {
  ApiResult,
  PagedSettingSummary,
  SettingDetailDto,
  SettingMutationResponse,
  UpdateSettingRequest
} from "@/api/types/generated";

export interface SettingListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  groupCode?: string;
}

export function getSettingsApi(query: SettingListQuery): Promise<ApiResult<PagedSettingSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "groupCode", query.groupCode);
  return requestJson<PagedSettingSummary>(`/api/v1/system/settings?${params}`);
}

export function getSettingApi(key: string): Promise<ApiResult<SettingDetailDto>> {
  return requestJson<SettingDetailDto>(`/api/v1/system/settings/${encodeURIComponent(key)}`);
}

export function updateSettingApi(key: string, request: UpdateSettingRequest): Promise<ApiResult<SettingMutationResponse>> {
  return requestJson<SettingMutationResponse>(`/api/v1/system/settings/${encodeURIComponent(key)}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}
