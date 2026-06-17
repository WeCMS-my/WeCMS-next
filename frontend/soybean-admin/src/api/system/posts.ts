import { requestJson } from "@/api/request";
import type { ApiResult, CreatePostRequest, PagedPostSummary, PostDetailDto, PostMutationResponse, UpdatePostRequest } from "@/api/types/generated";

export interface PostListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  status?: string;
}

export function getPostsApi(query: PostListQuery = { page: 1, pageSize: 20 }): Promise<ApiResult<PagedPostSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "status", query.status);
  return requestJson<PagedPostSummary>(`/api/v1/system/posts?${params}`);
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

export function enablePostApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/posts/${id}/enable`, { method: "POST" });
}

export function disablePostApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/posts/${id}/disable`, { method: "POST" });
}

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}
