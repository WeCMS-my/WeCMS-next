import { requestJson } from "./request";
import type {
  ApiResult,
  CreateMenuRequest,
  MenuDetailDto,
  MenuMutationResponse,
  MenuTreeList,
  UpdateMenuRequest
} from "./types/generated";

export function getMenuTreeApi(): Promise<ApiResult<MenuTreeList>> {
  return requestJson<MenuTreeList>("/api/v1/system/menus/tree");
}

export function getMenuApi(id: number): Promise<ApiResult<MenuDetailDto>> {
  return requestJson<MenuDetailDto>(`/api/v1/system/menus/${id}`);
}

export function createMenuApi(request: CreateMenuRequest): Promise<ApiResult<MenuMutationResponse>> {
  return requestJson<MenuMutationResponse>("/api/v1/system/menus", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateMenuApi(id: number, request: UpdateMenuRequest): Promise<ApiResult<MenuMutationResponse>> {
  return requestJson<MenuMutationResponse>(`/api/v1/system/menus/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteMenuApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/menus/${id}`, { method: "DELETE" });
}

export function enableMenuApi(id: number): Promise<ApiResult<MenuMutationResponse>> {
  return requestJson<MenuMutationResponse>(`/api/v1/system/menus/${id}/enable`, { method: "POST" });
}

export function disableMenuApi(id: number): Promise<ApiResult<MenuMutationResponse>> {
  return requestJson<MenuMutationResponse>(`/api/v1/system/menus/${id}/disable`, { method: "POST" });
}
