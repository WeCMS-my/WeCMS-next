import { requestJson } from "@/api/request";
import type { ApiResult, CreatePostRequest, PagedPostSummary, PostDetailDto, PostMutationResponse, UpdatePostRequest } from "@/api/types/generated";

export function getPostsApi(): Promise<ApiResult<PagedPostSummary>> {
  return requestJson<PagedPostSummary>("/api/v1/system/posts?page=1&pageSize=100");
}

export function getPostApi(id: number): Promise<ApiResult<PostDetailDto>> {
  return requestJson<PostDetailDto>(`/api/v1/system/posts/${id}`);
}

export function createPostApi(request: CreatePostRequest): Promise<ApiResult<PostMutationResponse>> {
  return requestJson<PostMutationResponse>("/api/v1/system/posts", { method: "POST", body: JSON.stringify(request) });
}

export function updatePostApi(id: number, request: UpdatePostRequest): Promise<ApiResult<PostMutationResponse>> {
  return requestJson<PostMutationResponse>(`/api/v1/system/posts/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function deletePostApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/posts/${id}`, { method: "DELETE" });
}

export function enablePostApi(id: number): Promise<ApiResult<PostMutationResponse>> {
  return requestJson<PostMutationResponse>(`/api/v1/system/posts/${id}/enable`, { method: "POST" });
}

export function disablePostApi(id: number): Promise<ApiResult<PostMutationResponse>> {
  return requestJson<PostMutationResponse>(`/api/v1/system/posts/${id}/disable`, { method: "POST" });
}
