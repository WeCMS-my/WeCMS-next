import { requestJson } from "@/api/request";
import type {
  ApiResult,
  BatchUnbanSecurityBansRequest,
  BatchUnbanSecurityBansResponse,
  PagedSecurityBanSummary,
  SecurityBanDetailDto,
  SecurityBanMutationResponse,
  SecurityStatusDto,
  UnbanSecurityBanRequest
} from "@/api/types/generated";

export interface SecurityBanQuery {
  page: number;
  pageSize: number;
  banType?: string;
  target?: string;
  severity?: string;
  source?: string;
  activeOnly?: boolean;
}

export function getSecurityStatusApi(): Promise<ApiResult<SecurityStatusDto>> {
  return requestJson<SecurityStatusDto>("/api/v1/system/security/status");
}

export function getSecurityBansApi(query: SecurityBanQuery): Promise<ApiResult<PagedSecurityBanSummary>> {
  return requestJson<PagedSecurityBanSummary>(`/api/v1/system/security/bans?${toParams(query)}`);
}

export function getSecurityBanApi(id: number): Promise<ApiResult<SecurityBanDetailDto>> {
  return requestJson<SecurityBanDetailDto>(`/api/v1/system/security/bans/${id}`);
}

export function unbanSecurityBanApi(
  id: number,
  request: UnbanSecurityBanRequest
): Promise<ApiResult<SecurityBanMutationResponse>> {
  return requestJson<SecurityBanMutationResponse>(`/api/v1/system/security/bans/${id}/unban`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function batchUnbanSecurityBansApi(
  request: BatchUnbanSecurityBansRequest
): Promise<ApiResult<BatchUnbanSecurityBansResponse>> {
  return requestJson<BatchUnbanSecurityBansResponse>("/api/v1/system/security/bans/batch-unban", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

function toParams(query: object): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, rawValue] of Object.entries(query)) {
    const value = rawValue as boolean | string | number | undefined;
    if (value !== undefined && String(value).trim()) {
      params.set(key, String(value).trim());
    }
  }
  return params;
}
