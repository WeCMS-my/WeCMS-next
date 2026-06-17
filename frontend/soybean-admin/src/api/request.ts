import { readTokenSet } from "@/utils/token";
import type { ApiResult } from "./types/generated";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

export interface RequestOptions extends RequestInit {
  skipAuth?: boolean;
}

export async function requestJson<TData>(
  path: string,
  options: RequestOptions = {}
): Promise<ApiResult<TData>> {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && !headers.has("Content-Type") && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  if (!options.skipAuth) {
    const tokenSet = readTokenSet();
    if (tokenSet?.accessToken) {
      headers.set("Authorization", `Bearer ${tokenSet.accessToken}`);
    }
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers
  });

  const result = (await response.json()) as ApiResult<TData>;
  if (!response.ok) {
    throw result;
  }

  return result;
}
