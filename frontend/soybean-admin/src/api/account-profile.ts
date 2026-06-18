import { requestJson } from "./request";
import type {
  AccountAvatarResponse,
  AccountProfileResponse,
  AccountSecurityResponse,
  ApiResult,
  ChangeAccountPasswordRequest,
  UpdateAccountProfileRequest
} from "./types/generated";

export interface UploadAccountAvatarInput {
  file: File;
  sha256: string;
}

export function getAccountProfileApi(): Promise<ApiResult<AccountProfileResponse>> {
  return requestJson<AccountProfileResponse>("/api/v1/account/profile");
}

export function updateAccountProfileApi(
  request: UpdateAccountProfileRequest
): Promise<ApiResult<AccountProfileResponse>> {
  return requestJson<AccountProfileResponse>("/api/v1/account/profile", {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function changeAccountPasswordApi(request: ChangeAccountPasswordRequest): Promise<ApiResult<unknown>> {
  return requestJson<unknown>("/api/v1/account/password", {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function uploadAccountAvatarApi(input: UploadAccountAvatarInput): Promise<ApiResult<AccountAvatarResponse>> {
  const form = new FormData();
  form.set("originalName", input.file.name);
  form.set("mimeType", input.file.type);
  form.set("sizeBytes", String(input.file.size));
  form.set("sha256", input.sha256);
  form.set("file", input.file);
  return requestJson<AccountAvatarResponse>("/api/v1/account/avatar", {
    method: "POST",
    body: form
  });
}

export function getAccountSecurityApi(): Promise<ApiResult<AccountSecurityResponse>> {
  return requestJson<AccountSecurityResponse>("/api/v1/account/security");
}
