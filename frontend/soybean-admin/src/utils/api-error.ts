import type { ApiResult } from "@/api/types/generated";

export function apiErrorMessage(error: unknown): string {
  if (isApiResult(error)) {
    return error.msg || "请求失败。";
  }

  if (error instanceof Error) {
    return error.message || "请求失败。";
  }

  return "请求失败。";
}

function isApiResult(error: unknown): error is ApiResult<unknown> {
  return Boolean(error && typeof error === "object" && "code" in error && "msg" in error);
}
