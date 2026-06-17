import { requestJson } from "@/api/request";
import type {
  ApiResult,
  AuditLogDetailDto,
  PagedAuditLogSummary,
  PagedLoginLogSummary,
  PagedSecurityEventSummary,
  SecurityEventDetailDto,
  LoginLogDetailDto
} from "@/api/types/generated";

export interface LoginLogQuery {
  page: number;
  pageSize: number;
  username?: string;
  ip?: string;
  result?: string;
  from?: string;
  to?: string;
}

export interface AuditLogQuery {
  page: number;
  pageSize: number;
  user?: string;
  module?: string;
  resource?: string;
  action?: string;
  result?: string;
  from?: string;
  to?: string;
}

export interface SecurityEventQuery {
  page: number;
  pageSize: number;
  eventType?: string;
  severity?: string;
  user?: string;
  ip?: string;
  from?: string;
  to?: string;
}

export function getLoginLogsApi(query: LoginLogQuery): Promise<ApiResult<PagedLoginLogSummary>> {
  return requestJson<PagedLoginLogSummary>(`/api/v1/system/login-logs?${toParams(query)}`);
}

export function getLoginLogApi(id: number): Promise<ApiResult<LoginLogDetailDto>> {
  return requestJson<LoginLogDetailDto>(`/api/v1/system/login-logs/${id}`);
}

export function getAuditLogsApi(query: AuditLogQuery): Promise<ApiResult<PagedAuditLogSummary>> {
  return requestJson<PagedAuditLogSummary>(`/api/v1/system/audit-logs?${toParams(query)}`);
}

export function getAuditLogApi(id: number): Promise<ApiResult<AuditLogDetailDto>> {
  return requestJson<AuditLogDetailDto>(`/api/v1/system/audit-logs/${id}`);
}

export function getSecurityEventsApi(query: SecurityEventQuery): Promise<ApiResult<PagedSecurityEventSummary>> {
  return requestJson<PagedSecurityEventSummary>(`/api/v1/system/security-events?${toParams(query)}`);
}

export function getSecurityEventApi(id: number): Promise<ApiResult<SecurityEventDetailDto>> {
  return requestJson<SecurityEventDetailDto>(`/api/v1/system/security-events/${id}`);
}

function toParams(query: object): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, rawValue] of Object.entries(query)) {
    const value = rawValue as string | number | undefined;
    if (value !== undefined && String(value).trim()) {
      params.set(key, String(value).trim());
    }
  }
  return params;
}
