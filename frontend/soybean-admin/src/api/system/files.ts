import { requestBlob, requestJson } from "@/api/request";
import type { ApiResult, FileDetailDto, FileMutationResponse, PagedFileSummary } from "@/api/types/generated";

export interface FileListQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  mimeType?: string;
  status?: string;
}

export interface UploadFileInput {
  file: File;
  sha256: string;
}

export function getFilesApi(query: FileListQuery): Promise<ApiResult<PagedFileSummary>> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize)
  });
  appendParam(params, "keyword", query.keyword);
  appendParam(params, "mimeType", query.mimeType);
  appendParam(params, "status", query.status);
  return requestJson<PagedFileSummary>(`/api/v1/system/files?${params}`);
}

export function getFileApi(id: number): Promise<ApiResult<FileDetailDto>> {
  return requestJson<FileDetailDto>(`/api/v1/system/files/${id}`);
}

export function uploadFileApi(input: UploadFileInput): Promise<ApiResult<FileMutationResponse>> {
  const form = new FormData();
  form.set("originalName", input.file.name);
  form.set("mimeType", input.file.type);
  form.set("sizeBytes", String(input.file.size));
  form.set("sha256", input.sha256);
  form.set("file", input.file);
  return requestJson<FileMutationResponse>("/api/v1/system/files", {
    method: "POST",
    body: form
  });
}

export function previewFileApi(id: number): Promise<Blob> {
  return requestBlob(`/api/v1/system/files/${id}/preview`);
}

export function downloadFileApi(id: number): Promise<Blob> {
  return requestBlob(`/api/v1/system/files/${id}/download`);
}

export function deleteFileApi(id: number): Promise<ApiResult<unknown>> {
  return requestJson<unknown>(`/api/v1/system/files/${id}`, { method: "DELETE" });
}

function appendParam(params: URLSearchParams, key: string, value?: string): void {
  const normalized = value?.trim();
  if (normalized) {
    params.set(key, normalized);
  }
}
