import { requestJson } from "@/api/request";
import type {
  ApiResult,
  CreateI18nMessageRequest,
  I18nMessageDetailDto,
  I18nMutationResponse,
  PagedI18nMessageSummary,
  UpdateI18nMessageRequest
} from "@/api/types/generated";

export interface I18nMessageListQuery {
  page: number;
  pageSize: number;
  locale?: string;
  module?: string;
  keyword?: string;
  status?: string;
}

export function getI18nMessagesApi(query: I18nMessageListQuery): Promise<ApiResult<PagedI18nMessageSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "locale", query.locale);
  appendParam(params, "module", query.module);
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "status", query.status);
  return requestJson<PagedI18nMessageSummary>(`/api/v1/system/i18n/messages?${params}`);
}

export function getI18nMessageApi(id: number): Promise<ApiResult<I18nMessageDetailDto>> {
  return requestJson<I18nMessageDetailDto>(`/api/v1/system/i18n/messages/${id}`);
}

export function createI18nMessageApi(request: CreateI18nMessageRequest): Promise<ApiResult<I18nMutationResponse>> {
  return requestJson<I18nMutationResponse>("/api/v1/system/i18n/messages", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateI18nMessageApi(id: number, request: UpdateI18nMessageRequest): Promise<ApiResult<I18nMutationResponse>> {
  return requestJson<I18nMutationResponse>(`/api/v1/system/i18n/messages/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteI18nMessageApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/i18n/messages/${id}`, {
    method: "DELETE"
  });
}

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}

