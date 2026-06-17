import { requestJson } from "@/api/request";
import type { ApiResult, DepartmentTreeList } from "@/api/types/generated";

export function getDepartmentTreeApi(): Promise<ApiResult<DepartmentTreeList>> {
  return requestJson<DepartmentTreeList>("/api/v1/system/depts/tree");
}
