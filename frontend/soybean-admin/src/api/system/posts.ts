import { requestJson } from "@/api/request";
import type { ApiResult, PagedPostSummary } from "@/api/types/generated";

export function getPostsApi(): Promise<ApiResult<PagedPostSummary>> {
  return requestJson<PagedPostSummary>("/api/v1/system/posts?page=1&pageSize=100");
}
