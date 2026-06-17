import { requestJson } from "./request";
import type { ApiResult, MenuTreeList } from "./types/generated";

export function getMenuTreeApi(): Promise<ApiResult<MenuTreeList>> {
  return requestJson<MenuTreeList>("/api/v1/system/menus/tree");
}
