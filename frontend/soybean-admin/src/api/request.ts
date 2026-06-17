import { readTokenSet } from "@/utils/token";
import { clearTokenSet, saveTokenSet } from "@/utils/token";
import type { ApiResult, LoginResponse } from "./types/generated";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
let refreshPromise: Promise<LoginResponse> | null = null;

export interface RequestOptions extends RequestInit {
  skipAuth?: boolean;
  skipRefresh?: boolean;
}

export async function requestJson<TData>(
  path: string,
  options: RequestOptions = {}
): Promise<ApiResult<TData>> {
  return sendJson<TData>(path, options);
}

export async function requestBlob(path: string, options: RequestOptions = {}): Promise<Blob> {
  return sendBlob(path, options);
}

async function sendJson<TData>(path: string, options: RequestOptions): Promise<ApiResult<TData>> {
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

  const result = await readApiResult<TData>(response);
  if (response.status === 401 && !options.skipAuth && !options.skipRefresh) {
    await refreshSession();
    return sendJson<TData>(path, {
      ...options,
      skipRefresh: true
    });
  }

  if (!response.ok) {
    throw result;
  }

  return result;
}

async function refreshSession(): Promise<LoginResponse> {
  if (!refreshPromise) {
    refreshPromise = refreshAccessToken().finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

async function refreshAccessToken(): Promise<LoginResponse> {
  const tokenSet = readTokenSet();
  if (!tokenSet?.refreshToken) {
    clearTokenSet();
    redirectToLogin();
    throw new Error("Missing refresh token.");
  }

  const response = await fetch(`${apiBaseUrl}/api/v1/auth/refresh`, {
    method: "POST",
    headers: {
      "Accept": "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      refreshToken: tokenSet.refreshToken
    })
  });

  const result = await readApiResult<LoginResponse>(response);
  if (!response.ok) {
    clearTokenSet();
    redirectToLogin();
    throw result;
  }

  saveTokenSet({
    accessToken: result.data.accessToken,
    refreshToken: result.data.refreshToken,
    expiresAt: result.data.expiresAt
  });

  return result.data;
}

function redirectToLogin(): void {
  if (window.location.pathname !== "/login") {
    const redirect = encodeURIComponent(`${window.location.pathname}${window.location.search}`);
    window.location.assign(`/login?redirect=${redirect}`);
  }
}

async function sendBlob(path: string, options: RequestOptions): Promise<Blob> {
  const headers = new Headers(options.headers);
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

  if (response.status === 401 && !options.skipAuth && !options.skipRefresh) {
    await refreshSession();
    return sendBlob(path, {
      ...options,
      skipRefresh: true
    });
  }

  if (!response.ok) {
    const result = await readApiResult<unknown>(response);
    throw result;
  }

  return response.blob();
}

async function readApiResult<TData>(response: Response): Promise<ApiResult<TData>> {
  const text = await response.text();
  if (!text) {
    return {
      code: response.status,
      msg: response.statusText || "Empty response body.",
      data: null as TData
    };
  }

  try {
    return JSON.parse(text) as ApiResult<TData>;
  } catch {
    return {
      code: response.status,
      msg: text.slice(0, 300),
      data: null as TData
    };
  }
}
