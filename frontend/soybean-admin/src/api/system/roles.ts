import { requestJson } from "@/api/request";
import type { ApiResult, PagedRoleSummary } from "@/api/types/generated";

export function getRolesApi(): Promise<ApiResult<PagedRoleSummary>> {
  return requestJson<PagedRoleSummary>("/api/v1/system/roles?page=1&pageSize=100");
}
